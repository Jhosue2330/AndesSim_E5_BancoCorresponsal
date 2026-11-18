using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Panel de misión que acompaña al jugador (abajo de la mirada, como un visor):
    /// objetivo actual resaltado, pasos cumplidos en verde, evidencias y defensas contadas.
    /// Lee el estado del juego; no necesita que nadie le avise.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        public Transform panel;
        public TMP_Text header;
        public TMP_Text body;
        public OrderFeed feed;
        public CallbackDefense phone;
        public Scenario05Controller controller;
        public float distance = 1.6f;
        public float heightOffset = -0.58f;
        [Tooltip("Grados a la izquierda de la mirada: el panel queda abajo-izquierda, como un visor.")]
        public float yawOffset = -32f;
        public float followSpeed = 2.5f;

        struct Step { public Func<bool> done; public Func<string> label; }
        readonly List<Step> steps = new List<Step>();
        Transform head;
        float nextRefresh;
        int lastCurrent = -1;
        bool built;
        Vector3 baseScale = Vector3.one;

        void Start()
        {
            head = UIManager.I ? UIManager.I.head : Camera.main?.transform;
            if (panel) { baseScale = panel.localScale; panel.gameObject.SetActive(false); }
            PortalEntrada.Crossed += Build;
        }

        void OnDestroy() => PortalEntrada.Crossed -= Build;

        void Build()
        {
            var gm = GameManager.I;
            steps.Clear();
            bool defense = gm != null && gm.mode == SimMode.Defensa;
            if (defense)
                Add(() => gm.DefenseCount >= 3, () => $"Activa las 3 defensas en el panel ({gm.DefenseCount}/3)");
            Add(() => feed && (feed.Running || feed.Finished), () => "Inicia el monitoreo (botón verde)");
            Add(() => feed && feed.alert && feed.alert.Triggered, () => "Vigila las pantallas y la mesa holográfica");
            Add(() => feed && feed.BlockedByPlayer >= 3, () => $"Bloquea órdenes EN REVISIÓN mirándolas ({(feed ? feed.BlockedByPlayer : 0)}/3)");
            Add(() => gm.EvidenceCount >= gm.TotalEvidence, () => $"Encuentra las evidencias ({gm.EvidenceCount}/{gm.TotalEvidence})");
            Add(() => phone && phone.CallDone, () => "Llama al BANCO ANDINO (escritorio)");
            Add(() => gm.HasEnded, () => "Ver resultado");
            built = true;
            if (panel)
            {
                panel.gameObject.SetActive(true);
                SnapInFront();
            }
            if (header) header.text = defense ? "MISIÓN · DEFENDER EL CORRESPONSAL" : "MISIÓN · REVIVIR EL ATAQUE 2016";
        }

        void Add(Func<bool> done, Func<string> label) => steps.Add(new Step { done = done, label = label });

        void LateUpdate()
        {
            if (!built || panel == null || head == null) return;
            if (GameManager.I != null && GameManager.I.HasEnded) { panel.gameObject.SetActive(false); return; }
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = panel.forward;
            fwd = Quaternion.Euler(0f, yawOffset, 0f) * fwd;
            Vector3 target = head.position + fwd * distance + Vector3.up * heightOffset;
            panel.position = Vector3.Lerp(panel.position, target, Time.deltaTime * followSpeed);
            Vector3 look = panel.position - head.position;
            panel.rotation = Quaternion.Slerp(panel.rotation, Quaternion.LookRotation(look), Time.deltaTime * followSpeed * 2f);

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }

        void SnapInFront()
        {
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd = Quaternion.Euler(0f, yawOffset, 0f) * fwd;
            panel.position = head.position + fwd * distance + Vector3.up * heightOffset;
            panel.rotation = Quaternion.LookRotation(panel.position - head.position);
        }

        void Refresh()
        {
            var sb = new StringBuilder();
            int current = -1;
            string ok = Palette.Hex(Palette.Secure), now = Palette.Hex(Palette.Warning), dim = Palette.Hex(Palette.InkDim);
            for (int i = 0; i < steps.Count; i++)
            {
                bool d = steps[i].done();
                if (!d && current < 0) current = i;
                if (d) sb.Append($"<color={ok}>•  <s>{steps[i].label()}</s></color>\n");
                else if (i == current) sb.Append($"<color={now}><b>›  {steps[i].label()}</b></color>\n");
                else sb.Append($"<color={dim}>–  {steps[i].label()}</color>\n");
            }
            if (body) body.text = sb.ToString();
            if (current != lastCurrent && lastCurrent >= 0)
            {
                AudioManager.P(Sfx.Evidence, 0.4f);
                StartCoroutine(Ease.Tween(0.35f, k => panel.localScale = baseScale * Mathf.Lerp(1.08f, 1f, k), Ease.OutBack));
            }
            lastCurrent = current;
        }
    }
}
