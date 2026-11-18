using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// ART BIBLE · ANDES-SIM E5
    /// Estilo: «SOC cyber-noir»: sala de seguridad nocturna de acero azul oscuro donde las pantallas
    ///          y el neón son la luz; datos en cian, amenaza en rojo, alerta en ámbar, protegido en verde.
    /// Ánimo: sin luz de día; el neón cian ilumina en calma; la alerta inunda la sala de rojo;
    ///          la defensa exitosa vuelve la sala verde.
    /// TODO color del proyecto sale de aquí (materiales, luces, niebla, partículas, UI).
    /// </summary>
    public static class Palette
    {
        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        // Entorno
        public static readonly Color Void = H("#05070D");      // fondo / cielo / sombra profunda
        public static readonly Color Floor = H("#0B1222");     // piso navy brillante
        public static readonly Color Wall = H("#121A2C");      // paneles de pared
        public static readonly Color Trim = H("#22304A");      // marcos, columnas, zócalos
        public static readonly Color Steel = H("#3A4862");     // muebles metálicos
        public static readonly Color Paper = H("#C9D6E3");     // papeles / evidencias

        // Señales
        public static readonly Color Data = H("#19D3F0");      // cian · datos / calma
        public static readonly Color Threat = H("#FF3B4E");    // rojo · amenaza / pérdida
        public static readonly Color Warning = H("#FFB020");   // ámbar · alerta / revisión
        public static readonly Color Secure = H("#2BF59A");    // verde · protegido / defensa
        public static readonly Color Violet = H("#7A5CF0");    // acento secundario (portal)

        // Texto e interfaz
        public static readonly Color Ink = H("#DDF6FF");
        public static readonly Color InkDim = H("#8FA6B8");
        public static readonly Color Panel = new Color(0.02f, 0.05f, 0.10f, 0.94f);
        public static readonly Color PanelLight = new Color(0.05f, 0.10f, 0.18f, 0.96f);

        // Luz
        public static readonly Color KeyLight = H("#9FC8FF");
        public static readonly Color AmbientSky = H("#1A2A4A");
        public static readonly Color AmbientEquator = H("#0E1626");
        public static readonly Color AmbientGround = H("#07090F");

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
    }
}
