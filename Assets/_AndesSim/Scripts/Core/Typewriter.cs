using System.Collections;
using TMPro;
using UnityEngine;

namespace AndesSim
{
    /// <summary>Escribe un texto TextMeshPro letra por letra, con un "tic" de teclado.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class Typewriter : MonoBehaviour
    {
        public float charsPerSecond = 55f;
        public bool playOnEnable = false;
        TMP_Text text;
        Coroutine routine;

        public bool Done { get; private set; } = true;

        void Awake() => text = GetComponent<TMP_Text>();
        void OnEnable() { if (playOnEnable) Play(); }

        public void Play()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run());
        }

        public void Skip()
        {
            if (routine != null) StopCoroutine(routine);
            text.maxVisibleCharacters = 99999;
            Done = true;
        }

        IEnumerator Run()
        {
            Done = false;
            text.ForceMeshUpdate();
            int total = text.textInfo.characterCount;
            text.maxVisibleCharacters = 0;
            float shown = 0f; int last = 0;
            while (shown < total)
            {
                shown += charsPerSecond * Time.unscaledDeltaTime;
                int n = Mathf.Min(total, (int)shown);
                if (n != last && n % 3 == 0) AudioManager.P(Sfx.Typing, 0.35f);
                text.maxVisibleCharacters = last = n;
                yield return null;
            }
            text.maxVisibleCharacters = 99999;
            Done = true;
        }
    }
}
