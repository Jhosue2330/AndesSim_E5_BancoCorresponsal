using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Minijuego "lupa": el nombre del beneficiario aparece letra por letra en grande y el jugador
    /// debe mirar la letra equivocada. En el caso real, «SHALIKA FANDATION» (por FOUNDATION)
    /// hizo que un banco intermediario detuviera US$20 M.
    /// </summary>
    public class FandationGame : MonoBehaviour
    {
        public static FandationGame I;
        public RectTransform root;
        public RectTransform lettersArea;
        public TMP_Text title, hint;
        public GazeInteractable closeButton;
        public float distance = 2.2f;

        const string Name = "SHALIKA FANDATION";
        const int WrongIndex = 9; // la «A» de FANDATION (debería decir FOUNDATION)
        readonly List<GameObject> letters = new List<GameObject>();
        public bool Solved { get; private set; }

        void Awake()
        {
            I = this;
            if (root) root.gameObject.SetActive(false);
            if (closeButton) closeButton.onClick.AddListener(Close);
        }
        void OnDestroy() { if (I == this) I = null; }

        public void Open(OrderInfo order)
        {
            if (root == null) return;
            var head = UIManager.I ? UIManager.I.head : null;
            if (head)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                root.position = head.position + fwd * distance;
                root.rotation = Quaternion.LookRotation(fwd);
            }
            UIManager.I?.HideMessage();
            root.gameObject.SetActive(true);
            title.text = $"LUPA · {order.AmountText}";
            hint.text = Solved ? $"<color={Palette.Hex(Palette.Secure)}>FANDATION ≠ FOUNDATION</color>"
                               : "Una letra está mal. ¡Tócala!";
            Build();
            AudioManager.P(Sfx.Swoosh);
        }

        void Build()
        {
            foreach (var g in letters) Destroy(g);
            letters.Clear();
            float w = 96f, gap = 8f, total = Name.Length * (w + gap);
            for (int i = 0; i < Name.Length; i++)
            {
                char ch = Name[i];
                if (ch == ' ') continue;
                var go = new GameObject("Letra_" + i, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(lettersArea, false);
                rt.sizeDelta = new Vector2(w, 140f);
                rt.anchoredPosition = new Vector2(-total / 2f + i * (w + gap) + w / 2f, 0f);
                var img = go.GetComponent<Image>();
                img.color = Palette.PanelLight;
                var col = go.AddComponent<BoxCollider>(); col.size = new Vector3(w, 140f, 4f);
                var gi = go.AddComponent<GazeInteractable>();
                gi.highlightGraphic = img; gi.highlightColor = Color.Lerp(Palette.PanelLight, Palette.Warning, 0.5f);
                gi.allowDwell = false; // aquí hay que tocar: mirar fijo no basta (evita aciertos por azar)
                var tgo = new GameObject("T", typeof(RectTransform));
                var trt = (RectTransform)tgo.transform; trt.SetParent(rt, false);
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
                var t = tgo.AddComponent<TextMeshProUGUI>();
                t.text = ch.ToString(); t.fontSize = 96; t.alignment = TextAlignmentOptions.Center; t.color = Palette.Ink; t.fontStyle = FontStyles.Bold;
                t.raycastTarget = false;
                if (Solved && i == WrongIndex) { img.color = Palette.Threat; gi.interactable = false; }
                int index = i;
                gi.onClick.AddListener(() => Pick(index, rt, img));
                letters.Add(go);
            }
        }

        void Pick(int index, RectTransform rt, Image img)
        {
            if (Solved) return;
            if (index == WrongIndex)
            {
                Solved = true;
                img.color = Palette.Threat;
                hint.text = $"<color={Palette.Hex(Palette.Secure)}>¡EXACTO! FANDATION ≠ FOUNDATION</color>";
                AudioManager.P(Sfx.Achievement);
                Haptics.Confirm();
                FxManager.I?.Burst(rt.position, Palette.Warning, 30, 1.8f);
                Achievements.Unlock("detective");
            }
            else
            {
                AudioManager.P(Sfx.Fail, 0.5f);
                Haptics.Tap();
                StartCoroutine(Shake(rt));
            }
        }

        IEnumerator Shake(RectTransform rt)
        {
            Vector2 p = rt.anchoredPosition;
            yield return Ease.Tween(0.3f, k => rt.anchoredPosition = p + new Vector2(Mathf.Sin(k * 40f) * 10f * (1f - k), 0f), Ease.Linear, unscaled: false);
            rt.anchoredPosition = p;
        }

        /// <summary>Modo demo: toca la letra correcta.</summary>
        public void SolveForDemo()
        {
            foreach (var g in letters)
                if (g && g.name == "Letra_" + WrongIndex) g.GetComponent<GazeInteractable>().OnPointerClick();
        }

        public void Close()
        {
            if (root) root.gameObject.SetActive(false);
        }
    }
}
