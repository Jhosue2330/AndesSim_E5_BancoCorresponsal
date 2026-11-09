using System.Collections.Generic;
using UnityEngine;

namespace AndesSim
{
    public enum Sfx { Click, Hover, Evidence, Defense, Alarm, PhoneRing, PhoneAnswer, PhoneNoAnswer, OrderIn, Success, Fail, Teleport }

    /// <summary>
    /// Sonidos del escenario. Si un clip no está asignado, genera un tono simple
    /// para que la app siempre tenga sonido (reemplazar por audios libres con créditos).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I;

        [Header("Ambiente (loop)")]
        public AudioClip ambient;
        [Range(0, 1)] public float ambientVolume = 0.35f;

        [Header("Efectos (opcional: si están vacíos se usa un tono generado)")]
        public AudioClip click;
        public AudioClip hover;
        public AudioClip evidence;
        public AudioClip defense;
        public AudioClip alarm;
        public AudioClip phoneRing;
        public AudioClip phoneAnswer;
        public AudioClip phoneNoAnswer;
        public AudioClip orderIn;
        public AudioClip success;
        public AudioClip fail;
        public AudioClip teleport;

        [Range(0, 1)] public float sfxVolume = 0.8f;

        AudioSource sfxSource;
        AudioSource ambientSource;
        readonly Dictionary<Sfx, AudioClip> generated = new Dictionary<Sfx, AudioClip>();

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
        }

        void Start()
        {
            ambientSource.clip = ambient != null ? ambient : Generate("ambient", 4f, t =>
                0.05f * Mathf.Sin(2 * Mathf.PI * 55f * t) + 0.03f * Mathf.Sin(2 * Mathf.PI * 110.5f * t) + 0.01f * (Random.value - 0.5f));
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }

        void OnDestroy() { if (I == this) I = null; }

        public void Play(Sfx id, float volumeScale = 1f)
        {
            var clip = Get(id);
            if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        /// <summary>Atajo estático seguro: no falla si no hay AudioManager en la escena.</summary>
        public static void P(Sfx id, float volumeScale = 1f) { if (I != null) I.Play(id, volumeScale); }

        AudioClip Get(Sfx id)
        {
            AudioClip assigned = id switch
            {
                Sfx.Click => click, Sfx.Hover => hover, Sfx.Evidence => evidence, Sfx.Defense => defense,
                Sfx.Alarm => alarm, Sfx.PhoneRing => phoneRing, Sfx.PhoneAnswer => phoneAnswer,
                Sfx.PhoneNoAnswer => phoneNoAnswer, Sfx.OrderIn => orderIn, Sfx.Success => success,
                Sfx.Fail => fail, Sfx.Teleport => teleport, _ => null
            };
            if (assigned != null) return assigned;
            if (!generated.TryGetValue(id, out var g)) { g = GenerateFor(id); generated[id] = g; }
            return g;
        }

        // ---------- tonos generados ----------
        static float Tone(float f, float t) => Mathf.Sin(2 * Mathf.PI * f * t);
        static float Env(float t, float len) => Mathf.Clamp01(t * 60f) * Mathf.Clamp01((len - t) * 30f);

        AudioClip GenerateFor(Sfx id)
        {
            switch (id)
            {
                case Sfx.Click: return Generate("click", 0.06f, t => 0.5f * Tone(1200, t) * Env(t, 0.06f));
                case Sfx.Hover: return Generate("hover", 0.04f, t => 0.15f * Tone(900, t) * Env(t, 0.04f));
                case Sfx.Evidence: return Generate("evidence", 0.35f, t => 0.4f * Tone(t < 0.15f ? 660 : 990, t) * Env(t, 0.35f));
                case Sfx.Defense: return Generate("defense", 0.4f, t => 0.4f * Tone(440 + 600 * t, t) * Env(t, 0.4f));
                case Sfx.Alarm: return Generate("alarm", 1.2f, t => 0.45f * Tone((int)(t * 4) % 2 == 0 ? 880 : 660, t) * Env(t, 1.2f));
                case Sfx.PhoneRing: return Generate("ring", 2f, t =>
                    t < 1f ? 0.35f * (Tone(440, t) + Tone(480, t)) * (Mathf.Sin(2 * Mathf.PI * 20 * t) > 0 ? 1 : 0.2f) * Env(t, 1f) : 0f);
                case Sfx.PhoneAnswer: return Generate("answer", 0.5f, t => 0.35f * Tone(t < 0.2f ? 523 : 784, t) * Env(t, 0.5f));
                case Sfx.PhoneNoAnswer: return Generate("busy", 1.5f, t => ((int)(t * 4) % 2 == 0 ? 0.3f * (Tone(480, t) + Tone(620, t)) : 0f) * Env(t, 1.5f));
                case Sfx.OrderIn: return Generate("orderin", 0.08f, t => 0.2f * Tone(1500, t) * Env(t, 0.08f));
                case Sfx.Success: return Generate("success", 0.9f, t => 0.35f * Tone(t < 0.3f ? 523 : t < 0.6f ? 659 : 784, t) * Env(t, 0.9f));
                case Sfx.Fail: return Generate("fail", 0.9f, t => 0.35f * Tone(t < 0.45f ? 330 : 220, t) * Env(t, 0.9f));
                case Sfx.Teleport: return Generate("teleport", 0.25f, t => 0.3f * Tone(300 + 1600 * t, t) * Env(t, 0.25f));
            }
            return null;
        }

        static AudioClip Generate(string name, float seconds, System.Func<float, float> wave)
        {
            const int rate = 22050;
            int n = Mathf.CeilToInt(seconds * rate);
            var samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = Mathf.Clamp(wave(i / (float)rate), -1f, 1f);
            var clip = AudioClip.Create("gen_" + name, n, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
