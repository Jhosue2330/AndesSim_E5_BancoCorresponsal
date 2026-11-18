using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Decide cuándo fallan las pantallas: cuando entran las órdenes fraudulentas que se pagarán
    /// (la "mano" de Lazarus en el sistema) y todas juntas cuando salta la alerta.
    /// </summary>
    public class GlitchDirector : MonoBehaviour
    {
        public OrderFeed feed;
        public ScreenGlitch[] screens;
        bool alerted;

        void Start()
        {
            if (feed == null) return;
            feed.OrderArrived += OnArrived;
            feed.AlertTriggered += (_, __) => All(0.7f);
        }

        void OnDestroy() { if (feed != null) feed.OrderArrived -= OnArrived; }

        void OnArrived(OrderInfo o)
        {
            // Las órdenes que en 2016 se pagaron entran "con interferencia".
            if (o.realFate != OrderFate.Pagada || screens == null || screens.Length == 0) return;
            int i = Mathf.Clamp((o.number - 1) / Mathf.Max(1, feed.rowsPerScreen), 0, screens.Length - 1);
            if (screens[i]) screens[i].Trigger(0.35f);
        }

        public void All(float seconds)
        {
            if (alerted) return;
            alerted = true;
            if (screens != null) foreach (var s in screens) if (s) s.Trigger(seconds);
        }
    }
}
