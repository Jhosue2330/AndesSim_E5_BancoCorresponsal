using System.Collections;
using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Tormenta tras el ventanal: lluvia constante y relámpagos que iluminan toda la sala
    /// por un instante, con el trueno llegando segundos después (desde el ventanal, en 3D).
    /// </summary>
    public class StormController : MonoBehaviour
    {
        public Light lightning;
        public Renderer sky;
        public Transform windowCenter;
        public Vector2 interval = new Vector2(11f, 22f);
        public float flashIntensity = 2.6f;

        MaterialPropertyBlock mpb;
        Color skyBase = Color.white;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            if (lightning) lightning.enabled = false;
            AudioManager.I?.Loop3D("rain", windowCenter ? windowCenter : transform, 0.35f, 14f);
            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(Random.Range(4f, 8f));
            while (true)
            {
                yield return Strike();
                yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
            }
        }

        /// <summary>Un relámpago: destello doble, cielo encendido y trueno retrasado.</summary>
        public IEnumerator Strike()
        {
            float[] pattern = { 1f, 0f, 0.6f, 0f, 1f };
            float[] times = { 0.06f, 0.07f, 0.05f, 0.09f, 0.0f };
            for (int i = 0; i < pattern.Length; i++)
            {
                Set(pattern[i]);
                if (times[i] > 0f) yield return new WaitForSeconds(times[i]);
            }
            // desvanecer el último destello
            yield return Ease.Tween(0.45f, k => Set(1f - k), Ease.OutCubic, unscaled: false);
            Set(0f);
            yield return new WaitForSeconds(Random.Range(0.6f, 1.8f));
            AudioManager.At(Sfx.Thunder, windowCenter ? windowCenter.position + Vector3.back * 4f : transform.position, 1.5f);
            Haptics.Pulse(90);
        }

        void Set(float k)
        {
            if (lightning)
            {
                lightning.enabled = k > 0.01f;
                lightning.intensity = flashIntensity * k;
            }
            if (sky)
            {
                sky.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", Color.Lerp(skyBase, Color.white * 3.2f, k));
                sky.SetPropertyBlock(mpb);
            }
        }
    }
}
