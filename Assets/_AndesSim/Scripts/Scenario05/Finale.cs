using System.Collections;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Final cinematográfico: el holograma de la mesa crece hasta ocupar toda la sala y el jugador
    /// queda parado en medio de las rutas del dinero mientras se repite el viaje
    /// (rojo hacia Manila y Sri Lanka en 2016; verde y detenido con defensas).
    /// La cámara no se mueve: es el mundo el que cambia (cómodo en VR).
    /// </summary>
    public class Finale : MonoBehaviour
    {
        public Transform holoContent;     // mapa + nodos + rutas
        public FlowMap flow;
        public float growScale = 4.2f;
        public float holdSeconds = 6f;

        public bool Playing { get; private set; }

        public IEnumerator Play()
        {
            if (holoContent == null) yield break;
            Playing = true;
            var head = UIManager.I ? UIManager.I.head : null;
            Vector3 p0 = holoContent.position, s0 = holoContent.localScale;
            Vector3 center = head ? new Vector3(head.position.x, 0.06f, head.position.z) + Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized * 1.2f : p0;
            bool attack = GameManager.I == null || GameManager.I.mode == SimMode.Ataque;
            FxManager.Title("EL VIAJE DEL DINERO", attack ? "Así salieron US$101 M en 2016" : "Con defensas, el dinero nunca salió", attack ? Palette.Threat : Palette.Secure);
            AudioManager.P(Sfx.Whoosh);
            yield return Ease.Tween(2.2f, k =>
            {
                holoContent.position = Vector3.Lerp(p0, center, k);
                holoContent.localScale = Vector3.Lerp(s0, s0 * growScale, k);
            }, Ease.InOutSine, unscaled: false);
            if (flow) StartCoroutine(flow.Replay(attack));
            yield return new WaitForSeconds(holdSeconds);
            yield return Ease.Tween(1.4f, k =>
            {
                holoContent.position = Vector3.Lerp(center, p0, k);
                holoContent.localScale = Vector3.Lerp(s0 * growScale, s0, k);
            }, Ease.InOutSine, unscaled: false);
            Playing = false;
        }
    }
}
