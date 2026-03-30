#if __has_include(<GoogleSignIn/GoogleSignIn.h>)
#import <GoogleSignIn/GoogleSignIn.h>

extern UIViewController* UnityGetGLViewController(void);

// iOS client ID — used to initialize the SDK
static NSString *kGoogleClientID = @"192938409753-bmputs91st1210bnvhk4295g5ccubhjj.apps.googleusercontent.com";
// Web client ID — used as serverClientId so the ID token audience matches Supabase
static NSString *kGoogleServerClientID = @"192938409753-cbj32kfte94anvkvvck4qm6jindgdltc.apps.googleusercontent.com";

extern "C" {

void MurgeGoogleSignIn_Start() {
    UIViewController *rootVC = UnityGetGLViewController();

    GIDConfiguration *config = [[GIDConfiguration alloc] initWithClientID:kGoogleClientID
                                                          serverClientID:kGoogleServerClientID];
    [GIDSignIn.sharedInstance setConfiguration:config];

    [GIDSignIn.sharedInstance signInWithPresentingViewController:rootVC
                                                     completion:^(GIDSignInResult * _Nullable result,
                                                                  NSError * _Nullable error) {
        if (error) {
            NSString *errorMsg = [NSString stringWithFormat:@"Google Sign In error: %@",
                                  error.localizedDescription];
            UnitySendMessage("NativeSignIn", "OnGoogleSignInFailure", [errorMsg UTF8String]);
            return;
        }

        NSString *idToken = result.user.idToken.tokenString;
        if (idToken) {
            UnitySendMessage("NativeSignIn", "OnGoogleSignInSuccess", [idToken UTF8String]);
        } else {
            UnitySendMessage("NativeSignIn", "OnGoogleSignInFailure", "No ID token received");
        }
    }];
}

}

#else

extern "C" {
void MurgeGoogleSignIn_Start() {
    UnitySendMessage("NativeSignIn", "OnGoogleSignInFailure", "GoogleSignIn SDK not installed");
}
}

#endif
