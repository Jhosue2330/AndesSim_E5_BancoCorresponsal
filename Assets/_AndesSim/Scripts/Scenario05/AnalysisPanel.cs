using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Pantalla de ANÁLISIS técnico: explica por qué funcionó el ataque (vector y 3 fallas)
    /// y qué control faltó en cada caso. Se abre desde la pantalla final con «VER ANÁLISIS».
    /// Orden pedido por el curso: Ataque → Análisis → Defensa.
    /// </summary>
    public class AnalysisPanel : MonoBehaviour
    {
        [Tooltip("Raíz del panel (Canvas World Space). Empieza oculto.")]
        public GameObject root;
        public float distance = 2.6f;

        void Awake()
        {
            if (root) root.SetActive(false);
        }

        /// <summary>Muestra el análisis frente al jugador y oculta la pantalla final.</summary>
        public void Show()
        {
            if (root == null) return;
            var ui = UIManager.I;
            if (ui != null)
            {
                ui.HideMessage();
                if (ui.endScreenRoot) ui.endScreenRoot.SetActive(false);
            }
            root.SetActive(true);
            PlaceInFront(root.transform);
            AudioManager.P(Sfx.Evidence);
        }

        /// <summary>Vuelve a la pantalla final.</summary>
        public void Back()
        {
            if (root) root.SetActive(false);
            var ui = UIManager.I;
            if (ui != null && ui.endScreenRoot)
            {
                ui.endScreenRoot.SetActive(true);
                PlaceInFront(ui.endScreenRoot.transform);
            }
        }

        void PlaceInFront(Transform t)
        {
            var head = UIManager.I != null ? UIManager.I.head : null;
            if (head == null) return;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            t.position = head.position + fwd * distance;
            t.rotation = Quaternion.LookRotation(fwd);
        }
    }
}
