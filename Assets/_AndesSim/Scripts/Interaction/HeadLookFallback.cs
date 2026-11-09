using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace AndesSim
{
    /// <summary>
    /// Rotación de la cabeza del jugador:
    /// - Con Cardboard (XR activo): lee la orientación del visor desde el subsistema XR.
    /// - En el Editor sin XR: clic derecho o Alt + mover el mouse.
    /// - En un celular sin XR: giroscopio.
    /// </summary>
    public class HeadLookFallback : MonoBehaviour
    {
        public float mouseSensitivity = 3f;
        public bool useGyroOnDevice = true;
        [Tooltip("Aplicar también la posición del visor (Cardboard solo rota; se deja apagado).")]
        public bool applyXRPosition = false;

        float yaw, pitch;
        bool gyroOn;
        Vector3 basePosition;
        readonly List<InputDevice> devices = new List<InputDevice>();

        void Start()
        {
            basePosition = transform.localPosition;
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
            if (XRSettings.isDeviceActive)
            {
                ApplyXRHeadPose();
                return;
            }
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

        // Se aplica también justo antes de renderizar para reducir la latencia.
        void OnEnable() => Application.onBeforeRender += OnBeforeRender;
        void OnDisable() => Application.onBeforeRender -= OnBeforeRender;
        void OnBeforeRender() { if (XRSettings.isDeviceActive) ApplyXRHeadPose(); }

        void ApplyXRHeadPose()
        {
            if (!TryGetHead(out var device)) return;
            if (device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rot) ||
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
                transform.localRotation = rot;
            if (applyXRPosition &&
                (device.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 pos) ||
                 device.TryGetFeatureValue(CommonUsages.devicePosition, out pos)))
                transform.localPosition = basePosition + pos;
        }

        bool TryGetHead(out InputDevice device)
        {
            device = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (device.isValid) return true;
            device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (device.isValid) return true;
            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count > 0) { device = devices[0]; return true; }
            return false;
        }
    }
}
