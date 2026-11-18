using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Logros del analista: dan rejugabilidad y premian investigar, no solo terminar.
    /// Se muestran al desbloquearse (sonido + destello) y en la pantalla final.
    /// </summary>
    public class Achievements : MonoBehaviour
    {
        public static Achievements I;
        public OrderFeed feed;

        public static readonly (string id, string title, string desc)[] All =
        {
            ("ojo",         "OJO DE HALCÓN",   "Encontraste todas las evidencias"),
            ("detective",   "DETECTIVE",       "Descubriste el error «FANDATION» con la lupa"),
            ("reflejos",    "REFLEJOS",        "Bloqueaste 5 órdenes con la mirada"),
            ("preguntas",   "BUENAS PREGUNTAS","Interrogaste al BANCO ANDINO antes de retener"),
            ("explorador",  "EXPLORADOR",      "Recorriste 15 m caminando por la sala"),
            ("muro",        "MURO VERDE",      "Defensa perfecta: no salió ni un dólar"),
            ("contrarreloj","CONTRARRELOJ",    "Terminaste la misión en menos de 4 minutos"),
        };

        readonly HashSet<string> unlocked = new HashSet<string>();
        float startTime = -1f, nextCheck;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; PortalEntrada.Crossed -= OnStart; }
        void Start() { PortalEntrada.Crossed += OnStart; }
        void OnStart() => startTime = Time.time;

        public float Elapsed => startTime < 0 ? 0f : Time.time - startTime;
        public IEnumerable<string> Unlocked => unlocked;

        public static void Unlock(string id) { if (I != null) I.DoUnlock(id); }

        readonly Queue<string> pending = new Queue<string>();
        float nextShow;

        void DoUnlock(string id)
        {
            if (!unlocked.Add(id)) return;
            pending.Enqueue(id); // se anuncian de a uno (si no, los textos se superponen)
        }

        void Announce(string id)
        {
            var a = All.FirstOrDefault(x => x.id == id);
            AudioManager.P(Sfx.Achievement);
            Haptics.Pulse(120);
            var head = UIManager.I ? UIManager.I.head : null;
            if (head)
            {
                Vector3 p = head.position + head.forward * 1.6f + Vector3.up * 0.35f;
                FxManager.I?.Burst(p, Palette.Warning, 26, 1.6f);
                FxManager.Float(p, "LOGRO · " + a.title, Palette.Warning);
            }
        }

        void LateUpdate()
        {
            if (pending.Count > 0 && Time.unscaledTime >= nextShow)
            {
                Announce(pending.Dequeue());
                nextShow = Time.unscaledTime + 1.4f;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.HasStarted || gm.HasEnded || Time.time < nextCheck) return;
            nextCheck = Time.time + 0.5f;
            if (gm.TotalEvidence > 0 && gm.EvidenceCount >= gm.TotalEvidence) DoUnlock("ojo");
            if (feed && feed.BlockedByPlayer >= 5) DoUnlock("reflejos");
            if (GazeWalk.DistanceWalked >= 15f) DoUnlock("explorador");
        }

        /// <summary>Logros que se deciden al terminar (los llama ResultPanel antes de armar el texto).</summary>
        public void EvaluateEnd(float lost)
        {
            var gm = GameManager.I;
            if (gm != null && gm.mode == SimMode.Defensa && lost <= 0.01f) DoUnlock("muro");
            if (Elapsed > 0f && Elapsed < 240f) DoUnlock("contrarreloj");
        }

        public string Summary()
        {
            var got = All.Where(a => unlocked.Contains(a.id)).Select(a => a.title);
            string list = unlocked.Count > 0 ? string.Join(" · ", got) : "ninguno todavía";
            return $"<color={Palette.Hex(Palette.Warning)}>LOGROS {unlocked.Count}/{All.Length}</color>  {list}";
        }
    }
}
