using System.Text;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>Calcula el texto de la pantalla final: dinero salvado y perdido en el modo jugado.</summary>
    public class ResultPanel : MonoBehaviour
    {
        public OrderFeed feed;

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

            sb.AppendLine($"<size=120%>Salvado: <color=#7dffb0>US$ {saved:0} M</color>   ·   Perdido: <color=#ff8a80>US$ {lost:0} M</color></size>");
            if (feed)
            {
                sb.AppendLine($"Pagadas {feed.Count(OrderStatus.Pagada)} · Congeladas {feed.Count(OrderStatus.Bloqueada)} · " +
                              $"Retenidas {feed.Count(OrderStatus.Retenida)} · No reenviadas {feed.Count(OrderStatus.NoReenviada)}");
                sb.AppendLine($"Órdenes que bloqueaste tú: {feed.BlockedByPlayer}");
            }
            sb.AppendLine();
            if (gm.mode == SimMode.Ataque)
            {
                sb.AppendLine("<color=#ff5a4e>Así pasó en 2016:</color> la alerta «JUPITER» congeló 30 órdenes, pero 5 ya se habían pagado " +
                              "(4 a Manila por US$81 M y 1 a la «Fandation» por US$20 M). Nadie contestó en el BANCO ANDINO: era fin de semana.");
                sb.AppendLine("<b>Vulnerabilidades:</b> contactos solo en días hábiles · sin confirmación por canal alterno · sin reglas por destino ni monto.");
                sb.AppendLine("<color=#ffc24a>Mira «VER ANÁLISIS» para entender por qué funcionó el ataque.</color>");
            }
            else
            {
                sb.AppendLine(ok
                    ? "<color=#38f28a>Defensa exitosa:</color> las 5 órdenes que en 2016 se pagaron quedaron RETENIDAS."
                    : "<color=#ffd166>Defensa incompleta:</color> todavía salió dinero. Revisa qué controles faltó activar.");
                sb.AppendLine($"Defensas aplicadas: {gm.DefenseCount}/{gm.TotalDefenses}  ·  2016 sin defensas: perdido US$ {RealPaid:0} M.");
            }
            sb.AppendLine($"Evidencias encontradas: {gm.EvidenceCount}/{gm.TotalEvidence}");
            return sb.ToString();
        }
    }
}
