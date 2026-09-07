using System.Collections;
using BallisticSimulator.Core;
using BallisticSimulator.Data;
using BallisticSimulator.Targets;
using UnityEngine;
using UnityEngine.UIElements;

namespace BallisticSimulator.UI
{
    /// <summary>
    /// Controlador del panel lateral principal.
    /// Conecta todos los elementos de UI Toolkit con SimulationManager y TargetSpawner.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SidePanelUI : MonoBehaviour
    {
        // ── Referencias ───────────────────────────────────────────────────────────
        [Header("Presets de munición (arrastrar desde Assets)")]
        [SerializeField] private BulletPreset[] _presets;

        [SerializeField] private TargetSpawner _targetSpawner;

        // ── Elementos UI ──────────────────────────────────────────────────────────
        // Bala
        private DropdownField _presetDropdown;
        private Slider        _angleSlider;
        private FloatField    _angleField;
        private Slider        _velocitySlider;
        private FloatField    _velocityField;
        private Slider        _massSlider;
        private FloatField    _massField;
        private Slider        _radiusSlider;
        private FloatField    _radiusField;
        private Slider        _gravitySlider;
        private FloatField    _gravityField;
        private Slider        _timeScaleSlider;
        private FloatField    _timeScaleField;

        // Targets
        private SliderInt    _rowsSlider;
        private IntegerField _rowsField;
        private SliderInt    _colsSlider;
        private IntegerField _colsField;
        private SliderInt    _depthSlider;
        private IntegerField _depthField;
        private Slider       _boxSizeSlider;
        private FloatField   _boxSizeField;
        private Slider       _boxMassSlider;
        private FloatField   _boxMassField;
        private Slider       _boxDistSlider;
        private FloatField   _boxDistField;

        // Botones
        private Button _fireButton;
        private Button _resetButton;
        private Button _pauseButton;
        private Button _stepButton;
        private Button _exportButton;

        // Labels de estado
        private Label _statusLabel;
        private Label _rangeLabel;
        private Label _maxHeightLabel;
        private Label _flightTimeLabel;

        // Batch
        private Foldout    _batchFoldout;
        private FloatField _batchAngleMin;
        private FloatField _batchAngleMax;
        private FloatField _batchAngleStep;
        private FloatField _batchVelMin;
        private FloatField _batchVelMax;
        private FloatField _batchVelStep;
        private Button     _batchRunButton;
        private Label      _batchStatusLabel;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            BindElements(root);
            InitPresetDropdown();
            RegisterCallbacks();

            // Suscribirse a eventos del sim
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnShotCompleted += OnShotCompleted;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged.AddListener(OnStateChanged);

            // Aplicar valores iniciales
            PushAllToSim();
        }

        private void OnDisable()
        {
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnShotCompleted -= OnShotCompleted;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
        }

