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
            }
        }
    }
}
