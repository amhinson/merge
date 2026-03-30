#import <AuthenticationServices/AuthenticationServices.h>

// ─────────────────────────────────────────────────────────
// Apple Sign In native plugin for Unity
// Presents the ASAuthorizationController and sends the
// identity token back to Unity via UnitySendMessage.
// ─────────────────────────────────────────────────────────

@interface MurgeAppleSignInDelegate : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@end

@implementation MurgeAppleSignInDelegate

- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller {
    return UnityGetMainWindow();
}

- (void)authorizationController:(ASAuthorizationController *)controller
   didCompleteWithAuthorization:(ASAuthorization *)authorization {
    if ([authorization.credential isKindOfClass:[ASAuthorizationAppleIDCredential class]]) {
        ASAuthorizationAppleIDCredential *cred = (ASAuthorizationAppleIDCredential *)authorization.credential;

        // The identity token is what Supabase needs
        NSData *tokenData = cred.identityToken;
        if (tokenData) {
            NSString *idToken = [[NSString alloc] initWithData:tokenData encoding:NSUTF8StringEncoding];
            UnitySendMessage("NativeSignIn", "OnAppleSignInSuccess", [idToken UTF8String]);
        } else {
            UnitySendMessage("NativeSignIn", "OnAppleSignInFailure", "No identity token received");
        }
    } else {
        UnitySendMessage("NativeSignIn", "OnAppleSignInFailure", "Unexpected credential type");
    }
}

- (void)authorizationController:(ASAuthorizationController *)controller
           didCompleteWithError:(NSError *)error {
    NSString *errorMsg = [NSString stringWithFormat:@"Apple Sign In error: %@ (code %ld)",
                          error.localizedDescription, (long)error.code];
    UnitySendMessage("NativeSignIn", "OnAppleSignInFailure", [errorMsg UTF8String]);
}

@end

// Keep a strong reference so the delegate isn't deallocated during the flow
static MurgeAppleSignInDelegate *_signInDelegate = nil;

extern "C" {
    extern UIWindow* UnityGetMainWindow(void);

    void MurgeAppleSignIn_Start() {
        if (@available(iOS 13.0, *)) {
            ASAuthorizationAppleIDProvider *provider = [[ASAuthorizationAppleIDProvider alloc] init];
            ASAuthorizationAppleIDRequest *request = [provider createRequest];
            request.requestedScopes = @[ASAuthorizationScopeEmail];

            ASAuthorizationController *controller =
                [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];

            _signInDelegate = [[MurgeAppleSignInDelegate alloc] init];
            controller.delegate = _signInDelegate;
            controller.presentationContextProvider = _signInDelegate;

            [controller performRequests];
        } else {
            UnitySendMessage("NativeSignIn", "OnAppleSignInFailure", "Apple Sign In requires iOS 13+");
        }
    }
}