        // ── Binding de elementos ──────────────────────────────────────────────────
        private void BindElements(VisualElement root)
        {
            // Bullet
            _presetDropdown  = root.Q<DropdownField>("preset-dropdown");
            _angleSlider     = root.Q<Slider>("angle-slider");
            _angleField      = root.Q<FloatField>("angle-field");
            _velocitySlider  = root.Q<Slider>("velocity-slider");
            _velocityField   = root.Q<FloatField>("velocity-field");
            _massSlider      = root.Q<Slider>("mass-slider");
            _massField       = root.Q<FloatField>("mass-field");
            _radiusSlider    = root.Q<Slider>("radius-slider");
            _radiusField     = root.Q<FloatField>("radius-field");
            _gravitySlider   = root.Q<Slider>("gravity-slider");
            _gravityField    = root.Q<FloatField>("gravity-field");
            _timeScaleSlider = root.Q<Slider>("timescale-slider");
            _timeScaleField  = root.Q<FloatField>("timescale-field");

            // Targets
            _rowsSlider      = root.Q<SliderInt>("rows-slider");
            _rowsField       = root.Q<IntegerField>("rows-field");
            _colsSlider      = root.Q<SliderInt>("cols-slider");
            _colsField       = root.Q<IntegerField>("cols-field");
            _depthSlider     = root.Q<SliderInt>("depth-slider");
            _depthField      = root.Q<IntegerField>("depth-field");
            _boxSizeSlider   = root.Q<Slider>("boxsize-slider");
            _boxSizeField    = root.Q<FloatField>("boxsize-field");
            _boxMassSlider   = root.Q<Slider>("boxmass-slider");
            _boxMassField    = root.Q<FloatField>("boxmass-field");
            _boxDistSlider   = root.Q<Slider>("boxdist-slider");
            _boxDistField    = root.Q<FloatField>("boxdist-field");

            // Botones
            _fireButton      = root.Q<Button>("btn-fire");
            _resetButton     = root.Q<Button>("btn-reset");
            _pauseButton     = root.Q<Button>("btn-pause");
            _stepButton      = root.Q<Button>("btn-step");
            _exportButton    = root.Q<Button>("btn-export");

            // Labels de resultado
            _statusLabel     = root.Q<Label>("lbl-status");
            _rangeLabel      = root.Q<Label>("lbl-range");
            _maxHeightLabel  = root.Q<Label>("lbl-maxheight");
            _flightTimeLabel = root.Q<Label>("lbl-flighttime");

            // Batch
            _batchFoldout     = root.Q<Foldout>("batch-foldout");
            _batchAngleMin    = root.Q<FloatField>("batch-angle-min");
            _batchAngleMax    = root.Q<FloatField>("batch-angle-max");
            _batchAngleStep   = root.Q<FloatField>("batch-angle-step");
            _batchVelMin      = root.Q<FloatField>("batch-vel-min");
            _batchVelMax      = root.Q<FloatField>("batch-vel-max");
            _batchVelStep     = root.Q<FloatField>("batch-vel-step");
            _batchRunButton   = root.Q<Button>("btn-batch-run");
            _batchStatusLabel = root.Q<Label>("lbl-batch-status");

            // Evitar que Sliders, Botones y Desplegables roben el foco del teclado (WASD)
            root.Query<VisualElement>()
                .Where(e => e is Slider || e is SliderInt || e is Button || e is Foldout || e is DropdownField)
                .ForEach(e => e.focusable = false);

            root.RegisterCallback<PointerDownEvent>(evt =>
            {
                // Al hacer clic derecho o fuera de campos numéricos, desenfocar UI para liberar el teclado a la cámara
                if (evt.button == 1)
                {
                    root.focusController?.focusedElement?.Blur();
                }
            });
        }

