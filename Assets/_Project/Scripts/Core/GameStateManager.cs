using UnityEngine;
using UnityEngine.Events;

namespace BallisticSimulator.Core
{
    /// <summary>
    /// Define todos los estados posibles del simulador y gestiona las transiciones entre ellos.
    /// Es la fuente de verdad para saber en qué estado está la aplicación.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static GameStateManager Instance { get; private set; }

        // ── Estado ────────────────────────────────────────────────────────────────
        public enum SimState
        {
            Setup,        // Configurando parámetros, preview activo
            Firing,       // Bala en vuelo (tiempo corre)
            Paused,       // Simulación pausada (bala congelada, cámara libre)
            BatchRunning  // Batch test en progreso
        }

        private SimState _currentState = SimState.Setup;

        public SimState CurrentState => _currentState;

        // ── Eventos ───────────────────────────────────────────────────────────────
        [System.Serializable] public class StateChangeEvent : UnityEvent<SimState> { }
        public StateChangeEvent OnStateChanged;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── API ───────────────────────────────────────────────────────────────────
        public void SetState(SimState newState)
        {
            if (_currentState == newState) return;

            Debug.Log($"[State] {_currentState} → {newState}");
            _currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        // Helpers de acceso rápido
        public bool IsSetup    => _currentState == SimState.Setup;
        public bool IsFiring   => _currentState == SimState.Firing;
        public bool IsPaused   => _currentState == SimState.Paused;
        public bool IsBatch    => _currentState == SimState.BatchRunning;
    }
}
