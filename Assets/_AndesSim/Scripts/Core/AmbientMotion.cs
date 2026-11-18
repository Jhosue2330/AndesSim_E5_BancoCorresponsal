using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Movimiento ambiental barato para que nada esté "muerto": girar, flotar, latir
    /// o desplazar la textura (LEDs de los racks, ventanas de la ciudad).
    /// </summary>
    public class AmbientMotion : MonoBehaviour
    {
        public enum Mode { Spin, Bob, Pulse, ScrollTexture, Flicker }
        public Mode mode = Mode.Spin;
        public Vector3 spinAxis = Vector3.up;
        public float speed = 30f;
        public float amplitude = 0.05f;
        public float phase;
        [Tooltip("Para ScrollTexture / Flicker: el material se clona una vez.")]
        public Renderer target;
        public Vector2 scroll = new Vector2(0f, 0.25f);
        public float stepSeconds = 0.18f;

        Vector3 basePos, baseScale;
        Material mat;
        float nextStep;
        Color baseColor;

        void Start()
        {
            basePos = transform.localPosition;
            baseScale = transform.localScale;
            if ((mode == Mode.ScrollTexture || mode == Mode.Flicker) && target)
            {
                mat = target.material; // clon único para esta animación
                baseColor = mat.HasProperty("_Color") ? mat.color : Color.white;
            }
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            switch (mode)
            {
                case Mode.Spin: transform.Rotate(spinAxis, speed * Time.deltaTime, Space.Self); break;
                case Mode.Bob: transform.localPosition = basePos + Vector3.up * Mathf.Sin(t) * amplitude; break;
                case Mode.Pulse: transform.localScale = baseScale * (1f + Mathf.Sin(t) * amplitude); break;
                case Mode.ScrollTexture:
                    if (mat && Time.time >= nextStep)
                    {   // saltos discretos: parece parpadeo de LEDs, no una textura que se desliza
                        nextStep = Time.time + stepSeconds;
                        mat.mainTextureOffset += scroll;
                    }
                    break;
                case Mode.Flicker:
                    if (mat) mat.color = baseColor * (0.85f + 0.15f * Mathf.PerlinNoise(t, phase));
                    break;
            }
        }
    }
}
