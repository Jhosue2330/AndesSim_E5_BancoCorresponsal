using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Analista del SOC (modelo hecho en Blender). Reacciona al ambiente:
    /// - Sentado: teclea; con la alerta se echa atrás y se lleva las manos a la cabeza.
    /// - Caminante: patrulla entre puntos; con la alerta corre a las pantallas y señala.
    /// </summary>
    public class NpcAnalyst : MonoBehaviour
    {
        public enum Role { Seated, Walker }
        public Role role = Role.Seated;
        public Animation anim;
        [Header("Caminante")]
        public Vector3[] patrol;
        public Vector3 alertSpot;
        public Vector3 lookAtOnAlert;
        public float walkSpeed = 0.9f;
        public float alertWalkSpeed = 1.6f;

        int target;
        float waitUntil;
        RoomMood lastMood = RoomMood.Calm;
        float alarmUntil;

        void Start()
        {
            if (anim == null) anim = GetComponentInChildren<Animation>();
            if (anim != null) foreach (AnimationState s in anim) { s.wrapMode = WrapMode.Loop; s.speed = 1f; }
            // Desfase: no todos teclean al mismo ritmo
            if (anim != null && anim["Teclear"] != null) anim["Teclear"].time = Random.value;
            Play(role == Role.Seated ? "Teclear" : "Idle");
        }

        void Play(string clip, float fade = 0.3f)
        {
            if (anim == null || anim[clip] == null) return;
            if (!anim.IsPlaying(clip)) anim.CrossFade(clip, fade);
        }

        void Update()
        {
            var mood = AmbienceController.I ? AmbienceController.I.Mood : RoomMood.Calm;
            if (mood != lastMood)
            {
                if (mood == RoomMood.Alert) alarmUntil = Time.time + 9f;
                lastMood = mood;
            }
            bool alarmed = Time.time < alarmUntil || mood == RoomMood.Alert;

            if (role == Role.Seated)
            {
                Play(alarmed ? "Alarma" : "Teclear");
                return;
            }

            // Caminante
            Vector3 goal = alarmed ? alertSpot : (patrol != null && patrol.Length > 0 ? patrol[target] : transform.position);
            Vector3 to = goal - transform.position; to.y = 0f;
            if (to.magnitude > 0.08f)
            {
                if (!alarmed && Time.time < waitUntil) { Play("Idle"); return; }
                float speed = alarmed ? alertWalkSpeed : walkSpeed;
                transform.position += to.normalized * Mathf.Min(speed * Time.deltaTime, to.magnitude);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 6f);
                Play("Caminar", 0.2f);
                if (anim && anim["Caminar"] != null) anim["Caminar"].speed = speed / walkSpeed;
            }
            else if (alarmed)
            {
                Vector3 look = lookAtOnAlert - transform.position; look.y = 0f;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 4f);
                Play("Senalar");
            }
            else
            {
                Play("Idle");
                if (patrol != null && patrol.Length > 0)
                {
                    target = (target + 1) % patrol.Length;
                    waitUntil = Time.time + Random.Range(2.5f, 5f);
                }
            }
        }
    }
}
