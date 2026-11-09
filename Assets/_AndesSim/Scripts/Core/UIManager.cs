using System.Collections;
using TMPro;
using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Interfaz en el espacio 3D (en VR no existe la pantalla plana):
    /// mensajes que siguen la mirada, línea de estado y pantalla final.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I;

        [Header("Cámara del jugador")]
        public Transform head;

        [Header("Mensaje flotante (Canvas World Space)")]
        public Transform toastRoot;
        public TMP_Text toastTitle;
        public TMP_Text toastBody;
        public float toastDistance = 2.2f;
        public float toastHeightOffset = -0.25f;
        public float followSpeed = 3f;

        [Header("Estado (opcional)")]
        public TMP_Text statusText;

        [Header("Pantalla final")]
        public GameObject endScreenRoot;
        public TMP_Text endTitle;
        public TMP_Text endBody;
        public TMP_Text endCredits;
        public float endDistance = 2.6f;

        Coroutine toastRoutine;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            if (toastRoot) toastRoot.gameObject.SetActive(false);
            if (endScreenRoot) endScreenRoot.SetActive(false);
        }

        void Start()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            gm.EvidenceRegistered += _ => RefreshStatus();
            gm.DefenseApplied += _ => RefreshStatus();
            gm.ModeChanged += _ => RefreshStatus();
            gm.ScenarioEnded += ShowEndScreen;
            RefreshStatus();
        }

        void OnDestroy() { if (I == this) I = null; }

        void LateUpdate()
        {
            if (head == null || toastRoot == null || !toastRoot.gameObject.activeSelf) return;
            // Sigue la mirada con suavidad (solo en horizontal para no marear).
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = toastRoot.forward;
            Vector3 target = head.position + fwd * toastDistance + Vector3.up * toastHeightOffset;
            toastRoot.position = Vector3.Lerp(toastRoot.position, target, Time.deltaTime * followSpeed);
            toastRoot.rotation = Quaternion.Slerp(toastRoot.rotation, Quaternion.LookRotation(fwd), Time.deltaTime * followSpeed);
        }

        /// <summary>Muestra un mensaje frente al jugador durante unos segundos.</summary>
        public void ShowMessage(string title, string body, float seconds = 5f)
        {
            if (toastRoot == null) { Debug.Log($"[UI] {title}: {body}"); return; }
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(title, body, seconds));
        }

        public static void Msg(string title, string body, float seconds = 5f)
        {
            if (I != null) I.ShowMessage(title, body, seconds);
            else Debug.Log($"[UI] {title}: {body}");
        }

        IEnumerator ToastRoutine(string title, string body, float seconds)
        {
            if (toastTitle) toastTitle.text = title;
            if (toastBody) toastBody.text = body;
            bool wasHidden = !toastRoot.gameObject.activeSelf;
            toastRoot.gameObject.SetActive(true);
            if (wasHidden && head != null) SnapInFront(toastRoot, toastDistance, toastHeightOffset);
            yield return new WaitForSeconds(seconds);
            toastRoot.gameObject.SetActive(false);
            toastRoutine = null;
        }

        public void HideMessage()
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = null;
            if (toastRoot) toastRoot.gameObject.SetActive(false);
        }

        public void RefreshStatus()
        {
            var gm = GameManager.I;
            if (statusText == null || gm == null) return;
            string modo = gm.mode == SimMode.Ataque ? "<color=#ff5a4e>ATAQUE 2016</color>" : "<color=#38f28a>DEFENSA</color>";
            string def = gm.mode == SimMode.Defensa ? $"  ·  Defensas {gm.DefenseCount}/{gm.TotalDefenses}" : "";
            statusText.text = $"Modo {modo}  ·  Evidencias {gm.EvidenceCount}/{gm.TotalEvidence}{def}";
        }

        void ShowEndScreen()
        {
            HideMessage();
            var gm = GameManager.I;
            if (endScreenRoot == null) { Debug.Log("[UI] FIN\n" + gm.BuildResultText()); return; }
            if (endTitle) endTitle.text = gm.mode == SimMode.Ataque ? "RESULTADO · ATAQUE 2016" : "RESULTADO · CON DEFENSA";
            if (endBody) endBody.text = gm.BuildResultText();
            if (endCredits && gm.data) endCredits.text = gm.data.credits;
            endScreenRoot.SetActive(true);
            if (head != null) SnapInFront(endScreenRoot.transform, endDistance, 0f);
        }

        void SnapInFront(Transform t, float distance, float heightOffset)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            t.position = head.position + fwd * distance + Vector3.up * heightOffset;
            t.rotation = Quaternion.LookRotation(fwd);
        }
    }
}
