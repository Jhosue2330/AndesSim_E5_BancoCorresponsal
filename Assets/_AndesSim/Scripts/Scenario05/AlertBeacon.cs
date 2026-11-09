using System.Collections;
using TMPro;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>Alerta de sanciones: luz que parpadea, alarma y banner en la pantalla central.</summary>
    public class AlertBeacon : MonoBehaviour
    {
        public Light alertLight;
        public Renderer beaconRenderer;
        public GameObject banner;
        public TMP_Text bannerText;
        [Tooltip("Evidencia «alerta_jupiter»: solo se puede inspeccionar cuando la alerta se activa.")]
        public EvidenceItem evidence;
        public float blinkSeconds = 12f;
        public int alarmRepeats = 3;

        public bool Triggered { get; private set; }
        Color onColor = new Color(1f, 0.35f, 0.3f);
        Color offColor = new Color(0.25f, 0.06f, 0.05f);
        MaterialPropertyBlock mpb;

        void Awake()
        {
            if (alertLight) alertLight.enabled = false;
            if (banner) banner.SetActive(false);
            if (evidence) evidence.interactable = false;
            SetBeacon(false);
        }

        public void Trigger(string term, string explanation)
        {
            if (Triggered) return;
            Triggered = true;
            if (banner) banner.SetActive(true);
            if (bannerText) bannerText.text = $"COINCIDENCIA: «{term}» · LISTA DE SANCIONES";
            if (evidence)
            {
                evidence.interactable = true;
                evidence.description = explanation;
            }
            StartCoroutine(Blink());
        }

        IEnumerator Blink()
        {
            float t = 0f;
            int rang = 0;
            float nextAlarm = 0f;
            while (t < blinkSeconds)
            {
                if (rang < alarmRepeats && t >= nextAlarm) { AudioManager.P(Sfx.Alarm); rang++; nextAlarm += 1.4f; }
                bool on = Mathf.Repeat(t, 0.6f) < 0.3f;
                if (alertLight) alertLight.enabled = on;
                SetBeacon(on);
                t += Time.deltaTime;
                yield return null;
            }
            if (alertLight) alertLight.enabled = false;
            SetBeacon(true);
        }

        void SetBeacon(bool on)
        {
            if (!beaconRenderer) return;
            mpb ??= new MaterialPropertyBlock();
            beaconRenderer.GetPropertyBlock(mpb);
            var c = on ? onColor : offColor;
            mpb.SetColor("_Color", c);
            mpb.SetColor("_EmissionColor", on ? onColor * 2f : Color.black);
            beaconRenderer.SetPropertyBlock(mpb);
        }
    }
}
