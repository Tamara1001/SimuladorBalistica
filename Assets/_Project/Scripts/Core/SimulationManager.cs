using System;
using System.Collections;
using BallisticSimulator.Camera;
using BallisticSimulator.Data;
using BallisticSimulator.Physics;
using BallisticSimulator.Targets;
using UnityEngine;

namespace BallisticSimulator.Core
{
    /// <summary>
    /// Controller central del simulador (MVC).
    /// Coordina: disparo, reset, recolección de datos y batch testing.
    ///
    /// MVC: este es el CONTROLLER.
    ///   - Posee el MODEL (BallisticParameters _params).
    ///   - Recibe acciones de la VIEW (SidePanelUI) a través de métodos públicos.
    ///   - Nunca escribe en la UI directamente; notifica cambios via eventos.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static SimulationManager Instance { get; private set; }

        // ── Referencias de escena (asignar en Inspector o via Editor Tool) ────────
        [Header("Componentes de escena")]
        [SerializeField] private BulletController  _bulletController;
        [SerializeField] private TrajectoryPreview _trajectoryPreview;
        [SerializeField] private TargetSpawner     _targetSpawner;
        [SerializeField] private Transform         _muzzleTransform;
        [SerializeField] private Transform         _gunBaseTransform;

        [Header("Cámara PiP")]
        [SerializeField] private BulletPiPCamera _pipCamera;

        // ── MODEL: parámetros activos de la simulación ────────────────────────────
        // Accesible desde el Inspector para depuración; solo el Controller lo muta.
        [Header("Parámetros actuales (Model)")]
        [SerializeField] private BallisticParameters _params = new BallisticParameters();

        // Propiedad de solo lectura para que la View pueda leer el estado si necesita
        public BallisticParameters Params => _params;

