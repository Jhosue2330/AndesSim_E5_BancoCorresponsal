using System.Collections;
using TMPro;
using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Llegada en ascensor de vidrio: el jugador sube por la fachada con la ciudad de noche,
    /// el contador de pisos avanza, suena el "ding" y se abren las puertas a la sala.
    /// Subida lenta y lineal (sin aceleraciones bruscas): cómoda en VR.
    /// </summary>
    public class ElevatorRide : MonoBehaviour
    {
        public Transform cabin;
        public Transform doorLeft, doorRight;
        public TMP_Text floorDisplay;
        public float startDepth = 24f;      // metros bajo el piso de la sala
        public float rideSeconds = 7f;
        public int topFloor = 38;
        public float doorOpen = 0.85f;

        Vector3 topPos, doorL0, doorR0;

        void Awake()
        {
            if (cabin == null) cabin = transform;
            topPos = cabin.position;
            if (doorLeft) doorL0 = doorLeft.localPosition;
            if (doorRight) doorR0 = doorRight.localPosition;
            cabin.position = topPos + Vector3.down * startDepth;
            if (floorDisplay) floorDisplay.text = "PISO 01";
        }

        /// <summary>Punto donde se para el jugador dentro de la cabina.</summary>
        public Vector3 Stand => cabin.position;

        /// <summary>Rotación para que el jugador mire hacia las puertas.</summary>
        public Quaternion FacingDoors
        {
            get
            {
                if (!doorLeft || !doorRight) return cabin.rotation;
                Vector3 d = (doorLeft.position + doorRight.position) * 0.5f - cabin.position; d.y = 0f;
                return d.sqrMagnitude > 0.001f ? Quaternion.LookRotation(d) : cabin.rotation;
            }
        }

        public IEnumerator Ride(Transform rig)
        {
            Vector3 bottom = topPos + Vector3.down * startDepth;
            cabin.position = bottom;
            Vector3 offset = rig.position - cabin.position;
            AudioManager.P(Sfx.Whoosh, 0.6f);
            yield return Ease.Tween(rideSeconds, k =>
            {
                cabin.position = Vector3.Lerp(bottom, topPos, k);
                rig.position = cabin.position + offset;
                if (floorDisplay) floorDisplay.text = $"PISO {Mathf.Max(1, Mathf.RoundToInt(k * topFloor)):00}";
            }, Ease.InOutSine, unscaled: false);
            AudioManager.At(Sfx.Ding, cabin.position + Vector3.up * 2f, 1.2f);
            Haptics.Tap();
            if (floorDisplay) floorDisplay.text = $"PISO {topFloor} · SOC";
            yield return new WaitForSeconds(0.4f);
            yield return Ease.Tween(1.1f, k =>
            {
                // cada puerta se aleja del centro (no importa cómo quedó el eje X al importar)
                if (doorLeft) doorLeft.localPosition = doorL0 + new Vector3(Mathf.Sign(doorL0.x - doorR0.x), 0f, 0f) * doorOpen * k;
                if (doorRight) doorRight.localPosition = doorR0 + new Vector3(Mathf.Sign(doorR0.x - doorL0.x), 0f, 0f) * doorOpen * k;
            }, Ease.OutCubic, unscaled: false);
            yield return new WaitForSeconds(1.6f);
        }
    }
}
