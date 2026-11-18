using TMPro;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Tablero de la mesa holográfica: contadores grandes que cuentan (nunca saltan)
    /// el dinero en tránsito, pagado (perdido) y detenido. Cada cambio hace un "pop".
    /// </summary>
    public class StatsBoard : MonoBehaviour
    {
        public OrderFeed feed;
        public TMP_Text paidValue, heldValue, reviewValue, totalValue;
        public float countSpeed = 60f; // millones por segundo

        float paid, held, review, total;
        float pPop, hPop, rPop;

        void Start()
        {
            if (feed != null) feed.OrderChanged += _ => Refresh();
            Refresh();
        }

        void Refresh()
        {
            if (feed == null) return;
            float np = feed.Sum(OrderStatus.Pagada);
            float nh = feed.Sum(OrderStatus.Bloqueada, OrderStatus.Retenida);
            float nr = feed.Sum(OrderStatus.EnRevision, OrderStatus.EnTransito);
            if (np > tp) pPop = 1f;
            if (nh > th) hPop = 1f;
            if (nr > tr) rPop = 1f;
            tp = np; th = nh; tr = nr;
            tt = np + nh + nr;
        }

        float tp, th, tr, tt;

        void Update()
        {
            float step = countSpeed * Time.deltaTime;
            paid = Mathf.MoveTowards(paid, tp, step);
            held = Mathf.MoveTowards(held, th, step);
            review = Mathf.MoveTowards(review, tr, step);
            total = Mathf.MoveTowards(total, tt, step);
            pPop = Mathf.MoveTowards(pPop, 0f, Time.deltaTime * 3f);
            hPop = Mathf.MoveTowards(hPop, 0f, Time.deltaTime * 3f);
            rPop = Mathf.MoveTowards(rPop, 0f, Time.deltaTime * 3f);
            Set(paidValue, paid, pPop);
            Set(heldValue, held, hPop);
            Set(reviewValue, review, rPop);
            if (totalValue) totalValue.text = $"US$ {total:0} M";
        }

        static void Set(TMP_Text t, float v, float pop)
        {
            if (t == null) return;
            t.text = $"US$ {v:0} M";
            t.transform.localScale = Vector3.one * (1f + 0.18f * Ease.OutCubic(pop));
        }
    }
}
