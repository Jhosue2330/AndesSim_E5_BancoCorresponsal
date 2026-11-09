using System;
using System.Collections;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Teléfono de la sala. Llamar al BANCO ANDINO para confirmar las órdenes.
    /// Ataque 2016: suena sin respuesta (fin de semana).
    /// Defensa: con «Contactos 24/7» y «Número registrado» activos, la verificación se completa
    /// y las 5 órdenes pagadas pasan a retenidas.
    /// </summary>
    public class CallbackDefense : GazeInteractable
    {
        public const string Contacts247 = "contactos_24_7";
        public const string RegisteredNumber = "canal_alterno";

        [Header("Escenario 5")]
        public OrderFeed feed;
        public int rings = 3;
        public float ringSeconds = 2f;
        [Tooltip("Parte que se levanta al llamar (auricular).")]
        public Transform handset;

        public bool Calling { get; private set; }
        public bool CallDone { get; private set; }
        public bool Verified { get; private set; }
        public event Action<bool> CallFinished;

        protected override void Clicked()
        {
            if (Calling) return;
            var gm = GameManager.I;
            if (gm == null || !gm.HasStarted) return;
            StartCoroutine(Call());
        }

        IEnumerator Call()
        {
            Calling = true;
            var gm = GameManager.I;
            var data = gm.data as CorresponsalScenarioData;
            if (feed) feed.SetSimTime(new DateTime(2016, 2, 6, 10, 15, 0)); // sábado
            Vector3 handsetPos = handset ? handset.localPosition : Vector3.zero;
            if (handset) handset.localPosition = handsetPos + Vector3.up * 0.08f;

            bool has247 = gm.mode == SimMode.Defensa && gm.HasDefense(Contacts247);
            bool hasRegistered = gm.mode == SimMode.Defensa && gm.HasDefense(RegisteredNumber);
            string number = hasRegistered && data != null ? data.andinoEmergencyNumber : "número del mensaje SWIFT";
            UIManager.Msg("LLAMANDO AL BANCO ANDINO", $"Sábado 6 feb 2016 · {number}", rings * ringSeconds + 0.5f);

            int n = has247 && hasRegistered ? 2 : rings;
            for (int i = 0; i < n; i++)
            {
                AudioManager.P(Sfx.PhoneRing);
                yield return new WaitForSeconds(ringSeconds);
            }

            Verified = has247 && hasRegistered;
            if (Verified)
            {
                AudioManager.P(Sfx.PhoneAnswer);
                int recalled = feed ? feed.RecallAfterVerification() : 0;
                UIManager.Msg("VERIFICACIÓN COMPLETADA",
                    (data ? data.callVerifiedText : "Órdenes retenidas.") + $"\n({recalled} órdenes cambiaron a RETENIDA)", 8f);
            }
            else
            {
                AudioManager.P(Sfx.PhoneNoAnswer);
                string why = data ? data.callNoAnswerText : "Nadie contesta.";
                if (gm.mode == SimMode.Defensa)
                {
                    if (!has247) why += "\n<color=#ffd166>Falta activar: Contactos 24/7.</color>";
                    if (!hasRegistered) why += "\n<color=#ffd166>Falta activar: Número registrado (canal alterno).</color>";
                }
                UIManager.Msg("SIN RESPUESTA", why, 8f);
            }

            if (handset) handset.localPosition = handsetPos;
            Calling = false;
            CallDone = true;
            CallFinished?.Invoke(Verified);
        }
    }
}
