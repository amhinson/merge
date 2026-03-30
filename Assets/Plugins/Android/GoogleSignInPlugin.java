package com.murge.game;

import android.app.Activity;
import android.content.Intent;
import android.util.Log;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.Task;

import com.unity3d.player.UnityPlayer;

/**
 * Google Sign In plugin for Unity using the legacy Google Sign-In API.
 * Returns the ID token to Unity via UnitySendMessage.
 */
public class GoogleSignInPlugin {

    private static final String TAG = "GoogleSignIn";
    private static final int RC_SIGN_IN = 9001;

    // Web Client ID from Google Cloud Console (must match Supabase config)
    private static final String WEB_CLIENT_ID = "192938409753-cbj32kfte94anvkvvck4qm6jindgdltc.apps.googleusercontent.com";

    private static GoogleSignInClient signInClient;

    public static void signIn(final Activity activity) {
        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
            .requestIdToken(WEB_CLIENT_ID)
            .requestEmail()
            .build();

        signInClient = GoogleSignIn.getClient(activity, gso);

        // Sign out first to force account picker
        signInClient.signOut().addOnCompleteListener(task -> {
            Intent signInIntent = signInClient.getSignInIntent();
            activity.startActivityForResult(signInIntent, RC_SIGN_IN);
        });
    }

    // Must be called from onActivityResult in the Unity activity
    public static boolean handleActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode != RC_SIGN_IN) return false;

        Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(data);
        try {
            GoogleSignInAccount account = task.getResult(ApiException.class);
            String idToken = account.getIdToken();
            if (idToken != null) {
                Log.d(TAG, "Google Sign In success, token length: " + idToken.length());
                UnityPlayer.UnitySendMessage("NativeSignIn", "OnGoogleSignInSuccess", idToken);
            } else {
                UnityPlayer.UnitySendMessage("NativeSignIn", "OnGoogleSignInFailure", "No ID token");
            }
        } catch (ApiException e) {
            Log.e(TAG, "Google Sign In failed: " + e.getStatusCode() + " " + e.getMessage());
            UnityPlayer.UnitySendMessage("NativeSignIn", "OnGoogleSignInFailure",
                "Google Sign In failed: " + e.getStatusCode());
        }
        return true;
    }
}
