using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// "Rebobinado": la noche completa en una línea de tiempo que se escribe sola,
    /// comparando lo que pasó en 2016 con lo que habría pasado con las defensas.
    /// </summary>
    public class TimelinePanel : MonoBehaviour
    {
        public GameObject root;
        public Typewriter[] columns;
        public float distance = 2.6f;
        [Tooltip("Panel que se oculta al abrir la línea de tiempo (el de Análisis).")]
        public GameObject hideOnShow;

        void Awake() { if (root) root.SetActive(false); }

        public void Show()
        {
            if (root == null) return;
            var ui = UIManager.I;
            if (ui != null) { ui.HideMessage(); if (ui.endScreenRoot) ui.endScreenRoot.SetActive(false); }
            if (hideOnShow) hideOnShow.SetActive(false);
            root.SetActive(true);
            var head = ui ? ui.head : null;
            if (head)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                root.transform.position = head.position + fwd * distance;
                root.transform.rotation = Quaternion.LookRotation(fwd);
            }
            AudioManager.P(Sfx.Whoosh);
            if (columns != null) foreach (var c in columns) if (c) c.Play();
        }

        public void Back()
        {
            if (root) root.SetActive(false);
            var ui = UIManager.I;
            if (ui != null && ui.endScreenRoot)
            {
                ui.endScreenRoot.SetActive(true);
                var head = ui.head;
                if (head)
                {
                    Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                    ui.endScreenRoot.transform.position = head.position + fwd * ui.endDistance;
                    ui.endScreenRoot.transform.rotation = Quaternion.LookRotation(fwd);
                }
            }
        }
    }
}
