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
        [Tooltip("Diálogo con opciones (si no hay, la verificación es automática).")]
        public DialogChoices dialog;

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
            UIManager.Msg("LLAMANDO…", "BANCO ANDINO · sáb 6 feb", rings * ringSeconds + 0.5f);

            int n = has247 && hasRegistered ? 2 : rings;
            for (int i = 0; i < n; i++)
            {
                AudioManager.At(Sfx.PhoneRing, transform.position);
                Haptics.Ring();
                // El teléfono vibra mientras suena (1 s) y luego descansa.
                float t0 = Time.time;
                while (Time.time - t0 < ringSeconds)
                {
                    float e = Time.time - t0;
                    if (handset) handset.localPosition = handsetPos + Vector3.up * 0.08f +
                        (e < 1f ? new Vector3(Mathf.Sin(e * 90f), 0f, Mathf.Cos(e * 77f)) * 0.04f : Vector3.zero);
                    yield return null;
                }
            }
            Vector3 top = GetComponent<Collider>().bounds.center + Vector3.up * 0.3f;

            Verified = has247 && hasRegistered;
            if (Verified)
            {
                AudioManager.At(Sfx.PhoneAnswer, transform.position);
                Haptics.Confirm();
                if (dialog) yield return Conversation();
                int recalled = feed ? feed.RecallAfterVerification() : 0;
                AmbienceController.Set(RoomMood.Secure);
                FxManager.I?.Burst(top, Palette.Secure, 30, 2f);
                FxManager.Float(top, "FRAUDE CONFIRMADO", Palette.Secure);
                UIManager.Msg("VERIFICADO", recalled > 0 ? $"{recalled} órdenes recuperadas" : "Órdenes retenidas", 3f);
            }
            else
            {
                AudioManager.At(Sfx.PhoneNoAnswer, transform.position);
                Haptics.Warn();
                AmbienceController.Set(RoomMood.Danger);
                FxManager.Float(top, "SIN RESPUESTA", Palette.Threat);
                string why = "Fin de semana: nadie atiende.";
                if (gm.mode == SimMode.Defensa)
                {
                    if (!has247) why += $"\n<color={Palette.Hex(Palette.Warning)}>Falta: Contactos 24/7</color>";
                    if (!hasRegistered) why += $"\n<color={Palette.Hex(Palette.Warning)}>Falta: Número registrado</color>";
                }
                UIManager.Msg("SIN RESPUESTA", why, 3.5f);
            }

            if (dialog) dialog.Hide();
            if (handset) handset.localPosition = handsetPos;
            Calling = false;
            CallDone = true;
            CallFinished?.Invoke(Verified);
        }

        /// <summary>
        /// Conversación con la línea 24/7: el jugador elige qué preguntar. Retener todo cierra la llamada.
        /// </summary>
        IEnumerator Conversation()
        {
            const string who = "BANCO ANDINO · 24/7";
            bool askedOrders = false, askedFandation = false;
            string[] options =
            {
                "¿Enviaron 36 órdenes?",
                "¿Conocen a «FANDATION»?",
                "¡Retengan todo!"
            };
            string text = "Línea 24/7, BANCO ANDINO. ¿Diga?";
            while (true)
            {
                int pick = -1;
                yield return dialog.Ask(who, text, options, i => pick = i);
                AudioManager.P(Sfx.Click);
                if (pick == 0) { askedOrders = true; text = "No. Esas órdenes NO son nuestras."; }
                else if (pick == 1) { askedFandation = true; text = "No existe. Y está mal escrito."; }
                else break;
            }
            if (askedOrders && askedFandation) Achievements.Unlock("preguntas");
            yield return dialog.Say(who, "Retenido. Confirmamos por escrito.", 2f);
        }
    }
}
