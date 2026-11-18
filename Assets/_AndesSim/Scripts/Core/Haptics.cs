using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Vibración del celular (Android). En VR la vibración "se siente" dentro de la escena:
    /// la alerta tiembla, bloquear da un golpecito, el teléfono vibra al sonar.
    /// En el Editor no hace nada.
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static bool tried;

        static AndroidJavaObject Vibrator()
        {
            if (tried) return vibrator;
            tried = true;
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch { vibrator = null; }
            return vibrator;
        }
#endif

        /// <summary>Vibra <paramref name="ms"/> milisegundos.</summary>
        public static void Pulse(long ms)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var v = Vibrator();
            if (v != null) { try { v.Call("vibrate", ms); return; } catch { } }
            Handheld.Vibrate(); // respaldo (y asegura el permiso VIBRATE en el APK)
#endif
        }

        // Atajos con intención (el resto del código no piensa en milisegundos)
        public static void Tap() => Pulse(25);        // mirar/elegir
        public static void Confirm() => Pulse(60);    // bloquear, defensa, evidencia
        public static void Warn() => Pulse(180);      // pago perdido
        public static void Alarm() => Pulse(450);     // alerta JUPITER
        public static void Ring() => Pulse(350);      // teléfono sonando

        /// <summary>Solo para que Unity agregue el permiso VIBRATE al compilar.</summary>
        static void EnsurePermission() { Handheld.Vibrate(); }
    }
}
