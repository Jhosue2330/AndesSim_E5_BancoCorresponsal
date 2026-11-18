using UnityEngine;

namespace AndesSim
{
    /// <summary>Pista que el jugador encuentra con la retícula. Llama a GameManager.RegisterEvidence.</summary>
    public class EvidenceItem : GazeInteractable
    {
        [Header("Evidencia")]
        public string evidenceId = "evidencia";
        public string title = "Evidencia";
        [TextArea(2, 6)] public string description;
        public Color foundColor = new Color(1f, 0.76f, 0.29f);
        [Tooltip("Objeto opcional que aparece al encontrarla (p. ej. una etiqueta).")]
        public GameObject foundMarker;
        [Header("Inspección")]
        [Tooltip("El objeto vuela hacia el jugador, gira y se lee en grande (documentos).")]
        public bool inspectable;
        public float inspectScale = 2.4f;
        public float inspectSeconds = 4.5f;
        bool inspecting;

        public bool Found { get; private set; }

        System.Collections.IEnumerator Pop()
        {
            Vector3 s0 = transform.localScale;
            yield return Ease.Tween(0.35f, k => transform.localScale = s0 * (1f + 0.25f * Mathf.Sin(k * Mathf.PI)), Ease.OutCubic);
            transform.localScale = s0;
        }

        protected override void Awake()
        {
            base.Awake();
            if (foundMarker) foundMarker.SetActive(false);
        }

        protected override void Clicked()
        {
            var gm = GameManager.I;
            bool isNew = gm != null && !gm.HasEvidence(evidenceId);
            gm?.RegisterEvidence(evidenceId);
            UIManager.Msg((isNew ? "EVIDENCIA · " : "") + title, description, 7f);
            if (!Found)
            {
                Found = true;
                AudioManager.At(Sfx.Evidence, transform.position, 1.2f);
                Haptics.Confirm();
                SetBaseColor(foundColor);
                if (foundMarker) foundMarker.SetActive(true);
                Vector3 top = GetComponent<Collider>().bounds.center + Vector3.up * 0.25f;
                FxManager.I?.Burst(top, Palette.Warning, 16, 1.4f);
                if (gm != null) FxManager.Float(top, $"+1 EVIDENCIA  {gm.EvidenceCount}/{gm.TotalEvidence}", Palette.Warning);
                if (!inspectable) StartCoroutine(Pop());
            }
            if (inspectable && !inspecting) StartCoroutine(Inspect());
        }

        /// <summary>El documento vuela frente a la cara, se agranda, gira un poco y vuelve a su lugar.</summary>
        System.Collections.IEnumerator Inspect()
        {
            var head = UIManager.I ? UIManager.I.head : null;
            if (head == null) yield break;
            inspecting = true;
            AudioManager.At(Sfx.Swoosh, transform.position);
            Vector3 p0 = transform.position, s0 = transform.localScale;
            Quaternion r0 = transform.rotation;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            Vector3 p1 = head.position + fwd * 0.75f + Vector3.down * 0.12f;
            // la cara del documento (su eje "arriba") apunta a los ojos
            Quaternion r1 = Quaternion.LookRotation(Vector3.up, -fwd);
            r1 = Quaternion.FromToRotation(r1 * Vector3.up, (head.position - p1).normalized) * r1;
            yield return Ease.Tween(0.55f, k =>
            {
                transform.position = Vector3.Lerp(p0, p1, k) + Vector3.up * 0.25f * Mathf.Sin(k * Mathf.PI);
                transform.rotation = Quaternion.Slerp(r0, r1, k);
                transform.localScale = Vector3.Lerp(s0, s0 * inspectScale, k);
            }, Ease.OutCubic, unscaled: false);
            float t = 0f;
            while (t < inspectSeconds)
            {
                t += Time.deltaTime;
                transform.rotation = r1 * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 6f);
                yield return null;
            }
            AudioManager.At(Sfx.Swoosh, transform.position, 0.6f);
            Quaternion r2 = transform.rotation;
            yield return Ease.Tween(0.5f, k =>
            {
                transform.position = Vector3.Lerp(p1, p0, k);
                transform.rotation = Quaternion.Slerp(r2, r0, k);
                transform.localScale = Vector3.Lerp(s0 * inspectScale, s0, k);
            }, Ease.InOutSine, unscaled: false);
            transform.SetPositionAndRotation(p0, r0);
            transform.localScale = s0;
            inspecting = false;
        }
    }
}
