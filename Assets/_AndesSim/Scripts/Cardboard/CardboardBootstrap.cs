using Google.XR.Cardboard;
using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Arranque de Cardboard (basado en CardboardStartup del ejemplo oficial de Google):
    /// pantalla siempre encendida, escaneo del código QR del visor, botón de engranaje,
    /// botón de cerrar y recentrado manteniendo pulsado el botón del visor.
    /// Este ensamblado solo compila si el paquete com.google.xr.cardboard está instalado.
    /// </summary>
    public class CardboardBootstrap : MonoBehaviour
    {
        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.brightness = 1.0f;
            if (Application.isEditor) return;
            if (!Api.HasDeviceParams()) Api.ScanDeviceParams();
        }

        void Update()
        {
            if (Application.isEditor) return;
            if (Api.IsGearButtonPressed) Api.ScanDeviceParams();
            if (Api.IsCloseButtonPressed) Application.Quit();
            if (Api.IsTriggerHeldPressed) Api.Recenter();
            if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();
            Api.UpdateScreenParams();
        }
    }
}
