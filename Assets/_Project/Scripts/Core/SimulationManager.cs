using System;
using System.Collections;
using BallisticSimulator.Data;
using BallisticSimulator.Physics;
using BallisticSimulator.Targets;
using UnityEngine;

namespace BallisticSimulator.Core
{
    /// <summary>
    /// Orquestador central del simulador.
    /// Coordina: disparo, reset, recolección de datos, y batch testing.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static SimulationManager Instance { get; private set; }

        // ── Referencias (asignar en Inspector o via Editor Tool) ──────────────────
        [Header("Componentes de escena")]
        [SerializeField] private BulletController  _bulletController;
        [SerializeField] private TrajectoryPreview _trajectoryPreview;
        [SerializeField] private TargetSpawner     _targetSpawner;
        [SerializeField] private Transform         _muzzleTransform;
        [SerializeField] private Transform         _gunBaseTransform;

        // ── Parámetros actuales (sincronizados desde SidePanelUI) ─────────────────
        [HideInInspector] public float  AngleDegrees    = 10f;
        [HideInInspector] public float  InitialVelocity = 45f;
        [HideInInspector] public float  BulletMassG     = 8f;
        [HideInInspector] public float  BulletRadiusMm  = 4.5f;
        [HideInInspector] public float  Gravity         = 9.81f;
        [HideInInspector] public float  TimeScale       = 1.0f;
        [HideInInspector] public string PresetName      = "9mm Parabellum";

        // ── Sesión activa ──────────────────────────────────────────────────────────
        private SessionData _session;
        private ShotData    _currentShot;
        private int         _boxesHitThisShot;

        // ── Batch ─────────────────────────────────────────────────────────────────
        private bool _batchMode;
        private bool _batchShotComplete;

