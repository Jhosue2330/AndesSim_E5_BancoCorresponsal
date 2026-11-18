using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// MODO DEMO: juega toda la experiencia sola con una cámara que mira suavemente cada momento
    /// clave. Pensado para grabar el video desde la PC (en el visor manda la cabeza del jugador).
    /// Recorrido: Ataque 2016 completo → Análisis → Línea de tiempo → Defensa completa.
    /// </summary>
    public class DemoDirector : MonoBehaviour
    {
        public PortalEntrada portal;
        public Scenario05Controller ctrl;
        public Transform cam;
        public TeleportPoint tpCenter, tpPanel, tpDesk;
        public AnalysisPanel analysis;
        public TimelinePanel timeline;
        public Transform portalAttackButton, portalDefenseButton;

        /// <summary>0 = apagado, 1 = demo del ataque, 2 = demo de la defensa (sobrevive al reinicio de escena).</summary>
        static int stage;
        public static bool Running => stage > 0;

        void Start()
        {
            if (stage > 0) StartCoroutine(stage == 1 ? AttackDemo() : DefenseDemo());
        }

        /// <summary>Botón "DEMO AUTOMÁTICA" del portal.</summary>
        public void Begin()
        {
            if (stage > 0) return;
            stage = 1;
            StartCoroutine(AttackDemo());
        }

        // ---------- cámara ----------
        IEnumerator Look(Vector3 target, float seconds = 1.2f)
        {
            if (cam == null || XRSettings.isDeviceActive) yield break;
            Quaternion from = cam.rotation;
            Quaternion to = Quaternion.LookRotation(target - cam.position);
            yield return Ease.Tween(seconds, k => cam.rotation = Quaternion.Slerp(from, to, k), Ease.InOutSine, unscaled: false);
        }

        IEnumerator LookAt(string objectName, float seconds = 1.2f, Vector3? offset = null)
        {
            var go = GameObject.Find(objectName);
            if (go) yield return Look(go.transform.position + (offset ?? Vector3.zero), seconds);
        }

        static IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        // ---------- recorrido ----------
        IEnumerator Intro(bool defense)
        {
            yield return Wait(1.5f);
            if (portalAttackButton) yield return Look(portalAttackButton.position + Vector3.up * 0.9f, 1.5f);
            yield return Wait(3.5f);
            yield return Look((defense ? portalDefenseButton : portalAttackButton).position, 1f);
            yield return Wait(0.8f);
            if (defense) portal.ChooseDefense(); else portal.ChooseAttack();
            // dentro del ascensor: mirar la ciudad por el vidrio y luego las puertas
            yield return Wait(1.6f);
            yield return Look(cam.position + new Vector3(1f, -0.1f, 0.3f), 1.8f);
            yield return Wait(3.5f);
            yield return Look(cam.position + new Vector3(-1f, 0f, 0f), 1.4f);
            while (GameManager.I == null || !GameManager.I.HasStarted) yield return null;
            yield return Wait(0.5f);
            yield return LookAt("Supervisora_Holograma", 1.2f, Vector3.up * 0.4f);
            yield return Wait(4f);
        }

        IEnumerator Monitoring()
        {
            var feed = ctrl.feed;
            yield return LookAt("Boton_Iniciar", 1f);
            yield return Wait(0.6f);
            ctrl.StartMonitoring();
            yield return LookAt("Pantalla_01", 1.2f);
            yield return Wait(2.5f);
            yield return LookAt("Mesa_Holografica", 1.2f, Vector3.up * 0.9f);
            while (!feed.alert.Triggered) yield return null;
            yield return LookAt("Pantalla_02", 0.8f, Vector3.up * 1.0f);
            yield return Wait(3.5f);
            yield return LookAt("Pantalla_01", 1f);
            int blocked = 0;
            float until = Time.time + 12f;
            while (blocked < 4 && Time.time < until)
            {
                var o = feed.Orders.FirstOrDefault(x => x.status == OrderStatus.EnRevision);
                if (o != null) { feed.PlayerBlock(o); blocked++; }
                yield return Wait(0.9f);
            }
            yield return LookAt("Mesa_Holografica", 1.2f, Vector3.up * 0.9f);
            yield return Wait(2.5f);
        }

        IEnumerator DeskAndCall(bool defense)
        {
            tpDesk.TeleportHere();
            yield return Wait(0.8f);
            foreach (var n in new[] { "Lista_Sanciones", "Directorio_Contactos" })
            {
                yield return LookAt(n, 1f);
                GameObject.Find(n)?.GetComponent<EvidenceItem>()?.OnPointerClick();
                yield return Wait(0.8f);
                yield return Look(cam.position + cam.forward, 0.1f);
                yield return Wait(5.5f);
            }
            var fandation = ctrl.feed.Orders.FirstOrDefault(o => o.beneficiary.Contains("FANDATION"));
            if (!defense && fandation != null && FandationGame.I != null)
            {
                FandationGame.I.Open(fandation);
                yield return Wait(2.5f);
                FandationGame.I.SolveForDemo();
                yield return Wait(3.5f);
                FandationGame.I.Close();
            }
            while (!ctrl.feed.Finished) yield return null;
            yield return Wait(4f);
            yield return LookAt("Telefono", 1f);
            ctrl.phone.OnPointerClick();
            var dialog = ctrl.phone.dialog;
            int step = 0;
            while (!ctrl.phone.CallDone)
            {
                if (dialog && dialog.WaitingChoice)
                {
                    yield return LookAt("UI_Llamada", 0.8f);
                    yield return Wait(2.8f);
                    dialog.Choose(step < 2 ? step : 2);
                    step++;
                }
                yield return null;
            }
            yield return Wait(2.5f);
        }

        IEnumerator Ending(bool defense)
        {
            while (ctrl.finishButton && !ctrl.finishButton.gameObject.activeSelf) yield return null;
            tpCenter.TeleportHere();
            yield return Wait(0.6f);
            yield return LookAt("Pantalla_02", 1f);
            ctrl.Finish();
            while (GameManager.I != null && !GameManager.I.HasEnded) yield return null;
            yield return Wait(0.5f);
            if (UIManager.I && UIManager.I.endScreenRoot) yield return Look(UIManager.I.endScreenRoot.transform.position, 0.8f);
            yield return Wait(7f);
            if (!defense)
            {
                analysis.Show();
                yield return Wait(9f);
                timeline.Show();
                yield return Wait(10f);
                stage = 2;
                GameManager.I.RestartDefense();
            }
            else
            {
                stage = 0; // fin de la demo
            }
        }

        IEnumerator AttackDemo()
        {
            yield return Intro(false);
            yield return Monitoring();
            yield return DeskAndCall(false);
            yield return Ending(false);
        }

        IEnumerator DefenseDemo()
        {
            yield return Intro(true);
            tpPanel.TeleportHere();
            yield return Wait(0.8f);
            foreach (var d in FindObjectsByType<DefenseControl>(FindObjectsSortMode.None).OrderBy(x => x.transform.position.z))
            {
                yield return Look(d.transform.position + Vector3.up * 0.3f, 0.9f);
                d.OnPointerClick();
                yield return Wait(2.2f);
            }
            tpCenter.TeleportHere();
            yield return Wait(0.8f);
            yield return Monitoring();
            yield return DeskAndCall(true);
            yield return Ending(true);
        }
    }
}
