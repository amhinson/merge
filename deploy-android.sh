#!/bin/bash
# Deploy AAB to Google Play internal testing track.
# Usage:
#   ./deploy-android.sh                # Build prod AAB + upload to internal track
#   ./deploy-android.sh upload-only    # Upload existing AAB (skip build)
#   ./deploy-android.sh production     # Upload to production track (careful!)
#
# Prerequisites:
#   - play-service-account.json in project root
#   - Keystore env vars set (MURGE_KEYSTORE_PASS, MURGE_KEY_ALIAS_PASS)
#   - pip3 install google-api-python-client google-auth-httplib2 google-auth-oauthlib

set -e

PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
PACKAGE_NAME="com.murge.game"
AAB_PATH="$PROJECT_PATH/Build/Android/murge-prod.aab"
SERVICE_ACCOUNT="$PROJECT_PATH/play-service-account.json"
MODE="${1:-build-and-upload}"
TRACK="internal"

if [ "$1" = "production" ]; then
    TRACK="production"
    MODE="upload-only"
    echo "WARNING: Deploying to PRODUCTION track!"
    read -p "Are you sure? (y/N) " confirm
    [ "$confirm" != "y" ] && exit 0
fi

# Check service account
if [ ! -f "$SERVICE_ACCOUNT" ]; then
    echo "Service account key not found at $SERVICE_ACCOUNT"
    echo "Download it from Google Cloud Console > IAM > Service Accounts > Manage Keys"
    exit 1
fi

# Build if needed
if [ "$MODE" != "upload-only" ]; then
    echo "Building prod AAB..."
    source ~/.zshrc 2>/dev/null || true
    "$PROJECT_PATH/build-android.sh" prod
fi

# Check AAB exists
if [ ! -f "$AAB_PATH" ]; then
    echo "AAB not found at $AAB_PATH"
    echo "Run ./build-android.sh prod first."
    exit 1
fi

echo ""
echo "========================================"
echo "  Uploading to Google Play ($TRACK)"
echo "========================================"
echo "  Package: $PACKAGE_NAME"
echo "  AAB:     $(du -h "$AAB_PATH" | cut -f1)"
echo ""

# Upload via Python script
python3 - "$SERVICE_ACCOUNT" "$PACKAGE_NAME" "$AAB_PATH" "$TRACK" << 'PYTHON'
import sys
from googleapiclient.discovery import build
from google.oauth2 import service_account

key_file = sys.argv[1]
package_name = sys.argv[2]
aab_path = sys.argv[3]
track = sys.argv[4]

SCOPES = ['https://www.googleapis.com/auth/androidpublisher']

credentials = service_account.Credentials.from_service_account_file(
    key_file, scopes=SCOPES)

service = build('androidpublisher', 'v3', credentials=credentials)
edits = service.edits()

# Create a new edit
edit = edits.insert(body={}, packageName=package_name).execute()
edit_id = edit['id']
print(f"Edit created: {edit_id}")

try:
    # Upload the AAB
    print(f"Uploading AAB...")
    bundle = edits.bundles().upload(
        editId=edit_id,
        packageName=package_name,
        media_body=aab_path,
        media_mime_type='application/octet-stream'
    ).execute()
    version_code = bundle['versionCode']
    print(f"Uploaded: version code {version_code}")

    # Assign to track
    edits.tracks().update(
        editId=edit_id,
        packageName=package_name,
        track=track,
        body={
            'track': track,
            'releases': [{
                'versionCodes': [str(version_code)],
                'status': 'draft',
            }]
        }
    ).execute()
    print(f"Assigned to {track} track")

    # Commit the edit
    edits.commit(editId=edit_id, packageName=package_name).execute()
    print(f"Committed! Live on {track} track.")

except Exception as e:
    # Abort on error
    try:
        edits.delete(editId=edit_id, packageName=package_name).execute()
    except:
        pass
    print(f"ERROR: {e}")
    sys.exit(1)
PYTHON

echo ""
echo "========================================"
echo "  Done! Check Google Play Console."
echo "========================================"
