using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AndesSim
{
    /// <summary>
    /// Objeto que reacciona a la retícula. Recibe los mismos mensajes que envía
    /// CardboardReticlePointer (OnPointerEnter / OnPointerExit / OnPointerClick),
    /// así funciona con el plugin de Cardboard o con nuestro GazeReticlePointer.
    /// Necesita un Collider en el mismo GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GazeInteractable : MonoBehaviour
    {
        [Tooltip("Si es falso, la retícula lo ignora.")]
        public bool interactable = true;
        [Tooltip("Permite activarlo mirando fijo unos segundos (sin tocar la pantalla).")]
        public bool allowDwell = true;

        [Header("Resaltado")]
        public Renderer highlightRenderer;
        public Graphic highlightGraphic;
        public Color highlightColor = new Color(0.22f, 0.95f, 0.54f);

        [Header("Eventos")]
        public UnityEvent onEnter = new UnityEvent();
        public UnityEvent onExit = new UnityEvent();
        public UnityEvent onClick = new UnityEvent();

        public bool IsGazed { get; private set; }

        Color baseColor;
        bool hasBase;
        MaterialPropertyBlock mpb;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        protected virtual void Awake()
        {
            if (highlightRenderer == null && highlightGraphic == null) highlightRenderer = GetComponent<Renderer>();
            CaptureBaseColor();
        }

        void CaptureBaseColor()
        {
            if (highlightGraphic != null) { baseColor = highlightGraphic.color; hasBase = true; }
            else if (highlightRenderer != null && highlightRenderer.sharedMaterial != null)
            {
                var m = highlightRenderer.sharedMaterial;
                baseColor = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.white;
                hasBase = true;
            }
        }

        /// <summary>Cambia el color "normal" (p. ej. al cambiar de estado).</summary>
        public void SetBaseColor(Color c)
        {
            baseColor = c; hasBase = true;
            ApplyColor(IsGazed && interactable ? highlightColor : baseColor);
        }

        void ApplyColor(Color c)
        {
            if (!hasBase) return;
            if (highlightGraphic != null) { highlightGraphic.color = c; return; }
            if (highlightRenderer == null) return;
            mpb ??= new MaterialPropertyBlock();
            highlightRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, c);
            mpb.SetColor(BaseColorId, c);
            highlightRenderer.SetPropertyBlock(mpb);
        }

        // ----- Mensajes de la retícula -----
        public void OnPointerEnter()
        {
            if (!interactable) return;
            IsGazed = true;
            ApplyColor(highlightColor);
            AudioManager.P(Sfx.Hover);
            onEnter?.Invoke();
            Entered();
        }

        public void OnPointerExit()
        {
            IsGazed = false;
            ApplyColor(baseColor);
            onExit?.Invoke();
            Exited();
        }

        public void OnPointerClick()
        {
            if (!interactable) return;
            AudioManager.P(Sfx.Click);
            onClick?.Invoke();
            Clicked();
        }

        protected virtual void Entered() { }
        protected virtual void Exited() { }
        protected virtual void Clicked() { }
    }
}
