using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Supervisora holográfica sobre la mesa: guía al jugador en momentos clave.
    /// Habla con subtítulos tipo máquina de escribir y "voz" sintetizada (sílabas),
    /// parpadea como holograma y gesticula mientras habla.
    /// </summary>
    public class SupervisorHolo : MonoBehaviour
    {
        public Animation anim;
        public Renderer[] renderers;
        public TMP_Text subtitle;
        public GameObject subtitleRoot;
        public float charsPerSecond = 32f;

        readonly Queue<(string text, float hold)> queue = new Queue<(string, float)>();
        bool speaking;
        MaterialPropertyBlock mpb;
        Transform head;

        public static SupervisorHolo I;

        void Awake() { I = this; mpb = new MaterialPropertyBlock(); if (subtitleRoot) subtitleRoot.SetActive(false); }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            head = UIManager.I ? UIManager.I.head : Camera.main?.transform;
            if (anim == null) anim = GetComponentInChildren<Animation>();
            if (anim) anim.CrossFade("Idle");
        }

        /// <summary>Agrega una frase a la cola (se dicen en orden).</summary>
        public static void Say(string text, float hold = 2.5f)
        {
            if (I == null) return;
            I.queue.Enqueue((text, hold));
            if (!I.speaking) I.StartCoroutine(I.Run());
        }

        IEnumerator Run()
        {
            speaking = true;
            if (subtitleRoot) subtitleRoot.SetActive(true);
            while (queue.Count > 0)
            {
                var (text, hold) = queue.Dequeue();
                if (anim) anim.CrossFade("Hablar", 0.25f);
                subtitle.text = text;
                subtitle.ForceMeshUpdate();
                int total = subtitle.textInfo.characterCount;
                for (int i = 0; i <= total; i++)
                {
                    subtitle.maxVisibleCharacters = i;
                    // "voz" sintetizada: una sílaba cada 3 letras, con tono variable
                    if (i % 3 == 0 && i < total) AudioManager.At(Sfx.Typing, transform.position + Vector3.up * 0.5f, 0.6f);
                    yield return new WaitForSeconds(1f / charsPerSecond);
                }
                if (anim) anim.CrossFade("Idle", 0.4f);
                yield return new WaitForSeconds(hold);
            }
            if (subtitleRoot) subtitleRoot.SetActive(false);
            speaking = false;
        }

        void Update()
        {
            // Parpadeo de holograma + mirar al jugador
            float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 6f, 0.3f);
            if (Random.value < 0.01f) flicker = 0.25f;
            if (renderers != null)
                foreach (var r in renderers)
                {
                    if (!r) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor("_TintColor", Palette.WithAlpha(Palette.Data, 0.55f * flicker));
                    r.SetPropertyBlock(mpb);
                }
            if (head)
            {
                Vector3 d = head.position - transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 2f);
                if (subtitleRoot && subtitleRoot.activeSelf)
                {
                    Vector3 s = subtitleRoot.transform.position - head.position;
                    subtitleRoot.transform.rotation = Quaternion.LookRotation(s);
                }
            }
        }
    }
}
