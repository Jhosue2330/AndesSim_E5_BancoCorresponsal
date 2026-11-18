using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Capa de "game feel": fundido a negro (teletransporte cómodo en VR), ráfagas de partículas,
    /// texto flotante ("+1 EVIDENCIA") y tarjetas de título cinematográficas. Todo con pool, sin Instantiate por evento.
    /// </summary>
    public class FxManager : MonoBehaviour
    {
        public static FxManager I;

        [Header("Referencias (las crea el constructor de escena)")]
        public Transform head;
        [Tooltip("Quad negro delante de la cámara para los fundidos.")]
        public Renderer fadeQuad;
        [Tooltip("Sistema de partículas para ráfagas (se usa con Emit).")]
        public ParticleSystem burst;

        [Header("Ajustes")]
        public float floatRise = 0.45f;
        public float floatSeconds = 1.3f;
        public float titleSeconds = 3.6f;

        readonly Queue<TextMeshPro> floatPool = new Queue<TextMeshPro>();
        TextMeshPro title, subtitle;
        MaterialPropertyBlock mpb;
        float fadeAlpha;
        Coroutine fadeRoutine;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            mpb = new MaterialPropertyBlock();
            SetFade(1f); // la app empieza en negro y aparece con un fundido
            for (int i = 0; i < 6; i++) floatPool.Enqueue(MakeText("FloatText", 1.4f));
            title = MakeText("Titulo", 1.7f);
            subtitle = MakeText("Subtitulo", 0.75f);
        }

        void Start() => FadeIn(1.2f);

        void OnDestroy() { if (I == this) I = null; }

        TextMeshPro MakeText(string name, float size)
        {
            var go = new GameObject("FX_" + name);
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.characterSpacing = 4f;
            t.fontStyle = FontStyles.Bold;
            t.color = Palette.Ink;
            go.SetActive(false);
            return t;
        }

        // ---------- fundidos ----------
        void SetFade(float a)
        {
            fadeAlpha = a;
            if (fadeQuad == null) return;
            fadeQuad.enabled = a > 0.001f;
            fadeQuad.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", Palette.WithAlpha(Palette.Void, a));
            fadeQuad.SetPropertyBlock(mpb);
        }

        public void FadeIn(float seconds = 0.35f) => FadeTo(0f, seconds);
        public void FadeOut(float seconds = 0.2f) => FadeTo(1f, seconds);

        void FadeTo(float target, float seconds)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            float from = fadeAlpha;
            fadeRoutine = StartCoroutine(Ease.Tween(seconds, k => SetFade(Mathf.Lerp(from, target, k)), Ease.InOutSine));
        }

        /// <summary>Fundido a negro, acción en la oscuridad, fundido de vuelta (cómodo en VR).</summary>
        public IEnumerator FadeThrough(Action inDarkness, float outSeconds = 0.18f, float inSeconds = 0.3f)
        {
            FadeOut(outSeconds);
            yield return new WaitForSecondsRealtime(outSeconds);
            inDarkness?.Invoke();
            yield return null;
            FadeIn(inSeconds);
        }

        // ---------- partículas ----------
        public void Burst(Vector3 position, Color color, int count = 14, float speed = 1.6f)
        {
            if (burst == null) return;
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = true, position = position, startColor = color };
            for (int i = 0; i < count; i++)
            {
                ep.velocity = UnityEngine.Random.onUnitSphere * speed * UnityEngine.Random.Range(0.6f, 1.2f) + Vector3.up * 0.4f;
                ep.startSize = UnityEngine.Random.Range(0.05f, 0.12f);
                ep.startLifetime = UnityEngine.Random.Range(0.45f, 0.8f);
                burst.Emit(ep, 1);
            }
        }

        // ---------- texto flotante ----------
        public static void Float(Vector3 position, string text, Color color)
        {
            if (I != null) I.StartCoroutine(I.FloatRoutine(position, text, color));
        }

        IEnumerator FloatRoutine(Vector3 position, string text, Color color)
        {
            var t = floatPool.Count > 0 ? floatPool.Dequeue() : MakeText("FloatText", 1.4f);
            t.text = text;
            t.gameObject.SetActive(true);
            Vector3 start = position;
            yield return Ease.Tween(floatSeconds, k =>
            {
                t.transform.position = start + Vector3.up * floatRise * Ease.OutCubic(k);
                FaceHead(t.transform);
                float s = k < 0.18f ? Ease.OutBack(k / 0.18f) : 1f;
                t.transform.localScale = Vector3.one * Mathf.Max(0.01f, s);
                t.color = Palette.WithAlpha(color, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            }, Ease.Linear);
            t.gameObject.SetActive(false);
            floatPool.Enqueue(t);
        }

        // ---------- tarjeta de título ----------
        public static void Title(string main, string sub, Color color)
        {
            if (I != null) I.StartCoroutine(I.TitleRoutine(main, sub, color));
        }

        IEnumerator TitleRoutine(string main, string sub, Color color)
        {
            if (head == null) yield break;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            Vector3 basePos = head.position + fwd * 3.2f + Vector3.up * 0.35f;
            title.text = main; subtitle.text = sub;
            title.gameObject.SetActive(true); subtitle.gameObject.SetActive(true);
            title.transform.position = basePos;
            subtitle.transform.position = basePos + Vector3.down * 0.26f;
            FaceHead(title.transform); FaceHead(subtitle.transform);
            AudioManager.P(Sfx.Title);
            yield return Ease.Tween(titleSeconds, k =>
            {
                float a = k < 0.15f ? k / 0.15f : k > 0.8f ? 1f - (k - 0.8f) / 0.2f : 1f;
                title.characterSpacing = Mathf.Lerp(14f, 4f, Ease.OutCubic(Mathf.Min(1f, k * 3f)));
                title.color = Palette.WithAlpha(color, a);
                subtitle.color = Palette.WithAlpha(Palette.Ink, a * 0.9f);
            }, Ease.Linear);
            title.gameObject.SetActive(false); subtitle.gameObject.SetActive(false);
        }

        void FaceHead(Transform t)
        {
            if (head == null) return;
            Vector3 d = t.position - head.position;
            if (d.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(d);
        }
    }
}
