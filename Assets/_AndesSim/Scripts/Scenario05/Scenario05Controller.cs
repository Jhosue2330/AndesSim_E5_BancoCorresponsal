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
            yield return new WaitForSeconds(3.8f);
            UIManager.Msg("VIERNES 5 FEB · 03:59",
                "Terminó la noche. Hay órdenes pagadas que nadie confirmó.\n" +
                "Ve al ESCRITORIO (derecha) y usa el TELÉFONO para llamar al BANCO ANDINO.", 9f);
        }

        void OnCallFinished(bool verified)
        {
            if (phoneHint) phoneHint.SetActive(false);
            StartCoroutine(ShowFinish());
        }

        IEnumerator ShowFinish()
        {
            yield return new WaitForSeconds(2f);
            if (finishButton) finishButton.gameObject.SetActive(true);
        }

        public void Finish()
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
