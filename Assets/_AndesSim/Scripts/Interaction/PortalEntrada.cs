using System.Collections;
using TMPro;
using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Portal de entrada: muestra la misión, deja elegir modo Ataque 2016 o Defensa
    /// y teletransporta al jugador a la sala del escenario.
    /// </summary>
    public class PortalEntrada : MonoBehaviour
    {
        public TMP_Text introTitle;
        public TMP_Text introBody;
        public TeleportPoint destination;
        [Tooltip("Efecto visual del portal (se anima al cruzar).")]
        public Transform portalRing;
        public float spinSpeed = 30f;
        public float crossDelay = 1.2f;

        public static event System.Action Crossed;

        bool crossing;

        void Start()
        {
            var gm = GameManager.I;
            if (gm != null && gm.data != null)
            {
                if (introTitle) introTitle.text = $"ESCENARIO {gm.data.scenarioNumber:00} · {gm.data.scenarioName.ToUpperInvariant()}";
                if (introBody) introBody.text =
                    $"<color={Palette.Hex(Palette.Warning)}>{gm.data.dateRange} · TRANSMISIÓN CLASIFICADA</color>\n" +
                    gm.data.story + "\n" +
                    $"<color={Palette.Hex(Palette.Secure)}>" + gm.data.playerGoal + "</color>\n" +
                    $"<size=85%><color={Palette.Hex(Palette.InkDim)}>Mira un botón y toca la pantalla (o sigue mirando 2 s) para entrar.</color></size>";
            }
            StartCoroutine(TypeIntro());
        }

        System.Collections.IEnumerator TypeIntro()
        {
            yield return new WaitForSeconds(1.0f); // espera el fundido inicial
            var tw = introBody ? introBody.GetComponent<Typewriter>() : null;
            if (tw) tw.Play();
        }

        void Update()
        {
            if (portalRing) portalRing.Rotate(0f, 0f, (crossing ? spinSpeed * 8f : spinSpeed) * Time.deltaTime, Space.Self);
        }

        public void ChooseAttack() => Cross(SimMode.Ataque);
        public void ChooseDefense() => Cross(SimMode.Defensa);

        void Cross(SimMode mode)
        {
            if (crossing) return;
            crossing = true;
            GameManager.I?.SetMode(mode);
            StartCoroutine(CrossRoutine());
        }

        IEnumerator CrossRoutine()
        {
            AudioManager.P(Sfx.Whoosh);
            FxManager.I?.Burst(portalRing ? portalRing.position : transform.position, Palette.Violet, 40, 3f);
            yield return new WaitForSeconds(crossDelay);
            if (destination) destination.TeleportHere();
            yield return new WaitForSeconds(0.3f);
            GameManager.I?.BeginScenario();
            Crossed?.Invoke();
        }
    }
}
