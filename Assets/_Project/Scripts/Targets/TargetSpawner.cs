using System.Collections.Generic;
using UnityEngine;

namespace BallisticSimulator.Targets
{
    /// <summary>
    /// Configuración de la grilla de targets.
    /// Serializable para pasar entre UI y spawner.
    /// </summary>
    [System.Serializable]
    public class TargetGridConfig
    {
        [Range(0, 5)] public int   Rows     = 2;
        [Range(0, 5)] public int   Columns  = 3;
        [Range(1, 5)] public int   Depth    = 2;  // capas en profundidad (eje X)
        public float BoxSize     = 1f;    // metros (lado del cubo)
        public float BoxMass     = 20f;   // kg
        public float Distance    = 35f;   // metros desde el origen (arma)
        public float Spacing     = 0.1f;  // espacio entre cajas (metros)

        /// <summary>Total de cajas = Rows × Columns × Depth (0 si cualquiera es 0).</summary>
        public int TotalBoxes => Rows * Columns * Depth;
    }

    /// <summary>
    /// Instancia, configura y resetea la grilla de cajas target.
    /// Soporta 0 cajas (modo sin targets para testeo de trayectoria pura).
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Config inicial")]
        [SerializeField] private TargetGridConfig _config = new TargetGridConfig();

        [Header("Prefab")]
        [SerializeField] private GameObject _boxPrefab;

        [Header("Referencia al arma (origen de disparo)")]
        [SerializeField] private Transform _muzzleTransform;

        // ── Estado ────────────────────────────────────────────────────────────────
        private readonly List<TargetBox> _activeBoxes = new List<TargetBox>();

        public TargetGridConfig Config => _config;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Start() => SpawnGrid();

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>
        /// Aplica una nueva configuración y regenera la grilla.
        /// Llamado desde la UI cuando cambian los sliders de targets.
        /// </summary>
        public void ApplyConfig(TargetGridConfig newConfig)
        {
            _config = newConfig;
            DestroyGrid();
            SpawnGrid();
        }

        /// <summary>Resetea todas las cajas a su posición original sin destruirlas.</summary>
        public void ResetTargets()
        {
            foreach (var box in _activeBoxes)
                if (box != null) box.ResetToOrigin();
        }

        // ── Spawn ─────────────────────────────────────────────────────────────────
        private void SpawnGrid()
        {
            if (_config.TotalBoxes == 0) return;
            if (_boxPrefab == null)
            {
                Debug.LogError("[TargetSpawner] No hay prefab de caja asignado.");
                return;
            }

            float cellSize = _config.BoxSize + _config.Spacing;

            // Calcular origen de la grilla centrado lateralmente
            float gridWidth = (_config.Columns - 1) * cellSize;

            // Posición base: frente al arma a la distancia configurada
            Vector3 muzzlePos = _muzzleTransform != null
                ? _muzzleTransform.position
                : Vector3.zero;

            Vector3 basePos = new Vector3(
                muzzlePos.x + _config.Distance,
                _config.BoxSize * 0.5f,   // apoyadas en el suelo
                muzzlePos.z - gridWidth * 0.5f
            );

            for (int row = 0; row < _config.Rows; row++)
            {
                for (int col = 0; col < _config.Columns; col++)
                {
                    for (int d = 0; d < _config.Depth; d++)
                    {
                        Vector3 pos = basePos + new Vector3(d * cellSize, row * cellSize, col * cellSize);

                        GameObject go  = Instantiate(_boxPrefab, pos, Quaternion.identity, transform);
                        go.name        = $"TargetBox_{row}_{col}_{d}";
                        go.tag         = "Target";

                        var box        = go.GetComponent<TargetBox>();
                        box.MassKg     = _config.BoxMass;
                        box.SizeM      = _config.BoxSize;
                        box.ApplyConfig();

                        _activeBoxes.Add(box);
                    }
                }
            }

            Debug.Log($"[TargetSpawner] Grilla {_config.Rows}×{_config.Columns} spawneada a {_config.Distance}m.");
        }

        private void DestroyGrid()
        {
            foreach (var box in _activeBoxes)
                if (box != null) Destroy(box.gameObject);

            _activeBoxes.Clear();
        }
    }
}
