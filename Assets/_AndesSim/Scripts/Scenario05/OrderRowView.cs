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

        // Fondos oscuros teñidos por la paleta: el color dice el estado, el texto se sigue leyendo.
        public static readonly Color CTransit = Mix(Palette.Data, 0.16f);
        public static readonly Color CPaid = Mix(Palette.Threat, 0.42f);
        public static readonly Color CReview = Mix(Palette.Warning, 0.38f);
        public static readonly Color CBlocked = Mix(Palette.Secure, 0.30f);
        public static readonly Color CHeld = Mix(Palette.Data, 0.34f);
        public static readonly Color CNone = Mix(Palette.InkDim, 0.18f);

        static Color Mix(Color c, float k) { var m = Color.Lerp(Palette.Panel, c, k); m.a = 0.95f; return m; }

        static readonly string W = Palette.Hex(Palette.Warning), G = Palette.Hex(Palette.Secure), D = Palette.Hex(Palette.Data), M = Palette.Hex(Palette.InkDim);

        OrderStatus? lastStatus;

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
                case OrderStatus.Pagada: state = $"<color={Palette.Hex(Palette.Threat)}>PAGADA</color>"; c = CPaid; break;
                case OrderStatus.EnRevision: state = $"<color={W}>EN REVISIÓN</color>"; c = CReview; break;
                case OrderStatus.Bloqueada: state = order.blockedByPlayer ? $"<color={G}>BLOQUEADA (tú)</color>" : $"<color={G}>CONGELADA</color>"; c = CBlocked; break;
                case OrderStatus.Retenida: state = order.blockedByPlayer ? $"<color={D}>RETENIDA (tú)</color>" : $"<color={D}>RETENIDA</color>"; c = CHeld; break;
                case OrderStatus.NoReenviada: state = $"<color={M}>NO REENVIADA</color>"; c = CNone; break;
                default: state = "EN TRÁNSITO"; c = CTransit; break;
            }
            string amount = order.amountMillions > 0 ? order.AmountText : "—";
            if (label) label.text = $"<b>#{order.number:00}</b><pos=7%>{amount}<pos=25%>{Trunc(order.beneficiary, 24)}<pos=62%><size=80%>{order.country}</size><pos=78%>{state}";
            SetBaseColor(c);
            if (lastStatus.HasValue && lastStatus.Value != order.status && isActiveAndEnabled) StartCoroutine(Flash(c));
            lastStatus = order.status;
        }

        System.Collections.IEnumerator Flash(Color c)
        {
            var rt = (RectTransform)transform;
            yield return Ease.Tween(0.45f, k =>
            {
                if (background) background.color = Color.Lerp(Color.Lerp(c, Color.white, 0.55f), c, k);
                rt.localScale = new Vector3(1f, 1f + 0.12f * (1f - k), 1f);
            }, Ease.OutCubic, unscaled: false);
            SetBaseColor(c);
        }

        static string Trunc(string s, int n) => string.IsNullOrEmpty(s) || s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        protected override void Entered()
        {
            if (order == null || order.status == OrderStatus.EnTransito || order.status == OrderStatus.NoReenviada) return;
            UIManager.Msg($"EXPEDIENTE · ORDEN #{order.number:00} · {order.AmountText}", Dossier(), 4f);
        }

        /// <summary>Ficha de la orden con sus "banderas rojas" (lo que un analista debería notar).</summary>
        string Dossier()
        {
            var flags = new System.Collections.Generic.List<string> { "beneficiario nunca usado" };
            if (order.amountMillions >= 5f) flags.Add("monto inusual para una persona o fundación");
            if (order.address.ToUpperInvariant().Contains("JUPITER")) flags.Add("«JUPITER» en la dirección (lista de sanciones)");
            if (order.beneficiary.ToUpperInvariant().Contains("FANDATION")) flags.Add("nombre mal escrito: «FANDATION»");
            string action = order.status == OrderStatus.EnRevision ? $"<color={W}>›› Toca para BLOQUEAR</color>"
                : IsFandation ? $"<color={W}>›› Toca para examinar el nombre con la LUPA</color>" : "";
            return $"<b>{order.beneficiary}</b> · {order.beneficiaryBank}\n{order.address} ({order.country})\n" +
                   $"<color={Palette.Hex(Palette.Threat)}>Banderas rojas:</color> " + string.Join(" · ", flags) + "\n" + action;
        }

        bool IsFandation => order != null && order.beneficiary.ToUpperInvariant().Contains("FANDATION");

        protected override void Clicked()
        {
            if (order == null) return;
            if (IsFandation && order.status != OrderStatus.EnRevision && order.status != OrderStatus.EnTransito && FandationGame.I != null)
            {
                FandationGame.I.Open(order);
                return;
            }
            if (feed != null) feed.PlayerBlock(order);
        }
    }
}
