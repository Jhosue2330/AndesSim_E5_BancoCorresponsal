using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Hace llegar las órdenes a las 3 pantallas con un temporizador, aplica el screening de sanciones
    /// y (en Defensa) las reglas por destino y monto. Lleva los totales.
    /// </summary>
    public class OrderFeed : MonoBehaviour
    {
        [Header("Referencias")]
        public SanctionsMatcher matcher;
        public PaymentRules rules;
        public AlertBeacon alert;
        [Tooltip("Contenedor de filas de cada pantalla (RectTransform dentro de un Canvas World Space).")]
        public RectTransform[] screens = new RectTransform[3];
        public TMP_Text clockText;
        public TMP_Text totalsText;

        [Header("Filas")]
        public int rowsPerScreen = 12;
        public float rowHeight = 64f;
        public float rowSpacing = 8f;
        public float fontSize = 30f;
        [Tooltip("Segundos que tarda en pagarse una orden sin alertas.")]
        public float settleSeconds = 1.2f;

        public event Action<OrderInfo> OrderChanged;
        public event Action<OrderInfo, string> AlertTriggered;
        public event Action FeedFinished;

        public bool Running { get; private set; }
        public bool Finished { get; private set; }
        public List<OrderInfo> Orders { get; private set; } = new List<OrderInfo>();

        CorresponsalScenarioData data;
        readonly Dictionary<OrderInfo, OrderRowView> rows = new Dictionary<OrderInfo, OrderRowView>();
        readonly HashSet<string> seenBeneficiaries = new HashSet<string>();
        DateTime simTime = new DateTime(2016, 2, 4, 20, 36, 0);

        void Start()
        {
            data = GameManager.I ? GameManager.I.data as CorresponsalScenarioData : null;
            if (data == null) { Debug.LogError("[OrderFeed] GameManager.data debe ser un CorresponsalScenarioData."); return; }
            // Copias para no modificar el asset durante el juego.
            foreach (var o in data.orders)
                Orders.Add(new OrderInfo
                {
                    number = o.number, reference = o.reference, beneficiary = o.beneficiary, beneficiaryBank = o.beneficiaryBank,
                    address = o.address, country = o.country, amountMillions = o.amountMillions, realFate = o.realFate
                });
            UpdateTexts();
        }

        public void StartFeed()
        {
            if (Running || Finished || data == null) return;
            StartCoroutine(Feed());
        }

        IEnumerator Feed()
        {
            Running = true;
            int shown = 0;
            foreach (var o in Orders)
            {
                simTime = simTime.AddMinutes(2.5);
                if (o.realFate == OrderFate.NoReenviada)
                {
                    o.status = OrderStatus.NoReenviada;
                    o.flagReason = "Rechazada por formato y nunca reenviada por los atacantes.";
                    CreateRow(o, shown++);
                    Changed(o);
                    continue;
                }

                o.status = OrderStatus.EnTransito;
                CreateRow(o, shown++);
                AudioManager.P(Sfx.OrderIn, 0.6f);

                string term = matcher ? matcher.Match(o, data.sanctionsList) : null;
                string rule = rules ? rules.Evaluate(o, data, seenBeneficiaries) : null;
                seenBeneficiaries.Add(o.beneficiary);

                if (term != null)
                {
                    o.flagReason = $"Coincidencia con la lista de sanciones: «{term}».";
                    SetStatus(o, OrderStatus.EnRevision);
                    if (alert != null && !alert.Triggered)
                    {
                        string why = SanctionsMatcher.Explain(o, term);
                        alert.Trigger(term, why);
                        UIManager.Msg("ALERTA · LISTA DE SANCIONES", why + "\n\nMira las órdenes EN REVISIÓN y toca para bloquearlas.", 9f);
                        AlertTriggered?.Invoke(o, term);
                    }
                    StartCoroutine(AutoFreeze(o, OrderStatus.Bloqueada));
                }
                else if (rule != null)
                {
                    o.flagReason = rule;
                    SetStatus(o, OrderStatus.EnRevision);
                    StartCoroutine(AutoFreeze(o, OrderStatus.Retenida));
                }
                else
                {
                    StartCoroutine(Settle(o));
                }

                yield return new WaitForSeconds(data.secondsBetweenOrders);
            }
            // Espera a que se resuelvan las órdenes en revisión.
            yield return new WaitForSeconds(Mathf.Max(settleSeconds, data.reviewSeconds) + 0.5f);
            simTime = new DateTime(2016, 2, 5, 3, 59, 0);
            Running = false;
            Finished = true;
            UpdateTexts();
            FeedFinished?.Invoke();
        }

        IEnumerator Settle(OrderInfo o)
        {
            yield return new WaitForSeconds(settleSeconds);
            if (o.status == OrderStatus.EnTransito) SetStatus(o, OrderStatus.Pagada);
        }

        IEnumerator AutoFreeze(OrderInfo o, OrderStatus finalStatus)
        {
            yield return new WaitForSeconds(data.reviewSeconds);
            if (o.status == OrderStatus.EnRevision) SetStatus(o, finalStatus);
        }

        /// <summary>El jugador mira y toca una orden.</summary>
        public void PlayerBlock(OrderInfo o)
        {
            switch (o.status)
            {
                case OrderStatus.EnRevision:
                    o.blockedByPlayer = true;
                    // Detenida por sanciones = Bloqueada; detenida por las reglas de defensa = Retenida.
                    bool byRule = o.flagReason != null && o.flagReason.StartsWith("Regla");
                    SetStatus(o, byRule ? OrderStatus.Retenida : OrderStatus.Bloqueada);
                    AudioManager.P(Sfx.Defense, 0.6f);
                    break;
                case OrderStatus.EnTransito:
                    UIManager.Msg($"ORDEN #{o.number:00} · SIN ALERTA",
                        "No hay ninguna regla que la detenga: sin límites por destino ni por monto inusual, el sistema la trata como válida y la paga.", 5f);
                    AudioManager.P(Sfx.Fail, 0.5f);
                    break;
                case OrderStatus.Pagada:
                    UIManager.Msg($"ORDEN #{o.number:00} · YA PAGADA",
                        "El dinero ya salió. Solo el BANCO ANDINO puede confirmar que es un fraude: llama por teléfono.", 5f);
                    AudioManager.P(Sfx.Fail, 0.5f);
                    break;
                case OrderStatus.NoReenviada:
                    UIManager.Msg($"ORDEN #{o.number:00}", o.flagReason, 4f);
                    break;
                default:
                    UIManager.Msg($"ORDEN #{o.number:00}", "Esta orden ya está detenida.", 2.5f);
                    break;
            }
        }

        /// <summary>Tras la llamada de verificación: lo pagado pasa a retenido y lo pendiente se bloquea.</summary>
        public int RecallAfterVerification()
        {
            int recalled = 0;
            foreach (var o in Orders)
            {
                if (o.status == OrderStatus.Pagada || o.status == OrderStatus.EnTransito) { SetStatus(o, OrderStatus.Retenida); recalled++; }
                else if (o.status == OrderStatus.EnRevision) SetStatus(o, OrderStatus.Bloqueada);
            }
            return recalled;
        }

        public void SetSimTime(DateTime t) { simTime = t; UpdateTexts(); }

        void SetStatus(OrderInfo o, OrderStatus s)
        {
            o.status = s;
            Changed(o);
        }

        void Changed(OrderInfo o)
        {
            if (rows.TryGetValue(o, out var r)) r.Refresh();
            UpdateTexts();
            OrderChanged?.Invoke(o);
        }

        // ---------- totales ----------
        public float Sum(params OrderStatus[] states)
        {
            float s = 0f;
            foreach (var o in Orders) if (Array.IndexOf(states, o.status) >= 0) s += o.amountMillions;
            return s;
        }

        public int Count(params OrderStatus[] states)
        {
            int c = 0;
            foreach (var o in Orders) if (Array.IndexOf(states, o.status) >= 0) c++;
            return c;
        }

        public float TotalAmount { get { float s = 0; foreach (var o in Orders) s += o.amountMillions; return s; } }
        public int BlockedByPlayer { get { int c = 0; foreach (var o in Orders) if (o.blockedByPlayer) c++; return c; } }

        void UpdateTexts()
        {
            if (clockText) clockText.text = FormatDate(simTime);
            if (totalsText)
                totalsText.text =
                    $"<color=#ff8a80>PAGADAS {Count(OrderStatus.Pagada)} · US$ {Sum(OrderStatus.Pagada):0} M</color>   " +
                    $"<color=#7dffb0>DETENIDAS {Count(OrderStatus.Bloqueada, OrderStatus.Retenida)} · US$ {Sum(OrderStatus.Bloqueada, OrderStatus.Retenida):0} M</color>   " +
                    $"<color=#ffd166>EN REVISIÓN {Count(OrderStatus.EnRevision)}</color>";
        }

        static readonly string[] Days = { "DOM", "LUN", "MAR", "MIÉ", "JUE", "VIE", "SÁB" };
        static readonly string[] Months = { "ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO", "SEP", "OCT", "NOV", "DIC" };

        /// <summary>Fecha en español sin depender de la cultura del celular.</summary>
        public static string FormatDate(DateTime t) =>
            $"{Days[(int)t.DayOfWeek]} {t.Day:00} {Months[t.Month - 1]} {t.Year} · {t:HH:mm}";

        // ---------- filas en pantalla ----------
        void CreateRow(OrderInfo o, int index)
        {
            int screenIdx = Mathf.Min(index / rowsPerScreen, screens.Length - 1);
            var parent = screens[screenIdx];
            if (parent == null) return;
            int slot = index - screenIdx * rowsPerScreen;
            float width = parent.rect.width;

            var go = new GameObject($"Orden_{o.number:00}", typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, rowHeight);
            rt.anchoredPosition = new Vector2(0, -slot * (rowHeight + rowSpacing));

            var bg = go.GetComponent<Image>();
            bg.color = OrderRowView.CTransit;

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(width, rowHeight, 4f);
            col.center = new Vector3(0, -rowHeight * 0.5f, 0);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.layer = go.layer;
            var trt = (RectTransform)textGo.transform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 0); trt.offsetMax = new Vector2(-16, 0);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.86f, 0.97f, 0.9f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            tmp.characterSpacing = 1.5f;

            var view = go.AddComponent<OrderRowView>();
            view.label = tmp;
            view.background = bg;
            view.Bind(o, this);
            rows[o] = view;
        }
    }
}
