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
                AudioManager.P(Sfx.Evidence);
                SetBaseColor(foundColor);
                if (foundMarker) foundMarker.SetActive(true);
                Vector3 top = GetComponent<Collider>().bounds.center + Vector3.up * 0.25f;
                FxManager.I?.Burst(top, Palette.Warning, 16, 1.4f);
                if (gm != null) FxManager.Float(top, $"+1 EVIDENCIA  {gm.EvidenceCount}/{gm.TotalEvidence}", Palette.Warning);
                StartCoroutine(Pop());
            }
        }
    }
}
