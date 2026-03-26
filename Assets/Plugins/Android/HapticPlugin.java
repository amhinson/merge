package com.murge.haptic;

import android.app.Activity;
import android.content.Context;
import android.os.Build;
import android.os.VibrationEffect;
import android.os.Vibrator;
import android.os.VibratorManager;
import com.unity3d.player.UnityPlayer;

public class HapticPlugin {

    private static Vibrator getVibrator() {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null) return null;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            VibratorManager vm = (VibratorManager) activity.getSystemService(Context.VIBRATOR_MANAGER_SERVICE);
            return vm != null ? vm.getDefaultVibrator() : null;
        } else {
            return (Vibrator) activity.getSystemService(Context.VIBRATOR_SERVICE);
        }
    }

    public static void vibrate(int milliseconds, int amplitude) {
        Vibrator vibrator = getVibrator();
        if (vibrator == null || !vibrator.hasVibrator()) return;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            VibrationEffect effect = VibrationEffect.createOneShot(milliseconds, amplitude);
            vibrator.vibrate(effect);
        } else {
            vibrator.vibrate(milliseconds);
        }
    }

    public static void hapticLight() {
        vibrate(20, 40);
    }

    public static void hapticMedium() {
        vibrate(30, 120);
    }

    public static void hapticHeavy() {
        vibrate(50, 255);
    }

    public static void hapticSelection() {
        vibrate(10, 30);
    }
}
