using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Short vibration pulses on Android; a single long buzz via Handheld on other platforms.</summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject s_vibrator;
        private static bool s_failed;

        private static AndroidJavaObject Vibrator
        {
            get
            {
                if (s_vibrator != null || s_failed) return s_vibrator;
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        s_vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                catch { s_failed = true; }
                return s_vibrator;
            }
        }
#endif

        /// <summary>Tiny tick (near misses, coins).</summary>
        public static void Light() => Pulse(18);

        /// <summary>Strong buzz (crashes, big fights).</summary>
        public static void Heavy()
        {
            if (!SaveData.VibrationOn) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            // Referencing Handheld.Vibrate also makes Unity add the Android VIBRATE permission,
            // which the short JNI pulses above rely on.
            Handheld.Vibrate();
#endif
        }

        private static void Pulse(long milliseconds)
        {
            if (!SaveData.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Vibrator?.Call("vibrate", milliseconds); } catch { /* device without vibrator */ }
#endif
        }
    }
}
