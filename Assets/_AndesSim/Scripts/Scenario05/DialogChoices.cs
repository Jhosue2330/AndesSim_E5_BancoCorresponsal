using System.Collections;
using TMPro;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Panel de diálogo con opciones elegidas con la mirada (para la llamada al BANCO ANDINO).
    /// </summary>
    public class DialogChoices : MonoBehaviour
    {
        public GameObject root;
        public TMP_Text speaker, line;
        public GazeInteractable[] buttons;
        public TMP_Text[] labels;

        int picked = -1;

        void Awake()
        {
            if (root) root.SetActive(false);
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                if (buttons[i]) buttons[i].onClick.AddListener(() => picked = k);
            }
        }

        /// <summary>Muestra una línea de diálogo y espera que el jugador elija una opción.</summary>
        public IEnumerator Ask(string who, string text, string[] options, System.Action<int> onPick)
        {
            root.SetActive(true);
            speaker.text = who;
            line.text = text;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i < options.Length;
                buttons[i].gameObject.SetActive(on);
                if (on) labels[i].text = options[i];
            }
            picked = -1;
            while (picked < 0) yield return null;
            onPick?.Invoke(picked);
        }

        /// <summary>Muestra una respuesta sin opciones durante unos segundos.</summary>
        public IEnumerator Say(string who, string text, float seconds)
        {
            root.SetActive(true);
            speaker.text = who; line.text = text;
            foreach (var b in buttons) b.gameObject.SetActive(false);
            yield return new WaitForSeconds(seconds);
        }

        public void Hide() { if (root) root.SetActive(false); }

        public bool WaitingChoice => root && root.activeSelf && picked < 0 && buttons.Length > 0 && buttons[0].gameObject.activeSelf;

        /// <summary>Elegir por código (modo demo).</summary>
        public void Choose(int i) => picked = i;
    }
}
