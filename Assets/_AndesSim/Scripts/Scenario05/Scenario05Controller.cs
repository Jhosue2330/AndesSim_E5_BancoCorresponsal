using System.Collections;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Orquesta el flujo del Escenario 5:
    /// Portal → sala → (Defensa: activar controles) → Iniciar monitoreo → alerta «JUPITER» →
    /// bloquear órdenes → llamar al BANCO ANDINO → Ver resultado.
    /// </summary>
    public class Scenario05Controller : MonoBehaviour
    {
        public OrderFeed feed;
        public CallbackDefense phone;
        [Tooltip("Botón por mirada que inicia el flujo de órdenes.")]
        public GazeInteractable startButton;
        [Tooltip("Botón por mirada que termina el escenario (se activa después de la llamada).")]
        public GazeInteractable finishButton;
        [Tooltip("Indicador que parpadea sobre el teléfono cuando toca llamar.")]
        public GameObject phoneHint;

        void Start()
        {
            PortalEntrada.Crossed += OnCrossed;
            if (feed) feed.FeedFinished += OnFeedFinished;
            if (phone) phone.CallFinished += OnCallFinished;
            if (feed) feed.AlertTriggered += (_, __) => StartCoroutine(SayLater(2.0f, "¡Alerta de sanciones! Mira las órdenes en ámbar y bloquéalas con la mirada."));
            if (startButton) { startButton.onClick.AddListener(StartMonitoring); startButton.gameObject.SetActive(false); }
            if (finishButton) { finishButton.onClick.AddListener(Finish); finishButton.gameObject.SetActive(false); }
            if (phoneHint) phoneHint.SetActive(false);
        }

        void OnDestroy() => PortalEntrada.Crossed -= OnCrossed;

        void OnCrossed()
        {
            if (startButton) startButton.gameObject.SetActive(true);
            bool defense = GameManager.I != null && GameManager.I.mode == SimMode.Defensa;
            AmbienceController.Set(RoomMood.Calm);
            FxManager.Title("JUEVES 4 FEB 2016 · 20:36", "BANCO CORRESPONSAL INTERNACIONAL · SALA DE MONITOREO", defense ? Palette.Secure : Palette.Data);
            StartCoroutine(IntroMessage(defense));
            SupervisorHolo.Say("Bienvenido, analista. Esta noche entran órdenes SWIFT del BANCO ANDINO. Vigila la mesa holográfica.", 1.5f);
            SupervisorHolo.Say(defense
                ? "Antes de empezar, activa las tres defensas en el panel de la izquierda."
                : "Cuando estés listo, mira el botón verde INICIAR MONITOREO.", 2f);
        }

        IEnumerator SayLater(float delay, string text)
        {
            yield return new WaitForSeconds(delay);
            SupervisorHolo.Say(text, 2.5f);
        }

        IEnumerator IntroMessage(bool defense)
        {
            yield return new WaitForSeconds(3.8f);
            if (defense)
                UIManager.Msg("MODO DEFENSA",
                    "Antes de iniciar el monitoreo, ve al PANEL DE CONTROLES (izquierda) y activa las defensas.\n" +
                    "Luego mira el botón «INICIAR MONITOREO» bajo las pantallas.", 9f);
            else
                UIManager.Msg("MODO ATAQUE · NOCHE DEL 4 DE FEBRERO DE 2016",
                    "Eres analista del Banco Corresponsal Internacional. Mira el botón «INICIAR MONITOREO» bajo las pantallas.\n" +
                    "Explora la sala: hay evidencias en el escritorio y en las paredes.", 9f);
        }

        public void StartMonitoring()
        {
            if (feed == null || feed.Running || feed.Finished) return;
            if (startButton) startButton.gameObject.SetActive(false);
            feed.StartFeed();
        }

        void OnFeedFinished() => StartCoroutine(FeedFinishedRoutine());

        IEnumerator FeedFinishedRoutine()
        {
            if (phoneHint) phoneHint.SetActive(true);
            float paid = feed ? feed.Sum(OrderStatus.Pagada) : 0f;
            FxManager.Title("VIERNES 5 FEB · 03:59", paid > 0 ? $"US$ {paid:0} M ya salieron del corresponsal" : "Ninguna orden salió del corresponsal",
                paid > 0 ? Palette.Threat : Palette.Secure);
            SupervisorHolo.Say(paid > 0
                ? $"Salieron {paid:0} millones y nadie los confirmó. Llama al BANCO ANDINO desde el escritorio."
                : "Ninguna orden salió. Igual confirma con el BANCO ANDINO por teléfono.", 3f);
            yield return new WaitForSeconds(3.8f);
            UIManager.Msg("VIERNES 5 FEB · 03:59",
                "Terminó la noche. Hay órdenes pagadas que nadie confirmó.\n" +
                "Ve al ESCRITORIO (derecha) y usa el TELÉFONO para llamar al BANCO ANDINO.", 9f);
        }

        void OnCallFinished(bool verified)
        {
            if (phoneHint) phoneHint.SetActive(false);
            SupervisorHolo.Say(verified
                ? "¡Fraude confirmado! Retuvimos todas las órdenes. Excelente trabajo."
                : "Nadie contesta: es fin de semana en el BANCO ANDINO. Así fue como se perdieron 101 millones.", 3f);
            StartCoroutine(ShowFinish());
        }

        IEnumerator ShowFinish()
        {
            yield return new WaitForSeconds(2f);
            if (finishButton) finishButton.gameObject.SetActive(true);
        }

        [Tooltip("Final cinematográfico (holograma gigante) antes de la pantalla de resultados.")]
        public Finale finale;
        bool finishing;

        public void Finish()
        {
            if (finishing) return;
            finishing = true;
            if (finishButton) finishButton.gameObject.SetActive(false);
            StartCoroutine(FinishRoutine());
        }

        IEnumerator FinishRoutine()
        {
            if (finale) yield return finale.Play();
            // El holograma y la supervisora ya cumplieron: se apagan para no tapar los resultados.
            if (finale && finale.holoContent) finale.holoContent.gameObject.SetActive(false);
            if (SupervisorHolo.I) SupervisorHolo.I.gameObject.SetActive(false);
            FinishNow();
        }

        void FinishNow()
        {
            bool clean = feed != null && feed.Sum(OrderStatus.Pagada) <= 0.01f;
            AudioManager.P(clean ? Sfx.Success : Sfx.Fail);
            AmbienceController.Set(clean ? RoomMood.Secure : RoomMood.Danger);
            var head = UIManager.I ? UIManager.I.head : null;
            if (head && clean) FxManager.I?.Burst(head.position + head.forward * 2f, Palette.Secure, 50, 2.4f);
            GameManager.I?.EndScenario();
        }
    }
}