        // ── Sesión activa ─────────────────────────────────────────────────────────
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
                Time.timeScale = 0f;
            else
                Time.timeScale = Mathf.Max(0.05f, _params.TimeScale);
        }

        // ── API pública — acciones (Controller recibe intenciones de la View) ─────

        /// <summary>Recalcula y muestra el preview de trayectoria. Solo actúa en estado Setup.</summary>
        public void RefreshPreview()
        {
            // Rotar el modelo 3D del cañón según el ángulo actual del Model
            if (_gunBaseTransform != null)
            {
                Transform pivotToRotate = _gunBaseTransform.name == "CannonPivot"
                    ? _gunBaseTransform
                    : _gunBaseTransform.Find("CannonPivot");

                if (pivotToRotate != null)
                    pivotToRotate.localRotation = Quaternion.Euler(0f, 0f, _params.AngleDegrees);
                else
                    _gunBaseTransform.localRotation = Quaternion.Euler(0f, 0f, _params.AngleDegrees);
            }

            if (_trajectoryPreview == null || _muzzleTransform == null) return;
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsSetup) return;

            _trajectoryPreview.UpdatePreview(
                _muzzleTransform.position,
                _params.AngleDegrees,
                _params.InitialVelocity,
                _params.Gravity);
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
            // Cancelar el lote si está en ejecución
            if (_batchMode)
            {
                if (_batchCoroutine != null)
                {
                    StopCoroutine(_batchCoroutine);
                    _batchCoroutine = null;
                }
                _batchMode = false;
                Time.timeScale = _params.TimeScale; // Restaurar el tiempo normal forzosamente
            }

            if (_bulletController != null)
                _bulletController.gameObject.SetActive(false);

            _pipCamera?.Deactivate();
            _targetSpawner?.ResetTargets();
            
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(GameStateManager.SimState.Setup);

            _trajectoryPreview?.Show();
            RefreshPreview();
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

        public event Action<int> OnBoxHitCountChanged;

        /// <summary>Notifica que una caja fue golpeada (llamado por TargetBox.ReceiveHit o cadena de impactos).</summary>
        public void RegisterBoxHit()
        {
            _boxesHitThisShot++;
            OnBoxHitCountChanged?.Invoke(_boxesHitThisShot);
        }

        /// <summary>Exporta la sesión actual a CSV y abre la carpeta.</summary>
        public string ExportSession()
        {
            string path = CSVExporter.Export(_session);
            if (path != null) CSVExporter.OpenExportFolder();
            return path;
        }

        // ── Setters del Model (la View llama estos, nunca escribe _params directo) ─

        /// <summary>Establece el ángulo de disparo y refresca el preview.</summary>
        public void SetAngle(float degrees)
        {
            _params.AngleDegrees = degrees;
            RefreshPreview();
        }

        /// <summary>Establece la velocidad inicial y refresca el preview.</summary>
        public void SetVelocity(float metersPerSecond)
        {
            _params.InitialVelocity = metersPerSecond;
            RefreshPreview();
        }

        /// <summary>Establece la masa de la bala en gramos.</summary>
        public void SetMass(float grams) => _params.BulletMassG = grams;

        /// <summary>Establece el radio de la bala en milímetros.</summary>
        public void SetRadius(float mm) => _params.BulletRadiusMm = mm;

        /// <summary>Establece la gravedad y refresca el preview.</summary>
        public void SetGravity(float gravity)
        {
            _params.Gravity = gravity;
            RefreshPreview();
        }

        /// <summary>Establece el multiplicador de velocidad de simulación.</summary>
        public void SetTimeScale(float scale) 
        {
            _params.TimeScale = scale;
            if (!_batchMode) Time.timeScale = scale;
        }

        /// <summary>
        /// Aplica un preset de munición completo al Model.
        /// Actualiza velocidad, masa, radio y nombre de preset de una sola vez.
        /// </summary>
        public void ApplyPreset(BulletPreset preset)
        {
            if (preset == null) return;
            _params.InitialVelocity = preset.MuzzleVelocity;
            _params.BulletMassG     = preset.MassGrams;
            _params.BulletRadiusMm  = preset.RadiusMm;
            _params.PresetName      = preset.PresetName;
            RefreshPreview();
        }

        /// <summary>Marca el preset como personalizado (cuando el usuario modifica manualmente).</summary>
        public void SetPresetName(string name) => _params.PresetName = name;

        // ── Batch Testing ─────────────────────────────────────────────────────────

        private Coroutine _batchCoroutine;

        public void StartBatchTest(
            float angleMin, float angleMax, float angleStep,
            float velMin,   float velMax,   float velStep,
            Action<int, int> onProgress = null)
        {
            if (_batchCoroutine != null) StopCoroutine(_batchCoroutine);
            _batchCoroutine = StartCoroutine(RunBatch(angleMin, angleMax, angleStep, velMin, velMax, velStep, onProgress));
        }

        /// <summary>
        /// Lanza un batch test iterando sobre rangos de ángulo y velocidad.
        /// Llamado desde StartBatchTest via coroutine.
        /// </summary>
        private IEnumerator RunBatch(
            float angleMin, float angleMax, float angleStep,
            float velMin,   float velMax,   float velStep,
            Action<int, int> onProgress = null)
        {
            _batchMode = true;
            GameStateManager.Instance.SetState(GameStateManager.SimState.BatchRunning);

            // Acelerar el tiempo masivamente para que el batch termine rápido
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 20f;

            int total = 0;
            for (float a = angleMin; a <= angleMax + 0.001f; a += Mathf.Max(0.1f, angleStep))
                for (float v = velMin; v <= velMax + 0.001f; v += Mathf.Max(1f, velStep))
                    total++;

            int done = 0;

            for (float angle = angleMin; angle <= angleMax + 0.001f; angle += Mathf.Max(0.1f, angleStep))
            {
                for (float vel = velMin; vel <= velMax + 0.001f; vel += Mathf.Max(1f, velStep))
                {
                    _params.AngleDegrees    = angle;
                    _params.InitialVelocity = vel;

                    _batchShotComplete = false;
                    _targetSpawner?.ResetTargets();
                    LaunchInternal();

                    yield return new WaitUntil(() => _batchShotComplete);

                    done++;
                    onProgress?.Invoke(done, total);

                    yield return null;
                }
            }

            _batchMode = false;
            Time.timeScale = originalTimeScale; // Restaurar el tiempo normal

            try 
            {
                ExportSession();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Sim] Error al exportar lote: {ex.Message}");
            }
            
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(GameStateManager.SimState.Setup);
                
            _trajectoryPreview?.Show();
            RefreshPreview();
        }

        // ── Callbacks de la bala ──────────────────────────────────────────────────
        private void HandleLanded(Vector3 pos, float time)
        {
            FinalizeShot(pos, time, hit: false, 0f, 0f);
            _batchShotComplete = true;

            if (_batchMode)
            {
                if (_bulletController != null)
                    _bulletController.gameObject.SetActive(false);
            }
            else
            {
                // La bala queda en el suelo y la cámara PiP sigue activa
                // hasta que el usuario resetea.
            }
        }

        private void HandleHit(Vector3 pos, float time, GameObject target, float relVel, float impulse)
        {
            var box = target.GetComponent<TargetBox>();
            if (box != null)
            {
                float bulletMassKg  = _params.BulletMassG / 1000f;
                float speed         = BulletPhysics.Speed(_params.AngleDegrees, _params.InitialVelocity, _params.Gravity, time);
                float kineticEnergy = 0.5f * bulletMassKg * speed * speed;
                Vector3 dir         = BulletPhysics.Velocity(_params.AngleDegrees, _params.InitialVelocity, _params.Gravity, time).normalized;

                box.ReceiveHit(pos, dir, kineticEnergy * 0.02f);
            }

            StartCoroutine(WaitAndFinalizeHit(pos, time, relVel, impulse));
        }

        private IEnumerator WaitAndFinalizeHit(Vector3 pos, float time, float relVel, float impulse)
        {
            // Esperar el tiempo configurado para dejar que las físicas actúen y la estructura caiga
            yield return new WaitForSeconds(_params.PostImpactWaitSeconds);

            FinalizeShot(pos, time, hit: true, relVel, impulse);
            _batchShotComplete = true;

            if (_batchMode)
            {
                if (_bulletController != null)
                    _bulletController.gameObject.SetActive(false);
            }
            else
            {
                // En modo manual, no desactivamos la cámara PiP ni la bala, 
                // ni volvemos al estado Setup automáticamente.
                // Permitimos que el motor físico siga mostrando las cajas volando
                // y la bala rebotando hasta que el usuario decida reiniciar.
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Prepara el simulador para el siguiente disparo sin resetear la escena.
        /// Los targets NO se limpian — el usuario debe pulsar REINICIAR para eso.
        /// </summary>
        private void ReadyForNextShot()
        {
            if (_bulletController != null)
                _bulletController.gameObject.SetActive(false);

            _trajectoryPreview?.Show();
            RefreshPreview();

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(GameStateManager.SimState.Setup);
        }

        private void LaunchInternal()
        {
            if (_bulletController == null || _muzzleTransform == null) return;

            _boxesHitThisShot = 0;
            _currentShot      = BuildShotData();

            _bulletController.Origin          = _muzzleTransform.position;
            _bulletController.AngleDegrees    = _params.AngleDegrees;
            _bulletController.InitialVelocity = _params.InitialVelocity;
            _bulletController.BulletRadiusMm  = _params.BulletRadiusMm;
            _bulletController.BulletMassG     = _params.BulletMassG; // <-- IMPORTANTE: Pasar la masa
            _bulletController.Gravity         = _params.Gravity;
            
            // Mover la bala ANTES de encenderla garantiza que el TrailRenderer no dibuje desde (0,0,0)
            _bulletController.transform.position = _muzzleTransform.position;
            _bulletController.gameObject.SetActive(true);
            
            _bulletController.Launch(0f);

            _pipCamera?.Activate(_bulletController.transform);

            _trajectoryPreview?.Hide();
            if (GameStateManager.Instance != null && !_batchMode)
                GameStateManager.Instance.SetState(GameStateManager.SimState.Firing);
        }

        private void FinalizeShot(Vector3 impactPos, float time, bool hit, float relVel, float impulse)
        {
            if (_currentShot == null) return;

            _currentShot.ImpactHit         = hit;
            _currentShot.ImpactX           = impactPos.x;
            _currentShot.ImpactY           = impactPos.y - (_muzzleTransform != null ? _muzzleTransform.position.y : 0f); // Relativo al cañón (Y=0)
            _currentShot.ImpactZ           = impactPos.z;
            _currentShot.FlightTimeSeconds = time;
            _currentShot.MaxHeightM        = _bulletController?.MaxHeightReached ?? 0f; // MaxHeight ya era relativo al Origin
            _currentShot.BoxesHit          = _boxesHitThisShot;
            _currentShot.RelativeVelocity  = relVel;
            _currentShot.CollisionImpulse  = impulse;
            _currentShot.Timestamp         = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (_muzzleTransform != null)
            {
                // Rango horizontal exacto (ignorando la diferencia de altura para la fórmula base de alcance)
                _currentShot.RangeM = Vector2.Distance(
                    new Vector2(_muzzleTransform.position.x, _muzzleTransform.position.z),
                    new Vector2(impactPos.x, impactPos.z));
            }

            _session.AddShot(_currentShot);
            DatabaseManager.Instance?.InsertShot(_currentShot);
            OnShotCompleted?.Invoke(_currentShot);

            Debug.Log($"[Sim] Disparo #{_currentShot.ShotId} | " +
                      $"Impacto={hit} | Rango={_currentShot.RangeM:F1}m | " +
                      $"AltMax={_currentShot.MaxHeightM:F1}m | T={time:F2}s | VelRel={relVel:F1}m/s | Impulso={impulse:F1}Ns");
        }

        private ShotData BuildShotData()
        {
            var cfg = _targetSpawner?.Config ?? new TargetGridConfig();
            return new ShotData
            {
                PresetName      = _params.PresetName,
                AngleDegrees    = _params.AngleDegrees,
                InitialVelocity = _params.InitialVelocity,
                BulletMassGrams = _params.BulletMassG,
                BulletRadiusMm  = _params.BulletRadiusMm,
                Gravity         = _params.Gravity,
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
