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
        [Tooltip("Opcional: ascensor de llegada (si está, el portal lleva primero al ascensor).")]
        public ElevatorRide elevator;
        public Transform playerRig;
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
                if (introTitle) introTitle.text = "ESCENARIO 05 · BANCO CORRESPONSAL";
                if (introBody) introBody.text =
                    $"<color={Palette.Hex(Palette.Warning)}>4 FEB 2016 · US$ 951 M EN JUEGO</color>\n" +
                    "Lazarus envía órdenes falsas.\n" +
                    $"<color={Palette.Hex(Palette.Secure)}><b>¿Puedes detenerlas?</b></color>\n" +
                    $"<size=70%><color={Palette.Hex(Palette.InkDim)}>Mira un botón y toca</color></size>";
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
            if (elevator && playerRig)
            {
                // Portal → ascensor (fundido) → subida → puertas abiertas → sala
                yield return FxManager.I != null ? FxManager.I.FadeThrough(() =>
                {
                    playerRig.position = elevator.Stand;
                    playerRig.rotation = elevator.FacingDoors;
                }, 0.25f, 0.6f) : null;
                yield return elevator.Ride(playerRig);
            }
            if (destination) destination.TeleportHere();
            yield return new WaitForSeconds(0.6f);
            GameManager.I?.BeginScenario();
            Crossed?.Invoke();
        }
    }
}
