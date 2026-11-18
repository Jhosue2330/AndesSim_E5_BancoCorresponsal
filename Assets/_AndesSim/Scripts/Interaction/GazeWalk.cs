using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Caminar libre en Cardboard: mantener presionada la pantalla (o el botón del visor)
    /// mientras se mira el piso → el jugador avanza hacia donde mira.
    /// Comodidad VR: velocidad constante, sin aceleración brusca y un "túnel" que oscurece
    /// los bordes de la vista mientras se camina (reduce el mareo). El teletransporte sigue disponible.
    /// </summary>
    public class GazeWalk : MonoBehaviour
    {
        public Transform rig;
        public Transform head;
        public Renderer tunnel;
        public float speed = 1.3f;
        public float holdToWalk = 0.3f;
        [Tooltip("Hay que mirar al menos este ángulo hacia abajo para caminar.")]
        public float minPitchDown = 12f;
        public Vector2 boundsX = new Vector2(-6.4f, 6.4f);
        public Vector2 boundsZ = new Vector2(-5.5f, 4.6f);
        public float bodyRadius = 0.28f;

        float held, tunnelAlpha;
        MaterialPropertyBlock mpb;

        public bool Walking { get; private set; }
        public static float DistanceWalked { get; private set; }

        void Awake() { mpb = new MaterialPropertyBlock(); SetTunnel(0f); DistanceWalked = 0f; }

        void Update()
        {
            var gm = GameManager.I;
            bool allowed = gm != null && gm.HasStarted && !gm.HasEnded && rig && head;
            bool pressing = allowed && Pressing();
            held = pressing ? held + Time.deltaTime : 0f;

            float pitch = Vector3.Angle(Vector3.ProjectOnPlane(head.forward, Vector3.up), head.forward) * (head.forward.y < 0 ? 1 : -1);
            Walking = pressing && held >= holdToWalk && pitch >= minPitchDown && !GazingInteractable();

            if (Walking)
            {
                Vector3 dir = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                Vector3 step = dir * speed * Time.deltaTime;
                Vector3 chest = new Vector3(head.position.x, rig.position.y + 1.0f, head.position.z);
                if (!Physics.SphereCast(chest, bodyRadius, dir, out _, step.magnitude + bodyRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    Vector3 p = rig.position + step;
                    p.x = Mathf.Clamp(p.x, boundsX.x, boundsX.y);
                    p.z = Mathf.Clamp(p.z, boundsZ.x, boundsZ.y);
                    DistanceWalked += (p - rig.position).magnitude;
                    rig.position = p;
                }
            }
            tunnelAlpha = Mathf.MoveTowards(tunnelAlpha, Walking ? 1f : 0f, Time.deltaTime * 4f);
            SetTunnel(tunnelAlpha);
        }

        bool GazingInteractable()
        {
            if (Physics.Raycast(head.position, head.forward, out var hit, 20f, ~0, QueryTriggerInteraction.Collide))
            {
                var gi = hit.collider.GetComponent<GazeInteractable>();
                return gi != null && gi.interactable;
            }
            return false;
        }

        static bool Pressing()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0)) return true;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var ph = Input.GetTouch(i).phase;
                if (ph == TouchPhase.Stationary || ph == TouchPhase.Moved || ph == TouchPhase.Began) return true;
            }
#endif
            return false;
        }

        void SetTunnel(float a)
        {
            if (!tunnel) return;
            tunnel.enabled = a > 0.01f;
            tunnel.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", Palette.WithAlpha(Palette.Void, 0.92f * a));
            tunnel.SetPropertyBlock(mpb);
        }
    }
}
