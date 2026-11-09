using System.Collections;
using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Punto de teletransporte: mirar y tocar (o mirar fijo) mueve al jugador aquí.
    /// Sin movimiento continuo para evitar mareo.
    /// </summary>
    public class TeleportPoint : GazeInteractable
    {
        [Tooltip("Raíz del jugador (padre de la cámara). Si está vacío, se busca 'PlayerRig'.")]
        public Transform playerRig;
        [Tooltip("Si se asigna, el jugador queda mirando hacia este objeto.")]
        public Transform lookAt;
        public string label = "Ir aquí";
        public float fadeSeconds = 0.15f;

        public static event System.Action<TeleportPoint> Teleported;

        protected override void Awake()
        {
            base.Awake();
            if (playerRig == null) { var go = GameObject.Find("PlayerRig"); if (go) playerRig = go.transform; }
        }

        protected override void Clicked() => TeleportHere();

        public void TeleportHere()
        {
            if (playerRig == null) return;
            StartCoroutine(Move());
        }

        IEnumerator Move()
        {
            AudioManager.P(Sfx.Teleport);
            yield return new WaitForSeconds(fadeSeconds);
            Vector3 p = transform.position;
            playerRig.position = new Vector3(p.x, playerRig.position.y, p.z);
            if (lookAt != null)
            {
                Vector3 dir = Vector3.ProjectOnPlane(lookAt.position - playerRig.position, Vector3.up);
                if (dir.sqrMagnitude > 0.01f) playerRig.rotation = Quaternion.LookRotation(dir);
            }
            Teleported?.Invoke(this);
        }
    }
}
