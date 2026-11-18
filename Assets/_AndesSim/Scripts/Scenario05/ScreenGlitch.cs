using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Interferencia digital en una pantalla: el contenido salta, aparecen franjas de color
    /// y suena estática. Señal visual de que alguien "está adentro" del sistema.
    /// </summary>
    public class ScreenGlitch : MonoBehaviour
    {
        public RectTransform content;
        public Image[] bars;
        public CanvasGroup group;
        public float idleMinSeconds = 3.5f, idleMaxSeconds = 7f;

        Vector2 basePos;
        bool running;
        float nextIdle;

        void Awake()
        {
            if (content) basePos = content.anchoredPosition;
            if (bars != null) foreach (var b in bars) if (b) b.enabled = false;
            nextIdle = Time.time + Random.Range(idleMinSeconds, idleMaxSeconds);
        }

        void Update()
        {
            // En alerta o peligro la pantalla falla sola de vez en cuando.
            var mood = AmbienceController.I ? AmbienceController.I.Mood : RoomMood.Calm;
            if ((mood == RoomMood.Alert || mood == RoomMood.Danger) && Time.time > nextIdle)
            {
                nextIdle = Time.time + Random.Range(idleMinSeconds, idleMaxSeconds);
                Trigger(0.25f, false);
            }
        }

        public void Trigger(float seconds = 0.4f, bool sound = true)
        {
            if (!running && isActiveAndEnabled) StartCoroutine(Run(seconds, sound));
        }

        IEnumerator Run(float seconds, bool sound)
        {
            running = true;
            if (sound) AudioManager.At(Sfx.Glitch, transform.position, 0.7f);
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                if (content) content.anchoredPosition = basePos + new Vector2(Random.Range(-14f, 14f), Random.Range(-3f, 3f));
                if (group) group.alpha = Random.value < 0.15f ? 0.55f : 1f;
                if (bars != null)
                    foreach (var b in bars)
                    {
                        if (!b) continue;
                        b.enabled = Random.value < 0.6f;
                        var rt = b.rectTransform;
                        rt.anchorMin = new Vector2(0f, Random.value);
                        rt.anchorMax = new Vector2(1f, rt.anchorMin.y);
                        rt.sizeDelta = new Vector2(0f, Random.Range(6f, 34f));
                        rt.anchoredPosition = new Vector2(Random.Range(-40f, 40f), 0f);
                        b.color = Palette.WithAlpha(Random.value < 0.5f ? Palette.Threat : Palette.Data, Random.Range(0.25f, 0.6f));
                    }
                yield return new WaitForSeconds(Random.Range(0.03f, 0.07f));
            }
            if (content) content.anchoredPosition = basePos;
            if (group) group.alpha = 1f;
            if (bars != null) foreach (var b in bars) if (b) b.enabled = false;
            running = false;
        }
    }
}
