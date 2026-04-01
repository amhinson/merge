#import <StoreKit/StoreKit.h>

extern "C" {
    void _RequestAppReview() {
        if (@available(iOS 14.0, *)) {
            UIWindowScene *windowScene = nil;
            for (UIScene *scene in [UIApplication sharedApplication].connectedScenes) {
                if (scene.activationState == UISceneActivationStateForegroundActive &&
                    [scene isKindOfClass:[UIWindowScene class]]) {
                    windowScene = (UIWindowScene *)scene;
                    break;
                }
            }
            if (windowScene) {
                [SKStoreReviewController requestReviewInScene:windowScene];
            }
        } else if (@available(iOS 10.3, *)) {
            [SKStoreReviewController requestReview];
        }
    }
}
