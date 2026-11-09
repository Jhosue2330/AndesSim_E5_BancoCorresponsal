using UnityEngine;
using UnityEngine.XR;

namespace AndesSim
{
    /// <summary>
    /// Rotación de cabeza cuando NO hay XR activo:
    /// en el Editor con el mouse (clic derecho o Alt + mover), en el celular con el giroscopio.
    /// Cuando Cardboard está activo, el plugin mueve la cámara y este script no hace nada.
    /// </summary>
    public class HeadLookFallback : MonoBehaviour
    {
        public float mouseSensitivity = 3f;
        public bool useGyroOnDevice = true;

        float yaw, pitch;
        bool gyroOn;

        void Start()
        {
            var e = transform.localEulerAngles;
            yaw = e.y; pitch = e.x > 180 ? e.x - 360 : e.x;
            if (useGyroOnDevice && !Application.isEditor && SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                gyroOn = true;
            }
        }

        void Update()
        {
            if (XRSettings.isDeviceActive) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (gyroOn)
            {
                Quaternion q = Input.gyro.attitude;
                q = new Quaternion(q.x, q.y, -q.z, -q.w);
                transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * q;
                return;
            }
            bool look = Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (!look) return;
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -80f, 80f);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
#endif
        }
    }
}
