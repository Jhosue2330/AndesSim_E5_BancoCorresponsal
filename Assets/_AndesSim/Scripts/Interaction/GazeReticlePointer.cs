using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Retícula por mirada (compatible con CardboardReticlePointer).
    /// Lanza un rayo desde el centro de la cámara y envía OnPointerEnter / OnPointerExit / OnPointerClick.
    /// Click: tocar la pantalla (botón del Cardboard), clic del mouse en el Editor,
    /// o mirar fijo <see cref="dwellSeconds"/> segundos.
    /// Si usas el CardboardReticlePointer del plugin, desactiva este componente.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GazeReticlePointer : MonoBehaviour
    {
        public float maxDistance = 20f;
        public LayerMask layerMask = ~0;
        [Tooltip("0 = desactivado")]
        public float dwellSeconds = 2f;
        [Tooltip("En el Editor sin visor, apuntar con el cursor del mouse en vez del centro de la cámara.")]
        public bool useMouseInEditor = true;

        [Header("Retícula")]
        public float reticleRestDistance = 3f;
        public float reticleInnerRadius = 0.006f;
        public float reticleOuterRadius = 0.016f;
        public Color reticleColor = new Color(0.85f, 1f, 0.9f, 0.9f);
        public Color progressColor = new Color(0.22f, 0.95f, 0.54f, 1f);

        GameObject gazed;
        GazeInteractable gazedInteractable;
        float gazeTime;
        bool dwellFired;
        LineRenderer ring, progress;
        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            ring = MakeLine("Reticle", reticleColor, 40);
            progress = MakeLine("ReticleProgress", progressColor, 40);
        }

        LineRenderer MakeLine(string name, Color c, int segments)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.positionCount = segments + 1;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        void Update()
        {
            // En el Editor (sin visor XR) se apunta con el mouse; en el celular, con la mirada.
            bool mouseMode = useMouseInEditor && Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive;
            var ray = mouseMode ? cam.ScreenPointToRay(MousePosition()) : new Ray(transform.position, transform.forward);
            ring.enabled = !mouseMode;
            if (mouseMode) progress.enabled = false;
            GameObject hitObj = null;
            float dist = reticleRestDistance;
            if (Physics.Raycast(ray, out var hit, maxDistance, layerMask, QueryTriggerInteraction.Collide))
            {
                hitObj = hit.collider.gameObject;
                dist = hit.distance;
            }

            if (hitObj != gazed)
            {
                if (gazed != null) gazed.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
                gazed = hitObj;
                gazedInteractable = gazed ? gazed.GetComponent<GazeInteractable>() : null;
                gazeTime = 0f;
                dwellFired = false;
                if (gazed != null) gazed.SendMessage("OnPointerEnter", SendMessageOptions.DontRequireReceiver);
            }

            // Solo objetos con GazeInteractable (o derivados) cuentan como interactivos.
            bool canInteract = gazedInteractable != null && gazedInteractable.interactable && gazedInteractable.isActiveAndEnabled;
            float dwellProgress = 0f;

            if (canInteract)
            {
                if (ClickPressed()) Click();
                bool dwellAllowed = !mouseMode && dwellSeconds > 0f && gazedInteractable.allowDwell;
                if (dwellAllowed && !dwellFired)
                {
                    gazeTime += Time.deltaTime;
                    dwellProgress = Mathf.Clamp01(gazeTime / dwellSeconds);
                    if (gazeTime >= dwellSeconds) Click();
                }
            }

            if (!mouseMode) DrawReticle(dist, canInteract, dwellProgress);
        }

        static Vector3 MousePosition()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
#endif
        }

        void Click()
        {
            dwellFired = true;
            if (gazed != null) gazed.SendMessage("OnPointerClick", SendMessageOptions.DontRequireReceiver);
        }

        static bool ClickPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) return true;
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
#endif
            return false;
        }

        void DrawReticle(float dist, bool expanded, float dwell)
        {
            // El tamaño crece con la distancia para verse siempre igual.
            float z = Mathf.Max(cam.nearClipPlane + 0.05f, dist - 0.02f);
            float r = (expanded ? reticleOuterRadius * 1.6f : reticleOuterRadius) * z;
            float w = (reticleOuterRadius - reticleInnerRadius) * z * (expanded ? 1.2f : 1f);
            SetCircle(ring, r, z, w, 1f);
            progress.enabled = dwell > 0f;
            if (dwell > 0f) SetCircle(progress, r, z - 0.001f, w * 1.5f, dwell);
        }

        static void SetCircle(LineRenderer lr, float radius, float z, float width, float fraction)
        {
            int n = lr.positionCount - 1;
            lr.startWidth = lr.endWidth = width;
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.PI * 0.5f - 2f * Mathf.PI * fraction * i / n;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, z));
            }
        }
    }
}
