using System.Collections;
using UnityEngine;

namespace AndesSim
{
    public enum RoomMood { Calm, Alert, Danger, Secure }

    /// <summary>
    /// El "director de iluminación": cambia el color del neón, las luces, la niebla y la música
    /// según el momento de la historia. Calma = cian · Alerta = rojo pulsante con baliza giratoria ·
    /// Peligro = rojo tenue · Protegido = verde.
    /// </summary>
    public class AmbienceController : MonoBehaviour
    {
        public static AmbienceController I;

        [Tooltip("Material compartido de todas las tiras de neón (se clona en tiempo de ejecución).")]
        public Material neonMaterial;
        [Tooltip("Renderers que usan el neón.")]
        public Renderer[] neonRenderers;
        [Tooltip("Luces de ambiente que se tiñen con el estado.")]
        public Light[] moodLights;
        [Tooltip("Pivote con focos rojos que gira durante la alerta.")]
        public Transform alarmSpinner;
        public Light[] alarmLights;
        public float alarmSpinSpeed = 220f;
        public float transitionSeconds = 1.2f;

        public RoomMood Mood { get; private set; } = RoomMood.Calm;

        Material neonRuntime;
        Color neonColor, lightColor, fogColor;
        float lightIntensity = 1f, pulse;
        float[] baseIntensity;
        Coroutine transition;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            if (neonMaterial != null)
            {
                neonRuntime = new Material(neonMaterial) { name = "Neon (runtime)" };
                if (neonRenderers != null)
                    foreach (var r in neonRenderers) if (r) r.sharedMaterial = neonRuntime;
            }
            baseIntensity = new float[moodLights?.Length ?? 0];
            for (int i = 0; i < baseIntensity.Length; i++) baseIntensity[i] = moodLights[i] ? moodLights[i].intensity : 1f;
            SetAlarm(false);
            Apply(Palette.Data, Palette.KeyLight, Palette.Void, 1f);
            neonColor = Palette.Data; lightColor = Palette.KeyLight; fogColor = RenderSettings.fogColor;
        }

        void OnDestroy() { if (I == this) I = null; }

        public static void Set(RoomMood mood) { if (I != null) I.SetMood(mood); }

        public void SetMood(RoomMood mood)
        {
            Mood = mood;
            Color n, l, f; float k;
            switch (mood)
            {
                case RoomMood.Alert: n = Palette.Threat; l = Palette.Threat; f = new Color(0.10f, 0.01f, 0.02f); k = 0.9f; break;
                case RoomMood.Danger: n = Palette.Threat * 0.75f; l = Color.Lerp(Palette.KeyLight, Palette.Threat, 0.55f); f = new Color(0.06f, 0.01f, 0.02f); k = 0.75f; break;
                case RoomMood.Secure: n = Palette.Secure; l = Color.Lerp(Palette.KeyLight, Palette.Secure, 0.5f); f = new Color(0.01f, 0.05f, 0.04f); k = 1.1f; break;
                default: n = Palette.Data; l = Palette.KeyLight; f = Palette.Void; k = 1f; break;
            }
            SetAlarm(mood == RoomMood.Alert);
            AudioManager.I?.SetTension(mood == RoomMood.Alert ? 1f : mood == RoomMood.Danger ? 0.55f : 0f);
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(Blend(n, l, f, k));
        }

        IEnumerator Blend(Color n, Color l, Color f, float k)
        {
            Color n0 = neonColor, l0 = lightColor, f0 = fogColor; float k0 = lightIntensity;
            yield return Ease.Tween(transitionSeconds, t =>
            {
                neonColor = Color.Lerp(n0, n, t); lightColor = Color.Lerp(l0, l, t);
                fogColor = Color.Lerp(f0, f, t); lightIntensity = Mathf.Lerp(k0, k, t);
            }, Ease.InOutSine, unscaled: false);
        }

        void SetAlarm(bool on)
        {
            if (alarmSpinner) alarmSpinner.gameObject.SetActive(on);
            if (alarmLights != null) foreach (var a in alarmLights) if (a) a.enabled = on;
        }

        void Update()
        {
            pulse = Mood == RoomMood.Alert ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 3.2f)) : 1f;
            Apply(neonColor * pulse, lightColor, fogColor, lightIntensity * (Mood == RoomMood.Alert ? 0.6f + 0.4f * pulse : 1f));
            if (alarmSpinner && alarmSpinner.gameObject.activeSelf) alarmSpinner.Rotate(0f, alarmSpinSpeed * Time.deltaTime, 0f, Space.World);
        }

        void Apply(Color neon, Color light, Color fog, float intensity)
        {
            if (neonRuntime) neonRuntime.color = neon;
            if (moodLights != null)
                for (int i = 0; i < moodLights.Length; i++)
                    if (moodLights[i]) { moodLights[i].color = light; moodLights[i].intensity = baseIntensity[i] * intensity; }
            RenderSettings.fogColor = fog;
        }
    }
}
