using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.Scenario05
{
    /// <summary>Fila de una orden en una pantalla de monitoreo. Mirar y tocar = intentar bloquearla.</summary>
    public class OrderRowView : GazeInteractable
    {
        public OrderInfo order;
        public OrderFeed feed;
        public TMP_Text label;
        public Image background;

        public static readonly Color CTransit = new Color(0.10f, 0.16f, 0.13f, 0.95f);
        public static readonly Color CPaid = new Color(0.55f, 0.12f, 0.10f, 0.95f);
        public static readonly Color CReview = new Color(0.55f, 0.40f, 0.05f, 0.95f);
        public static readonly Color CBlocked = new Color(0.07f, 0.38f, 0.20f, 0.95f);
        public static readonly Color CHeld = new Color(0.05f, 0.30f, 0.40f, 0.95f);
        public static readonly Color CNone = new Color(0.18f, 0.20f, 0.19f, 0.95f);

        public void Bind(OrderInfo o, OrderFeed f)
        {
            order = o; feed = f;
            highlightGraphic = background;
            allowDwell = true;
            Refresh();
        }

        public void Refresh()
        {
            if (order == null) return;
            string state; Color c;
            switch (order.status)
            {
                case OrderStatus.Pagada: state = "<color=#ff8a80>PAGADA</color>"; c = CPaid; break;
                case OrderStatus.EnRevision: state = "<color=#ffd166>EN REVISIÓN</color>"; c = CReview; break;
                case OrderStatus.Bloqueada: state = order.blockedByPlayer ? "<color=#7dffb0>BLOQUEADA (tú)</color>" : "<color=#7dffb0>CONGELADA</color>"; c = CBlocked; break;
                case OrderStatus.Retenida: state = order.blockedByPlayer ? "<color=#8fe3ff>RETENIDA (tú)</color>" : "<color=#8fe3ff>RETENIDA</color>"; c = CHeld; break;
                case OrderStatus.NoReenviada: state = "<color=#9aa59f>NO REENVIADA</color>"; c = CNone; break;
                default: state = "EN TRÁNSITO"; c = CTransit; break;
            }
            string amount = order.amountMillions > 0 ? order.AmountText : "—";
            if (label) label.text = $"<b>#{order.number:00}</b><pos=7%>{amount}<pos=25%>{Trunc(order.beneficiary, 24)}<pos=62%><size=80%>{order.country}</size><pos=78%>{state}";
            SetBaseColor(c);
        }

        static string Trunc(string s, int n) => string.IsNullOrEmpty(s) || s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        protected override void Entered()
        {
            if (order != null && !string.IsNullOrEmpty(order.flagReason) && order.status == OrderStatus.EnRevision)
                UIManager.Msg($"ORDEN #{order.number:00} · {order.AmountText}", order.flagReason + "\nToca para BLOQUEAR.", 3f);
        }

        protected override void Clicked()
        {
            if (feed != null && order != null) feed.PlayerBlock(order);
        }
    }
}
