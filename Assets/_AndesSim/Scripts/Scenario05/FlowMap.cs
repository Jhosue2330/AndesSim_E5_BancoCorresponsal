using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Mesa holográfica: muestra el viaje del dinero.
    /// Cada orden sale del BANCO ANDINO hacia el Banco Corresponsal (pulso cian).
    /// Si se paga, sigue en ROJO hasta Manila o Sri Lanka y suma al contador del destino.
    /// Si se bloquea o retiene, el escudo del corresponsal destella en VERDE.
    /// </summary>
    public class FlowMap : MonoBehaviour
    {
        public OrderFeed feed;
        [Header("Nodos")]
        public Transform andino;
        public Transform corresponsal;
        public Transform manila;
        public Transform sriLanka;
        [Header("Visual")]
        public Renderer shield;
        public TMP_Text manilaAmount;
        public TMP_Text sriLankaAmount;
        public TMP_Text corresponsalAmount;
        public Material pulseMaterial;
        public float arcHeight = 0.28f;
        public float hopSeconds = 1.1f;
        public float pulseSize = 0.07f;

        class Pulse { public Transform t; public Renderer r; public Vector3 a, b; public float k, dur; public OrderInfo order; public bool toDest, counts; }

        readonly List<Pulse> active = new List<Pulse>();
        readonly Stack<Pulse> pool = new Stack<Pulse>();
        readonly Dictionary<OrderInfo, bool> settled = new Dictionary<OrderInfo, bool>();
        MaterialPropertyBlock mpb;
        float shieldFlash; Color shieldColor = Palette.Data;
        float manilaTotal, sriTotal, heldTotal, manilaShown, sriShown, heldShown;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        void Start()
        {
            if (feed == null) return;
            feed.OrderArrived += OnArrived;
            feed.OrderChanged += OnChanged;
            UpdateLabels(true);
        }

        void OnDestroy()
        {
            if (feed == null) return;
            feed.OrderArrived -= OnArrived;
            feed.OrderChanged -= OnChanged;
        }

        void OnArrived(OrderInfo o)
        {
            if (o.status == OrderStatus.NoReenviada) return;
            Launch(o, andino.position, corresponsal.position, Palette.Data, false);
        }

        void OnChanged(OrderInfo o)
        {
            bool wasPaid = settled.TryGetValue(o, out var paid) && paid;
            switch (o.status)
            {
                case OrderStatus.Pagada:
                    if (settled.ContainsKey(o)) return;
                    settled[o] = true;
                    Transform dest = o.country == "Sri Lanka" ? sriLanka : manila;
                    Launch(o, corresponsal.position, dest.position, Palette.Threat, true);
                    break;
                case OrderStatus.Bloqueada:
                case OrderStatus.Retenida:
                    if (wasPaid)
                    {   // recuperada tras la llamada de verificación
                        if (o.country == "Sri Lanka") sriTotal -= o.amountMillions; else manilaTotal -= o.amountMillions;
                    }
                    if (settled.ContainsKey(o) && !wasPaid) return;
                    settled[o] = false;
                    heldTotal += o.amountMillions;
                    Flash(Palette.Secure);
                    AudioManager.P(Sfx.Shield, 0.5f);
                    break;
                case OrderStatus.EnRevision:
                    Flash(Palette.Warning);
                    break;
            }
        }

        void Launch(OrderInfo o, Vector3 a, Vector3 b, Color c, bool toDest, bool counts = true)
        {
            var p = pool.Count > 0 ? pool.Pop() : MakePulse();
            p.a = a; p.b = b; p.k = 0f; p.dur = hopSeconds; p.order = o; p.toDest = toDest; p.counts = counts && o != null;
            p.t.gameObject.SetActive(true);
            p.r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c * 1.6f);
            mpb.SetColor("_TintColor", c);
            p.r.SetPropertyBlock(mpb);
            active.Add(p);
        }

        Pulse MakePulse()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Pulso";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * pulseSize * 2.2f;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = pulseMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new Pulse { t = go.transform, r = r };
        }

        void Update()
        {
            var cam = Camera.main ? Camera.main.transform : null;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.k += Time.deltaTime / p.dur;
                float k = Mathf.Clamp01(p.k);
                Vector3 pos = Vector3.Lerp(p.a, p.b, k) + Vector3.up * arcHeight * 4f * k * (1f - k);
                p.t.position = pos;
                if (cam) p.t.rotation = Quaternion.LookRotation(pos - cam.position);
                if (p.k >= 1f)
                {
                    if (p.toDest && p.counts)
                    {
                        if (p.order.country == "Sri Lanka") sriTotal += p.order.amountMillions; else manilaTotal += p.order.amountMillions;
                        AudioManager.At(Sfx.Paid, pos, 0.8f);
                        Haptics.Warn();
                        FxManager.I?.Burst(pos, Palette.Threat, 8, 0.6f);
                    }
                    p.t.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    pool.Push(p);
                }
            }

            if (shield)
            {
                shieldFlash = Mathf.MoveTowards(shieldFlash, 0f, Time.deltaTime * 1.5f);
                var baseC = AmbienceController.I != null && AmbienceController.I.Mood == RoomMood.Secure ? Palette.Secure : Palette.Data;
                shield.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", Color.Lerp(baseC * 0.35f, shieldColor * 1.5f, shieldFlash));
                shield.SetPropertyBlock(mpb);
                float s = 1f + 0.25f * shieldFlash;
                shield.transform.localScale = new Vector3(s, shield.transform.localScale.y, s);
            }
            UpdateLabels(false);
        }

        void Flash(Color c) { shieldColor = c; shieldFlash = 1f; }

        /// <summary>Repite el viaje del dinero para el final (no cambia los contadores).</summary>
        public System.Collections.IEnumerator Replay(bool attack)
        {
            for (int i = 0; i < 6; i++)
            {
                Launch(null, andino.position, corresponsal.position, Palette.Data, false, false);
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(hopSeconds * 0.8f);
            if (attack)
            {
                for (int i = 0; i < 4; i++) { Launch(null, corresponsal.position, manila.position, Palette.Threat, true, false); yield return new WaitForSeconds(0.25f); }
                Launch(null, corresponsal.position, sriLanka.position, Palette.Threat, true, false);
            }
            else
            {
                Flash(Palette.Secure);
                FxManager.I?.Burst(corresponsal.position + Vector3.up * 0.3f, Palette.Secure, 40, 2.4f);
                AudioManager.At(Sfx.Shield, corresponsal.position, 1.3f);
            }
        }

        void UpdateLabels(bool snap)
        {
            float rate = snap ? 1e6f : Time.deltaTime * 40f;
            manilaShown = Mathf.MoveTowards(manilaShown, manilaTotal, rate);
            sriShown = Mathf.MoveTowards(sriShown, sriTotal, rate);
            heldShown = Mathf.MoveTowards(heldShown, heldTotal, rate);
            if (manilaAmount) manilaAmount.text = $"MANILA\n<color={Palette.Hex(Palette.Threat)}>US$ {manilaShown:0} M</color>";
            if (sriLankaAmount) sriLankaAmount.text = $"SRI LANKA\n<color={Palette.Hex(Palette.Threat)}>US$ {sriShown:0} M</color>";
            if (corresponsalAmount) corresponsalAmount.text = $"CORRESPONSAL\n<color={Palette.Hex(Palette.Secure)}>detenido US$ {heldShown:0} M</color>";
        }
    }
}
