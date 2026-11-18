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
            if (feed) feed.AlertTriggered += (_, __) => StartCoroutine(SayLater(2.0f, "¡Alerta! Bloquea las órdenes ámbar."));
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
            FxManager.Title("4 FEB 2016 · 20:36", "SALA DE MONITOREO SWIFT", defense ? Palette.Secure : Palette.Data);
            StartCoroutine(IntroMessage(defense));
            SupervisorHolo.Say("Bienvenido, analista. Vigila el dinero.", 1.2f);
            SupervisorHolo.Say(defense
                ? "Activa las 3 defensas. ¡A tu izquierda!"
                : "Toca INICIAR para empezar.", 2f);
        }

        IEnumerator SayLater(float delay, string text)
        {
            yield return new WaitForSeconds(delay);
            SupervisorHolo.Say(text, 2.5f);
        }

        IEnumerator IntroMessage(bool defense)
        {
            yield break; // la supervisora ya lo dice (poco texto)
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
            FxManager.Title("VIE 5 FEB · 03:59", paid > 0 ? $"−US$ {paid:0} M" : "US$ 0 perdidos",
                paid > 0 ? Palette.Threat : Palette.Secure);
            SupervisorHolo.Say(paid > 0
                ? "Salió dinero. ¡Llama al banco!"
                : "Todo detenido. Confirma por teléfono.", 2f);
            yield break;
        }

        void OnCallFinished(bool verified)
        {
            if (phoneHint) phoneHint.SetActive(false);
            SupervisorHolo.Say(verified
                ? "¡Fraude confirmado! Todo retenido."
                : "Nadie contesta. Es fin de semana.", 2.5f);
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
