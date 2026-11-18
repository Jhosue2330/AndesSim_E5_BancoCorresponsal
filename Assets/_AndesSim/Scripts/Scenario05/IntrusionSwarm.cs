using System.Collections;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// La intrusión de Lazarus, mostrada de forma abstracta: un enjambre de datos rojos baja por
    /// el techo y entra a las pantallas cada vez que llega una orden fraudulenta que se pagará.
    /// En modo Defensa, si las reglas están activas, un ESCUDO (firewall) sobre la mesa lo detiene.
    /// (Sin personajes ni caricaturas: el adversario es un grupo criminal, se muestra por su efecto.)
    /// </summary>
    public class IntrusionSwarm : MonoBehaviour
    {
        public OrderFeed feed;
        public ParticleSystem swarm;
        public Vector3[] path;            // techo → mesa → pantallas
        public Transform dome;            // escudo firewall
        public Renderer domeRenderer;
        public float travelSeconds = 1.7f;

        MaterialPropertyBlock mpb;
        float domeFlash;
        bool domeUp;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            if (dome) dome.localScale = Vector3.zero;
            if (swarm) { var e = swarm.emission; e.enabled = false; }
            if (feed) feed.OrderArrived += OnArrived;
        }

        void OnDestroy() { if (feed) feed.OrderArrived -= OnArrived; }

        bool Shielded => GameManager.I != null && GameManager.I.HasDefense(PaymentRules.DefenseId);

        void OnArrived(OrderInfo o)
        {
            if (o.realFate != OrderFate.Pagada) return;
            if (Shielded && !domeUp) StartCoroutine(RaiseDome());
            StartCoroutine(Travel(Shielded));
        }

        IEnumerator RaiseDome()
        {
            domeUp = true;
            AudioManager.At(Sfx.Shield, dome.position, 1.3f);
            FxManager.Title("FIREWALL ACTIVO", "Intrusión bloqueada", Palette.Secure);
            yield return Ease.Tween(0.9f, k => dome.localScale = Vector3.one * Ease.OutBack(k), Ease.Linear, unscaled: false);
        }

        IEnumerator Travel(bool blocked)
        {
            if (swarm == null || path == null || path.Length < 2) yield break;
            var e = swarm.emission; e.enabled = true;
            int last = blocked ? path.Length - 2 : path.Length - 1;   // con escudo, se detiene sobre la mesa
            Vector3 end = blocked && dome ? dome.position + Vector3.up * 0.9f : path[last];
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / travelSeconds;
                swarm.transform.position = Sample(Mathf.Clamp01(t), last, end);
                yield return null;
            }
            e.enabled = false;
            if (blocked)
            {
                domeFlash = 1f;
                FxManager.I?.Burst(end, Palette.Secure, 24, 2f);
                AudioManager.At(Sfx.Shield, end, 1f);
                Haptics.Confirm();
            }
            else
            {
                FxManager.I?.Burst(end, Palette.Threat, 20, 1.6f);
                AudioManager.At(Sfx.Glitch, end, 1f);
            }
        }

        Vector3 Sample(float t, int last, Vector3 end)
        {
            float f = t * last;
            int i = Mathf.Min(Mathf.FloorToInt(f), last - 1);
            Vector3 a = path[i], b = i + 1 == last ? end : path[i + 1];
            return Vector3.Lerp(a, b, f - i) + new Vector3(Mathf.Sin(t * 30f), Mathf.Cos(t * 23f), 0f) * 0.08f;
        }

        void Update()
        {
            if (!domeRenderer || !domeUp) return;
            domeFlash = Mathf.MoveTowards(domeFlash, 0f, Time.deltaTime * 1.5f);
            float shimmer = 0.16f + 0.05f * Mathf.Sin(Time.time * 3f);
            domeRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_TintColor", Palette.WithAlpha(Palette.Secure, shimmer + 0.5f * domeFlash));
            domeRenderer.SetPropertyBlock(mpb);
        }
    }
}
