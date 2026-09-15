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
        [Range(0, 10)] public int   Rows     = 5;
        [Range(0, 10)] public int   Columns  = 5;
        [Range(1, 5)]  public int   Depth    = 2;  // capas en profundidad (eje X)
        public float BoxSize     = 0.4f;  // metros (lado del cubo, 40cm)
        public float BoxMass     = 1.5f;  // kg (más livianas)
        public float Distance    = 35f;   // metros desde el origen (arma)
        
        // Sin juntas fijas, las cajas pueden spawnearse rozándose (0f) sin explotar,
        // ya que PhysX maneja pilas simples muchísimo mejor que mallas de joints hiper-conectadas.
        public float Spacing     = 0.0f; 

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
        // El spawneo inicial ahora es manejado 100% por la UI (SidePanelUI) al arrancar, 
        // evitando el doble spawneo que causaba la explosión física.
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

        /// <summary>Resetea todas las cajas a su posición original regenerando la grilla y sus joints.</summary>
        public void ResetTargets()
        {
            DestroyGrid();
            SpawnGrid();
        }

        /// <summary>Libera todas las cajas del estado congelado.</summary>
        public void UnfreezeAll()
        {
            foreach (var box in _activeBoxes)
            {
                if (box != null) box.Unfreeze();
            }
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

            TargetBox[,,] gridArray = new TargetBox[_config.Rows, _config.Columns, _config.Depth];

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

                        gridArray[row, col, d] = box;
                        _activeBoxes.Add(box);
                    }
                }
            }

            for (int row = 0; row < _config.Rows; row++)
            {
                for (int col = 0; col < _config.Columns; col++)
                {
                    for (int d = 0; d < _config.Depth; d++)
                    {
                        var rb = gridArray[row, col, d].GetComponent<Rigidbody>();
                        // Reducir fricción/drag de las cajas para que vuelen más lejos al ser golpeadas
                        rb.linearDamping = 0.05f; 
                        rb.angularDamping = 0.05f;
                        
                        // Optimización: Limitar la velocidad de despenetración
                        // Evita que cajas ligeramente solapadas se empujen violentamente, ahorrando cálculos pesados.
                        rb.maxDepenetrationVelocity = 2.0f;
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
