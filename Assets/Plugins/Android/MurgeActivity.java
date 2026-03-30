package com.murge.game;

import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayerActivity;

/**
 * Custom activity that forwards onActivityResult to the Google Sign In plugin.
 */
public class MurgeActivity extends UnityPlayerActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        GoogleSignInPlugin.handleActivityResult(requestCode, resultCode, data);
    }
}
