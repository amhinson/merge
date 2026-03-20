Before building for TestFlight, you need:

Apple Developer Account ($99/year)

1. Enroll at https://developer.apple.com if you haven't already
2. A paid account is required for TestFlight/App Store — the free account won't work

App Store Connect Setup

1. Log into https://appstoreconnect.apple.com
2. Create a new app: Apps → + → New App


    - Platform: iOS
    - Name: Daily Drop
    - Bundle ID: register a new one (e.g. com.yourname.dailydrop)
    - SKU: dailydrop

Unity Project Settings

1. In Unity: Edit → Project Settings → Player → iOS tab


    - Bundle Identifier: must match what you registered above
    - Version: 0.1.0
    - Build: 1
    - Signing Team ID: your Apple Developer Team ID (found in developer.apple.com → Membership)
    - Automatically Sign: checked

Xcode

1. Make sure Xcode is installed and up to date
2. Open Xcode at least once and accept the license
3. Sign in with your Apple ID: Xcode → Settings → Accounts

Update the build script Unity path — verify this matches your install:
ls /Applications/Unity/Hub/Editor/
Update UNITY_PATH in build-ios.sh if the version folder is different.

Once all that's done, run ./build-ios.sh prod.