        // ── Callbacks ────────────────────────────────────────────────────────────
        private void RegisterCallbacks()
        {
            // ── Preset ──
            _presetDropdown?.RegisterValueChangedCallback(_ => OnPresetChanged());

            // ── Sliders de bala (sincronizar slider ↔ field y actualizar sim) ──
            LinkSliderToField(_angleSlider,     _angleField,     v => { SimulationManager.Instance.AngleDegrees    = v; SimulationManager.Instance.RefreshPreview(); });
            LinkSliderToField(_velocitySlider,  _velocityField,  v => { SimulationManager.Instance.InitialVelocity = v; SimulationManager.Instance.RefreshPreview(); });
            LinkSliderToField(_massSlider,      _massField,      v =>   SimulationManager.Instance.BulletMassG     = v);
            LinkSliderToField(_radiusSlider,    _radiusField,    v =>   SimulationManager.Instance.BulletRadiusMm  = v);
            LinkSliderToField(_gravitySlider,   _gravityField,   v => { SimulationManager.Instance.Gravity         = v; SimulationManager.Instance.RefreshPreview(); });
            LinkSliderToField(_timeScaleSlider, _timeScaleField, v =>   SimulationManager.Instance.TimeScale       = v);

            // ── Sliders de targets (regenerar grilla al soltar) ──
            LinkIntSliderToField(_rowsSlider,  _rowsField,  _ => ApplyTargetConfig());
            LinkIntSliderToField(_colsSlider,  _colsField,  _ => ApplyTargetConfig());
            LinkIntSliderToField(_depthSlider, _depthField, _ => ApplyTargetConfig());
            LinkSliderToField(_boxSizeSlider,  _boxSizeField, _ => ApplyTargetConfig());
            LinkSliderToField(_boxMassSlider,  _boxMassField, _ => ApplyTargetConfig());
            LinkSliderToField(_boxDistSlider,  _boxDistField, _ => ApplyTargetConfig());

            // ── Botones ──
            _fireButton?.RegisterCallback<ClickEvent>(_ => SimulationManager.Instance.Fire());
            _resetButton?.RegisterCallback<ClickEvent>(_ => SimulationManager.Instance.Reset());
            _pauseButton?.RegisterCallback<ClickEvent>(_ => SimulationManager.Instance.TogglePause());
            _stepButton?.RegisterCallback<ClickEvent>(_ => SimulationManager.Instance.StepFrame());
            _exportButton?.RegisterCallback<ClickEvent>(_ =>
            {
                string path = SimulationManager.Instance.ExportSession();
                if (path != null)
                {
                    if (_statusLabel != null) _statusLabel.text = $"✓ CSV exportado";
                    CSVExporter.OpenExportFolder();
                }
            });

            // ── Batch ──
            _batchRunButton?.RegisterCallback<ClickEvent>(_ => StartBatch());
        }

        // ── Preset ───────────────────────────────────────────────────────────────
        private void InitPresetDropdown()
        {
            if (_presetDropdown == null || _presets == null) return;

            _presetDropdown.choices.Clear();
            foreach (var p in _presets)
            {
                if (p != null) _presetDropdown.choices.Add(p.PresetName);
            }

            _presetDropdown.choices.Add("Personalizado");
            _presetDropdown.index = 1; // 9mm por defecto
            OnPresetChanged();
        }

        private void OnPresetChanged()
        {
            if (_presetDropdown == null) return;

            int idx = _presetDropdown.index;
            if (_presets != null && idx >= 0 && idx < _presets.Length && _presets[idx] != null)
            {
                var preset = _presets[idx];
                SetSliderAndField(_velocitySlider, _velocityField, preset.MuzzleVelocity);
                SetSliderAndField(_massSlider,     _massField,     preset.MassGrams);
                SetSliderAndField(_radiusSlider,   _radiusField,   preset.RadiusMm);
                if (SimulationManager.Instance != null)
                    SimulationManager.Instance.PresetName = preset.PresetName;
            }
            else
            {
                if (SimulationManager.Instance != null)
                    SimulationManager.Instance.PresetName = "Personalizado";
            }
        }

        // ── Targets ──────────────────────────────────────────────────────────────
        private void ApplyTargetConfig()
        {
            if (_targetSpawner == null) return;

            var cfg = new TargetGridConfig
            {
                Rows     = _rowsSlider?.value  ?? 2,
                Columns  = _colsSlider?.value  ?? 3,
                Depth    = _depthSlider?.value ?? 2,
                BoxSize  = _boxSizeSlider?.value ?? 1f,
                BoxMass  = _boxMassSlider?.value ?? 20f,
                Distance = _boxDistSlider?.value ?? 35f,
            };
            _targetSpawner.ApplyConfig(cfg);
        }

        // ── Estado ───────────────────────────────────────────────────────────────
        private void OnStateChanged(GameStateManager.SimState state)
        {
            bool setup  = state == GameStateManager.SimState.Setup;
            bool paused = state == GameStateManager.SimState.Paused;

            _fireButton?.SetEnabled(setup);
            _pauseButton?.SetEnabled(!setup);
            _stepButton?.SetEnabled(paused);

            if (_pauseButton != null)
                _pauseButton.text = paused ? "▶ REANUDAR" : "⏸ PAUSAR";

            if (_statusLabel != null)
                _statusLabel.text = state switch
                {
                    GameStateManager.SimState.Setup        => "EN ESPERA",
                    GameStateManager.SimState.Firing       => "EN VUELO...",
                    GameStateManager.SimState.Paused       => "PAUSADO",
                    GameStateManager.SimState.BatchRunning => "LOTE EN PROGRESO...",
                    _                                      => ""
                };
        }

