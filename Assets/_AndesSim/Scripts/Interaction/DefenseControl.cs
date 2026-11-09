using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Contramedida que el jugador activa con la retícula (interruptor, botón, teléfono).
    /// Solo funciona en modo Defensa; en modo Ataque explica que en 2016 no existía.
    /// </summary>
    public class DefenseControl : GazeInteractable
    {
        [Header("Defensa")]
        public string defenseId = "defensa";
        public string title = "Defensa";
        [TextArea(2, 6)] public string description;
        public Color activeColor = new Color(0.22f, 0.95f, 0.54f);
        public Color disabledColor = new Color(0.25f, 0.28f, 0.27f);
        [Tooltip("Parte que gira al activarse (p. ej. la palanca del interruptor).")]
        public Transform lever;
        public Vector3 leverOnEuler = new Vector3(-35f, 0f, 0f);
        [Tooltip("Luz o indicador opcional que se enciende.")]
        public GameObject onIndicator;

        public bool IsActive { get; private set; }

        void Start()
        {
            if (onIndicator) onIndicator.SetActive(false);
            if (GameManager.I != null && GameManager.I.mode == SimMode.Ataque) SetBaseColor(disabledColor);
        }

        protected override void Clicked()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            if (gm.mode == SimMode.Ataque)
            {
                UIManager.Msg("NO DISPONIBLE EN 2016", $"«{title}» no existía la noche del ataque. Prueba el modo Defensa.", 4f);
                AudioManager.P(Sfx.Fail, 0.5f);
                return;
            }
            if (IsActive) { UIManager.Msg(title + " · ACTIVA", description, 4f); return; }
            Activate();
        }

        public void Activate()
        {
            IsActive = true;
            GameManager.I?.ApplyDefense(defenseId);
            SetBaseColor(activeColor);
            if (lever) lever.localEulerAngles = leverOnEuler;
            if (onIndicator) onIndicator.SetActive(true);
            AudioManager.P(Sfx.Defense);
            UIManager.Msg("DEFENSA ACTIVADA · " + title, description, 6f);
        }
    }
}
