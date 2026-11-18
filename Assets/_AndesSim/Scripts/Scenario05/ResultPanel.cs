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
            bool ok = lost <= 0.01f;

            var (score, letter, color) = Grade(gm, lost);
            sb.AppendLine($"<size=320%><b><color={Palette.Hex(color)}>{letter}</color></b></size><voffset=0.6em>  CALIFICACIÓN DEL ANALISTA · <b>{score}</b>/100</voffset>");
            sb.AppendLine($"<size=120%>Salvado: <color={Palette.Hex(Palette.Secure)}>US$ {saved:0} M</color>   ·   Perdido: <color={Palette.Hex(Palette.Threat)}>US$ {lost:0} M</color></size>");
            if (feed)
            {
                sb.AppendLine($"Pagadas {feed.Count(OrderStatus.Pagada)} · Congeladas {feed.Count(OrderStatus.Bloqueada)} · " +
                              $"Retenidas {feed.Count(OrderStatus.Retenida)} · No reenviadas {feed.Count(OrderStatus.NoReenviada)}");
                sb.AppendLine($"Órdenes que bloqueaste tú: {feed.BlockedByPlayer}");
            }
            sb.AppendLine();
            if (gm.mode == SimMode.Ataque)
            {
                sb.AppendLine($"<color={Palette.Hex(Palette.Threat)}>Así pasó en 2016:</color>"+" la alerta «JUPITER» congeló 30 órdenes, pero 5 ya se habían pagado " +
                              "(4 a Manila por US$81 M y 1 a la «Fandation» por US$20 M). Nadie contestó en el BANCO ANDINO: era fin de semana.");
                sb.AppendLine("<b>Vulnerabilidades:</b> contactos solo en días hábiles · sin confirmación por canal alterno · sin reglas por destino ni monto.");
                sb.AppendLine($"<color={Palette.Hex(Palette.Warning)}>Mira «VER ANÁLISIS» para entender por qué funcionó el ataque.</color>");
            }
            else
            {
                sb.AppendLine(ok
                    ? $"<color={Palette.Hex(Palette.Secure)}>Defensa exitosa:</color>"+" las 5 órdenes que en 2016 se pagaron quedaron RETENIDAS."
                    : $"<color={Palette.Hex(Palette.Warning)}>Defensa incompleta:</color>"+" todavía salió dinero. Revisa qué controles faltó activar.");
                sb.AppendLine($"Defensas aplicadas: {gm.DefenseCount}/{gm.TotalDefenses}  ·  2016 sin defensas: perdido US$ {RealPaid:0} M.");
            }
            sb.AppendLine($"Evidencias encontradas: {gm.EvidenceCount}/{gm.TotalEvidence}");
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