        // ── Eventos ───────────────────────────────────────────────────────────────
        /// <summary>Se dispara al completarse un disparo (landed o hit).</summary>
        public event Action<ShotData> OnShotCompleted;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _session = new SessionData();
        }

        private void Start()
        {
            if (_bulletController != null)
            {
                _bulletController.OnLanded += HandleLanded;
                _bulletController.OnHit    += HandleHit;
            }
            RefreshPreview();
        }

        private void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = Mathf.Max(0.05f, TimeScale);
            }
        }

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>Recalcula y muestra el preview de trayectoria. Solo actúa en estado Setup.</summary>
        public void RefreshPreview()
        {
            // Rotar el modelo 3D del cañón según el ángulo actual
            if (_gunBaseTransform != null)
            {
                // Si el objeto asignado tiene un hijo 'CannonPivot' o es el pivot en sí, rotar adecuadamente
                Transform pivotToRotate = _gunBaseTransform.name == "CannonPivot" ? 
                    _gunBaseTransform : _gunBaseTransform.Find("CannonPivot");

                if (pivotToRotate != null)
                {
                    pivotToRotate.localRotation = Quaternion.Euler(0f, 0f, AngleDegrees);
                }
                else
                {
                    _gunBaseTransform.localRotation = Quaternion.Euler(0f, 0f, AngleDegrees);
                }
            }

            if (_trajectoryPreview == null || _muzzleTransform == null) return;
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsSetup) return;

            _trajectoryPreview.UpdatePreview(
                _muzzleTransform.position,
                AngleDegrees,
                InitialVelocity,
                Gravity);
        }

        /// <summary>Dispara la bala. Solo válido en estado Setup.</summary>
        public void Fire()
        {
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsSetup) return;
            LaunchInternal();
        }

        /// <summary>Resetea la escena: bala, targets y preview.</summary>
        public void Reset()
        {
            if (_bulletController != null)
                _bulletController.gameObject.SetActive(false);

            _targetSpawner?.ResetTargets();
            _trajectoryPreview?.Show();
            RefreshPreview();

            if (!_batchMode && GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(GameStateManager.SimState.Setup);
        }

        /// <summary>Alterna pausa / reanuda la simulación.</summary>
        public void TogglePause()
        {
            var state = GameStateManager.Instance;
            if (state.IsFiring)
            {
                _bulletController?.SetPaused(true);
                state.SetState(GameStateManager.SimState.Paused);
            }
            else if (state.IsPaused)
            {
                _bulletController?.SetPaused(false);
                state.SetState(GameStateManager.SimState.Firing);
            }
        }

        /// <summary>Avanza un frame cuando está en pausa.</summary>
        public void StepFrame()
        {
            if (GameStateManager.Instance.IsPaused)
                _bulletController?.StepOneFrame();
        }

        /// <summary>Notifica que una caja fue golpeada (llamado por TargetBox.ReceiveHit).</summary>
        public void RegisterBoxHit() => _boxesHitThisShot++;

        /// <summary>Exporta la sesión actual a CSV y abre la carpeta.</summary>
        public string ExportSession()
        {
            string path = CSVExporter.Export(_session);
            if (path != null) CSVExporter.OpenExportFolder();
            return path;
        }

        // ── Batch Testing ─────────────────────────────────────────────────────────

        /// <summary>
        /// Lanza un batch test iterando sobre rangos de ángulo y velocidad.
        /// Llamado desde SidePanelUI via coroutine.
        /// </summary>
        public IEnumerator RunBatch(
            float angleMin, float angleMax, float angleStep,
            float velMin,   float velMax,   float velStep,
            Action<int, int> onProgress = null)
        {
            _batchMode = true;
            GameStateManager.Instance.SetState(GameStateManager.SimState.BatchRunning);

            // Calcular total de disparos
            int total = 0;
            for (float a = angleMin; a <= angleMax + 0.001f; a += Mathf.Max(0.1f, angleStep))
                for (float v = velMin;   v <= velMax + 0.001f;   v += Mathf.Max(1f,   velStep))
                    total++;

            int done = 0;

            for (float angle = angleMin; angle <= angleMax + 0.001f; angle += Mathf.Max(0.1f, angleStep))
            {
                for (float vel = velMin; vel <= velMax + 0.001f; vel += Mathf.Max(1f, velStep))
                {
                    AngleDegrees    = angle;
                    InitialVelocity = vel;

                    _batchShotComplete = false;
                    _targetSpawner?.ResetTargets();
                    LaunchInternal();

                    // Esperar a que el disparo complete
                    yield return new WaitUntil(() => _batchShotComplete);

                    done++;
                    onProgress?.Invoke(done, total);

                    yield return null; // un frame de respiro entre disparos
                }
            }

            _batchMode = false;
            ExportSession();
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(GameStateManager.SimState.Setup);
            _trajectoryPreview?.Show();
            RefreshPreview();
        }

        // ── Callbacks de la bala ──────────────────────────────────────────────────
        private void HandleLanded(Vector3 pos, float time)
        {
            FinalizeShot(pos, time, hit: false);
            _batchShotComplete = true;
            if (!_batchMode) Reset();
        }

        private void HandleHit(Vector3 pos, float time, GameObject target)
        {
            // Aplicar física de impacto a la caja
            var box = target.GetComponent<TargetBox>();
            if (box != null)
            {
                float bulletMassKg  = BulletMassG / 1000f;
                float speed         = BulletPhysics.Speed(AngleDegrees, InitialVelocity, Gravity, time);
                float kineticEnergy = 0.5f * bulletMassKg * speed * speed;
                Vector3 dir         = BulletPhysics.Velocity(AngleDegrees, InitialVelocity, Gravity, time).normalized;

                // Factor de escala para dar un efecto visible sin ser exagerado
                box.ReceiveHit(pos, dir, kineticEnergy * 0.02f);
            }

            FinalizeShot(pos, time, hit: true);
            _batchShotComplete = true;

            if (_batchMode)
            {
                // En batch: reset mínimo para continuar (sin volver a Setup todavía)
                if (_bulletController != null)
                    _bulletController.gameObject.SetActive(false);
            }
            // En modo normal: el usuario ve el ragdoll y presiona Reset manualmente
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private void LaunchInternal()
        {
            if (_bulletController == null || _muzzleTransform == null) return;

            _boxesHitThisShot = 0;
            _currentShot      = BuildShotData();

            _bulletController.Origin          = _muzzleTransform.position;
            _bulletController.AngleDegrees    = AngleDegrees;
            _bulletController.InitialVelocity = InitialVelocity;
            _bulletController.BulletRadiusMm  = BulletRadiusMm;
            _bulletController.Gravity         = Gravity;
            _bulletController.gameObject.SetActive(true);
            _bulletController.Launch(0f);

            _trajectoryPreview?.Hide();
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(GameStateManager.SimState.Firing);
        }

        private void FinalizeShot(Vector3 impactPos, float time, bool hit)
        {
            if (_currentShot == null) return;

            _currentShot.ImpactHit         = hit;
            _currentShot.ImpactX           = impactPos.x;
            _currentShot.ImpactY           = impactPos.y;
            _currentShot.ImpactZ           = impactPos.z;
            _currentShot.FlightTimeSeconds = time;
            _currentShot.MaxHeightM        = _bulletController?.MaxHeightReached ?? 0f;
            _currentShot.BoxesHit          = _boxesHitThisShot;
            _currentShot.Timestamp         = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Distancia horizontal recorrida
            if (_muzzleTransform != null)
                _currentShot.RangeM = Vector3.Distance(
                    new Vector3(_muzzleTransform.position.x, 0f, _muzzleTransform.position.z),
                    new Vector3(impactPos.x, 0f, impactPos.z));

            _session.AddShot(_currentShot);
            DatabaseManager.Instance?.InsertShot(_currentShot);
            OnShotCompleted?.Invoke(_currentShot);

            Debug.Log($"[Sim] Disparo #{_currentShot.ShotId} | " +
                      $"Impacto={hit} | Rango={_currentShot.RangeM:F1}m | " +
                      $"AltMax={_currentShot.MaxHeightM:F1}m | T={time:F2}s");
        }

        private ShotData BuildShotData()
        {
            var cfg = _targetSpawner?.Config ?? new TargetGridConfig();
            return new ShotData
            {
                PresetName      = PresetName,
                AngleDegrees    = AngleDegrees,
                InitialVelocity = InitialVelocity,
                BulletMassGrams = BulletMassG,
                BulletRadiusMm  = BulletRadiusMm,
                Gravity         = Gravity,
                BoxCount        = cfg.TotalBoxes,
                BoxSizeM        = cfg.BoxSize,
                BoxMassKg       = cfg.BoxMass,
                BoxDistanceM    = cfg.Distance,
                GridRows        = cfg.Rows,
                GridColumns     = cfg.Columns,
            };
        }
    }
}
