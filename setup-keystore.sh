#!/bin/bash
# Generate an Android release keystore for Murge.
# Run once. The keystore file is gitignored — back it up securely.
#
# Usage: ./setup-keystore.sh

set -e

KEYSTORE_PATH="./murge.keystore"
ALIAS="murge"

if [ -f "$KEYSTORE_PATH" ]; then
    echo "Keystore already exists at $KEYSTORE_PATH"
    echo "Delete it first if you want to regenerate."
    exit 1
fi

echo "========================================"
echo "  Generating Android Keystore"
echo "========================================"
echo ""
echo "You'll be prompted for passwords and identity info."
echo "Remember these passwords — you'll need them for every release build."
echo ""

keytool -genkeypair \
    -v \
    -keystore "$KEYSTORE_PATH" \
    -alias "$ALIAS" \
    -keyalg RSA \
    -keysize 2048 \
    -validity 10000

echo ""
echo "========================================"
echo "  Keystore created: $KEYSTORE_PATH"
echo "  Alias: $ALIAS"
echo "========================================"
echo ""
echo "Before building prod, set these environment variables:"
echo "  export MURGE_KEYSTORE_PASS=<your keystore password>"
echo "  export MURGE_KEY_ALIAS_PASS=<your key password>"
echo ""
echo "IMPORTANT: Back up this keystore file securely."
echo "If you lose it, you cannot update your app on Google Play."