        private void OnShotCompleted(ShotData shot)
        {
            if (_rangeLabel      != null) _rangeLabel.text      = $"Rango: {shot.RangeM:F1} m";
            if (_maxHeightLabel  != null) _maxHeightLabel.text  = $"Alt. máx: {shot.MaxHeightM:F1} m";
            if (_flightTimeLabel != null) _flightTimeLabel.text = $"T. vuelo: {shot.FlightTimeSeconds:F2} s";
            if (_statusLabel != null)
            {
                _statusLabel.text = shot.ImpactHit
                    ? $"💥 Impacto · {shot.BoxesHit} caja(s) golpeada(s)"
                    : "🛬 Aterrizó sin impacto";
            }
        }

        // ── Batch ─────────────────────────────────────────────────────────────────
        private void StartBatch()
        {
            if (SimulationManager.Instance == null) return;
            if (!GameStateManager.Instance.IsSetup) return;

            float angleMin  = _batchAngleMin?.value  ?? 10f;
            float angleMax  = _batchAngleMax?.value  ?? 80f;
            float angleStep = _batchAngleStep?.value ?? 10f;
            float velMin    = _batchVelMin?.value    ?? 100f;
            float velMax    = _batchVelMax?.value    ?? 100f;
            float velStep   = _batchVelStep?.value   ?? 1f;

            StartCoroutine(SimulationManager.Instance.RunBatch(
                angleMin, angleMax, angleStep,
                velMin, velMax, velStep,
                (done, total) =>
                {
                    if (_batchStatusLabel != null)
                        _batchStatusLabel.text = $"Batch: {done}/{total}";
                }));
        }

        // ── Helpers de binding ────────────────────────────────────────────────────
        private void PushAllToSim()
        {
            var sim = SimulationManager.Instance;
            if (sim == null) return;
            sim.AngleDegrees    = _angleSlider?.value ?? 10f;
            sim.InitialVelocity = _velocitySlider?.value ?? 45f;
            sim.BulletMassG     = _massSlider?.value ?? 8f;
            sim.BulletRadiusMm  = _radiusSlider?.value ?? 4.5f;
            sim.Gravity         = _gravitySlider?.value ?? 9.81f;
            sim.TimeScale       = _timeScaleSlider?.value ?? 1.0f;
            sim.RefreshPreview();
        }

        private static void LinkSliderToField(Slider slider, FloatField field, System.Action<float> onChanged)
        {
            if (slider == null || field == null) return;

            slider.RegisterValueChangedCallback(evt =>
            {
                field.SetValueWithoutNotify(evt.newValue);
                onChanged(evt.newValue);
            });
            field.RegisterValueChangedCallback(evt =>
            {
                slider.SetValueWithoutNotify(evt.newValue);
                onChanged(evt.newValue);
            });
        }

        private static void LinkIntSliderToField(SliderInt slider, IntegerField field, System.Action<int> onChanged)
        {
            if (slider == null || field == null) return;

            slider.RegisterValueChangedCallback(evt =>
            {
                field.SetValueWithoutNotify(evt.newValue);
                onChanged(evt.newValue);
            });
            field.RegisterValueChangedCallback(evt =>
            {
                slider.SetValueWithoutNotify(evt.newValue);
                onChanged(evt.newValue);
            });
        }

        private static void SetSliderAndField(Slider slider, FloatField field, float value)
        {
            slider?.SetValueWithoutNotify(value);
            field?.SetValueWithoutNotify(value);
        }
    }
}
