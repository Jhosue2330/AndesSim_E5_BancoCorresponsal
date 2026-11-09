using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AndesSim
{
    /// <summary>
    /// Instancia única que guarda el modo, las evidencias encontradas y las defensas aplicadas.
    /// Contrato común del curso: RegisterEvidence, ApplyDefense y EndScenario.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;                 // instancia única
        public ScenarioData data;                    // textos, fechas, cifras del escenario
        public SimMode mode = SimMode.Ataque;

        /// <summary>Modo pedido al recargar la escena con <see cref="RestartWithMode"/>.</summary>
        static SimMode? pendingMode;

        readonly HashSet<string> evidences = new HashSet<string>();
        readonly HashSet<string> appliedDefenses = new HashSet<string>();

        public event Action<string> EvidenceRegistered;
        public event Action<string> DefenseApplied;
        public event Action<SimMode> ModeChanged;
        public event Action ScenarioEnded;

        /// <summary>Un escenario puede proveer su propio texto de resultado (p. ej. dinero salvado).</summary>
        public Func<string> ResultTextProvider;

        public bool HasEnded { get; private set; }
        public bool HasStarted { get; private set; }
        public int EvidenceCount => evidences.Count;
        public int DefenseCount => appliedDefenses.Count;
        public int TotalEvidence { get; private set; }
        public int TotalDefenses { get; private set; }
        public IReadOnlyCollection<string> Evidences => evidences;
        public IReadOnlyCollection<string> Defenses => appliedDefenses;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            if (pendingMode.HasValue) { mode = pendingMode.Value; pendingMode = null; }
        }

        void Start()
        {
            TotalEvidence = FindObjectsByType<EvidenceItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            TotalDefenses = FindObjectsByType<DefenseControl>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        void OnDestroy() { if (I == this) I = null; }

        public void SetMode(SimMode newMode)
        {
            mode = newMode;
            ModeChanged?.Invoke(mode);
        }

        /// <summary>Marca el inicio de la simulación (después del portal).</summary>
        public void BeginScenario() => HasStarted = true;

        public bool HasEvidence(string id) => evidences.Contains(id);
        public bool HasDefense(string id) => appliedDefenses.Contains(id);

        /// <summary>Al encontrar una pista.</summary>
        public void RegisterEvidence(string id)
        {
            if (HasEnded || string.IsNullOrEmpty(id) || !evidences.Add(id)) return;
            Debug.Log($"[AndesSim] Evidencia registrada: {id} ({evidences.Count}/{TotalEvidence})");
            EvidenceRegistered?.Invoke(id);
        }

        /// <summary>Al activar una contramedida. Solo tiene efecto en modo Defensa.</summary>
        public void ApplyDefense(string id)
        {
            if (HasEnded || mode != SimMode.Defensa || string.IsNullOrEmpty(id) || !appliedDefenses.Add(id)) return;
            Debug.Log($"[AndesSim] Defensa aplicada: {id} ({appliedDefenses.Count}/{TotalDefenses})");
            DefenseApplied?.Invoke(id);
        }

        /// <summary>Muestra el resultado del modo actual.</summary>
        public void EndScenario()
        {
            if (HasEnded) return;
            HasEnded = true;
            Debug.Log($"[AndesSim] Fin del escenario en modo {mode}");
            ScenarioEnded?.Invoke();
        }

        /// <summary>Texto final: el del escenario si existe, si no el de ScenarioData.</summary>
        public string BuildResultText()
        {
            if (ResultTextProvider != null) return ResultTextProvider();
            if (data == null) return mode.ToString();
            return mode == SimMode.Ataque ? data.resultAttack : data.resultDefense;
        }

        /// <summary>Recarga la escena empezando en el modo indicado.</summary>
        public void RestartWithMode(SimMode newMode)
        {
            pendingMode = newMode;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void RestartAttack() => RestartWithMode(SimMode.Ataque);
        public void RestartDefense() => RestartWithMode(SimMode.Defensa);
    }
}
