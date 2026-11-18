using System.Text;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>Calcula el texto de la pantalla final: dinero salvado y perdido en el modo jugado.</summary>
    public class ResultPanel : MonoBehaviour
    {
        public OrderFeed feed;
        public CallbackDefense phone;

        // Cifras del caso real en el Banco Corresponsal (2016).
        const float RealPaid = 101f, RealBlocked = 850f;

        void Start()
        {
            if (GameManager.I != null) GameManager.I.ResultTextProvider = Build;
        }

        public string Build()
        {
            var gm = GameManager.I;
            var sb = new StringBuilder();
            float total = feed ? feed.TotalAmount : 951f;
            float lost = feed ? feed.Sum(OrderStatus.Pagada, OrderStatus.EnTransito) : RealPaid;
            float saved = total - lost;
            Achievements.I?.EvaluateEnd(lost);
            var (score, letter, color) = Grade(gm, lost);
            // Poco texto: nota gigante, dos números y los logros. Lo técnico vive en «VER ANÁLISIS».
            sb.AppendLine($"<align=center><size=420%><b><color={Palette.Hex(color)}>{letter}</color></b></size>");
            sb.AppendLine($"<size=90%><color={Palette.Hex(Palette.InkDim)}>ANALISTA · {score}/100</color></size>");
            sb.AppendLine();
            sb.AppendLine($"<size=150%><color={Palette.Hex(Palette.Secure)}>US$ {saved:0} M</color>   <color={Palette.Hex(Palette.Threat)}>−US$ {lost:0} M</color></size>");
            sb.AppendLine($"<size=80%><color={Palette.Hex(Palette.InkDim)}>SALVADO                          PERDIDO</color></size>");
            sb.AppendLine();
            sb.AppendLine(gm.mode == SimMode.Ataque
                ? $"<color={Palette.Hex(Palette.Threat)}>En 2016 nadie contestó el teléfono.</color>"
                : lost <= 0.01f ? $"<color={Palette.Hex(Palette.Secure)}>¡Defensa perfecta!</color>"
                                : $"<color={Palette.Hex(Palette.Warning)}>Faltaron defensas.</color>");
            if (Achievements.I != null)
            {
                float t = Achievements.I.Elapsed;
                sb.AppendLine();
                sb.AppendLine($"<size=85%>{Achievements.I.Summary()}   ·   {(int)t / 60}:{(int)t % 60:00}</size>");
            }
            sb.Append("</align>");
            return sb.ToString();
        }

        /// <summary>
        /// Calificación del analista (0–100). Ataque: investigación (evidencias, bloqueos, llamada).
        /// Defensa: controles, dinero salvado, evidencias y bloqueos.
        /// </summary>
        public (int score, string letter, Color color) Grade(GameManager gm, float lost)
        {
            float ev = gm.TotalEvidence > 0 ? (float)gm.EvidenceCount / gm.TotalEvidence : 0f;
            float blocks = feed ? Mathf.Clamp01(feed.BlockedByPlayer / 8f) : 0f;
            bool called = phone && phone.CallDone;
            float s = gm.mode == SimMode.Ataque
                ? 50f * ev + 30f * blocks + (called ? 20f : 0f)
                : 30f * (gm.TotalDefenses > 0 ? (float)gm.DefenseCount / gm.TotalDefenses : 0f) + (lost <= 0.01f ? 35f : 0f) + 20f * ev + 15f * blocks;
            int score = Mathf.RoundToInt(s);
            if (score >= 90) return (score, "S", Palette.Secure);
            if (score >= 75) return (score, "A", Palette.Data);
            if (score >= 55) return (score, "B", Palette.Warning);
            return (score, "C", Palette.Threat);
        }
    }
}
