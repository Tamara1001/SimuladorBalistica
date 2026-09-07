using BallisticSimulator.Core;
using UnityEngine;

namespace BallisticSimulator.Camera
{
    /// <summary>
    /// Gestiona la transición entre la cámara de setup (fija) y la cámara libre.
    /// Se suscribe al GameStateManager para cambiar automáticamente de modo.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        // ── Referencias ───────────────────────────────────────────────────────────
        [Header("Cámaras")]
        [SerializeField] private UnityEngine.Camera _setupCamera;
        [SerializeField] private UnityEngine.Camera _freeCamera;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Estado inicial: sólo la cámara de setup activa
            if (_setupCamera != null) _setupCamera.gameObject.SetActive(true);
            if (_freeCamera  != null) _freeCamera.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged.AddListener(OnStateChanged);
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
        }

        // ── Manejo de estados ─────────────────────────────────────────────────────
        private void OnStateChanged(GameStateManager.SimState newState)
        {
            switch (newState)
            {
                case GameStateManager.SimState.Setup:
                case GameStateManager.SimState.BatchRunning:
                    ActivateSetupCamera();
                    break;

                case GameStateManager.SimState.Firing:
                    ActivateFreeCamera();
                    break;

                case GameStateManager.SimState.Paused:
                    // La cámara libre permanece activa durante la pausa
                    break;
            }
        }

        // ── Transiciones ──────────────────────────────────────────────────────────
        private void ActivateSetupCamera()
        {
            if (_freeCamera  != null) _freeCamera.gameObject.SetActive(false);
            if (_setupCamera != null) _setupCamera.gameObject.SetActive(true);
        }

        private void ActivateFreeCamera()
        {
            if (_freeCamera == null || _setupCamera == null) return;

            // Teleportar la cámara libre a la posición actual de la cámara de setup
            // ANTES de activarla, para que OnEnable sincronice desde ahí
            _freeCamera.transform.SetPositionAndRotation(
                _setupCamera.transform.position,
                _setupCamera.transform.rotation);

            _setupCamera.gameObject.SetActive(false);
            _freeCamera.gameObject.SetActive(true); // dispara OnEnable → sincroniza target
        }
    }
}
