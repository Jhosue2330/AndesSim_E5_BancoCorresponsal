using System.Collections.Generic;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Datos del Escenario 5. Todas las personas, empresas y cuentas son ficticias.
    /// Cifras del caso: 36 órdenes por US$951 M; 30 bloqueadas (~US$850 M);
    /// 5 pagadas (US$101 M = 4 a Manila por US$81 M + 1 a la «Fandation» por US$20 M); 1 nunca reenviada.
    /// </summary>
    [CreateAssetMenu(menuName = "AndesSim/Escenario 05 Data", fileName = "Escenario05_Data")]
    public class CorresponsalScenarioData : ScenarioData
    {
        [Header("Escenario 5 · Órdenes")]
        public List<OrderInfo> orders = new List<OrderInfo>();
        [Tooltip("Lista de sanciones ficticia. «JUPITER» es el nombre de un petrolero sancionado.")]
        public string[] sanctionsList = { "JUPITER", "OCEAN STAR VII", "NEPTUNE CARGO", "BLUE HORIZON" };
        public float secondsBetweenOrders = 1.6f;
        [Tooltip("Segundos que una orden en revisión espera la decisión del jugador antes de que Cumplimiento la congele.")]
        public float reviewSeconds = 9f;
        [Tooltip("En modo defensa, montos mayores a esto hacia un beneficiario nuevo se retienen.")]
        public float unusualAmountMillions = 5f;
        public string[] usualCountries = { "Estados Unidos", "Alemania", "Japón", "Reino Unido" };

        [Header("Escenario 5 · Llamada")]
        public string andinoEmergencyNumber = "+00 (1) 555-0100 (ficticio)";
        [TextArea(2, 5)] public string callNoAnswerText =
            "Sábado 6 de febrero. Suena y suena… Nadie contesta en el BANCO ANDINO: los contactos de emergencia solo atienden de lunes a viernes. Los mensajes SWIFT de consulta tampoco tienen respuesta.";
        [TextArea(2, 5)] public string callVerifiedText =
            "Línea de incidentes 24/7 del BANCO ANDINO: «No reconocemos esas órdenes. No las autorizamos. Retengan todo». Las 5 órdenes pasan a RETENIDAS.";

        [ContextMenu("Cargar datos del caso (36 órdenes)")]
        public void LoadCaseDefaults()
        {
            scenarioNumber = 5;
            scenarioName = "Banco Corresponsal Internacional";
            scenarioType = "VR";
            dateRange = "4 al 8 feb 2016";
            story = "Jueves 4 de febrero de 2016, de noche. En la sala de monitoreo del Banco Corresponsal Internacional entran órdenes SWIFT del BANCO ANDINO: 36 órdenes por US$951 millones. Una alerta salta: la calle de la agencia receptora se llama igual que un petrolero de una lista de sanciones. Treinta órdenes quedan congeladas, pero cinco ya pasaron. Las consultas al BANCO ANDINO no tienen respuesta: es fin de semana.";
            playerGoal = "Vigila las pantallas, decide qué órdenes bloquear y verifica por teléfono con el BANCO ANDINO.";
            vulnerabilities = new[]
            {
                "Contactos de emergencia solo en días hábiles.",
                "Sin confirmación por un canal alterno (llamada a un número registrado).",
                "Reglas que no frenaron las 5 primeras órdenes (sin límites por destino ni por monto inusual)."
            };
            defenses = new[]
            {
                "Contactos 24/7 para incidentes.",
                "Confirmación por llamada a un número registrado.",
                "Reglas por destino y monto inusual."
            };
            resultAttack = "La alerta «JUPITER» congeló 30 órdenes, pero 5 ya se habían pagado: US$101 millones salieron.";
            resultDefense = "Las reglas y la llamada de verificación retienen las 5 órdenes: no sale ni un dólar.";
            credits =
                "ANDES-SIM · Escenario 5 · Banco Corresponsal Internacional\n" +
                "Basado en hechos reales (Banco de Bangladesh, 2016). Entidades y personas ficticias.\n" +
                "Equipo:\n" +
                "- Meza Miranda Jhosue Alejandro · Líder de proyecto, analista de ciberseguridad y QA\n" +
                "- Limache Cunyas Noemi Cielo · Desarrollo Unity (C#)\n" +
                "- Castro Inga Anyeli Eliana · Arte 3D, UI y sonido\n" +
                "Modelos 3D: \"Furniture Kit\" · Kenney (kenney.nl) · CC0\n" +
                "Audio (Freesound, licencia CC0 1.0):\n" +
                "- \"Server Room Ambience\" · SoundDesignForYou · freesound.org/s/646682\n" +
                "- \"Digital Alarm Loop.wav\" · bbrocer · freesound.org/s/389511\n" +
                "- \"phone ringing.mp3\" · Rebecca_f · freesound.org/s/425660\n" +
                "- \"Busy Signal\" · ramicio · freesound.org/s/158428\n" +
                "- \"Soft UI Button Click\" · Jummit · freesound.org/s/528561\n" +
                "- \"Phone pick up / hang up\" · deraj · freesound.org/s/218083\n" +
                "Herramientas de IA usadas: Claude Code (Anthropic), Google Antigravity.";

            orders = new List<OrderInfo>();
            const string bpi = "Banco Pacífico Insular";
            // 4 órdenes pagadas a Manila (US$81 M) — los campos de dirección no traían la calle.
            Add(bpi, "Agencia Las Palmeras, Makati, Manila", "Filipinas", OrderFate.Pagada, ("Ramiro D. Castell", 6f), ("Julio P. Alcázar", 30f), ("Andrés V. Montalvo", 20f), ("Ernesto Q. Villalba", 25f));
            // 1 orden pagada a la «Fandation» (US$20 M).
            Add("Banco Isla Esmeralda", "Colombo", "Sri Lanka", OrderFate.Pagada, ("SHALIKA FANDATION", 20f));
            // 30 órdenes bloqueadas por la coincidencia «JUPITER» (US$850 M).
            string[] names =
            {
                "Inversiones Coral Azul", "Comercial Marea Alta", "Tomás R. Galindo", "Grupo Brisa del Sur", "Lucía M. Ferrán",
                "Importadora Puerto Claro", "Héctor S. Cabral", "Servicios Arrecife", "Consorcio Ola Norte", "Pablo N. Ugarte",
                "Agroexport Las Islas", "Diego F. Salmerón", "Textiles Bahía Verde", "Naviera Faro Blanco", "Mateo J. Rivas",
                "Constructora Isla Grande", "Óscar L. Pineda", "Logística Coral", "Distribuidora Atolón", "Raúl E. Montero",
                "Energía Costa Serena", "Iván G. Robledo", "Minera Cabo Norte", "Turismo Laguna", "Sergio A. Valdés",
                "Comercial Pacífico Uno", "Fabián C. Ortega", "Holding Mar Abierto", "Inversiones Palmar", "Víctor H. Quiroga"
            };
            float[] amounts = { 25, 30, 35, 20, 28, 32, 27, 31, 29, 26, 33, 24, 30, 28, 27, 31, 29, 30, 26, 28, 34, 22, 29, 27, 31, 28, 30, 26, 29, 25 };
            var blocked = new (string, float)[names.Length];
            for (int i = 0; i < names.Length; i++) blocked[i] = (names[i], amounts[i]);
            Add(bpi, "Agencia Las Palmeras, Calle Jupiter, Manila", "Filipinas", OrderFate.Bloqueada, blocked);
            // 1 orden rechazada por formato que nunca se reenvió (no cuenta en los montos).
            Add(bpi, "Agencia Las Palmeras, Calle Jupiter, Manila", "Filipinas", OrderFate.NoReenviada, ("(orden rechazada por formato, nunca reenviada)", 0f));
        }

        void Add(string bank, string address, string country, OrderFate fate, params (string name, float amount)[] items)
        {
            foreach (var (name, amount) in items)
            {
                int n = orders.Count + 1;
                orders.Add(new OrderInfo
                {
                    number = n,
                    reference = $"MT103-0402-{n:000}",
                    beneficiary = name,
                    beneficiaryBank = bank,
                    address = address,
                    country = country,
                    amountMillions = amount,
                    realFate = fate
                });
            }
        }

        void OnValidate()
        {
            if (orders == null || orders.Count == 0) LoadCaseDefaults();
        }
    }
}
