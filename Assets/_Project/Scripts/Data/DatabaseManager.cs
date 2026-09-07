using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BallisticSimulator.Data
{
    /// <summary>
    /// Almacena todos los disparos de la sesión en memoria.
    /// Sirve como capa de datos central: la UI y otros sistemas consultan aquí,
    /// y el CSVExporter exporta desde aquí al finalizar.
    ///
    /// NOTA: Para integrar SQLite real en el futuro, instalar el paquete
    /// "sqlite-net-pcl" via NuGet for Unity o Asset Store, y reimplementar
    /// esta clase manteniendo la misma interfaz pública.
    /// </summary>
    public class DatabaseManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static DatabaseManager Instance { get; private set; }

        // ── Almacenamiento en memoria ─────────────────────────────────────────────
        private readonly List<ShotData> _shots = new List<ShotData>();

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[DB] DatabaseManager iniciado. Modo: en memoria. " +
                      "Los datos se persisten mediante CSV Export.");
        }

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>Registra un disparo en la sesión activa.</summary>
        public void InsertShot(ShotData shot)
        {
            if (shot == null) return;
            _shots.Add(shot);
            Debug.Log($"[DB] Disparo #{shot.ShotId} registrado. " +
                      $"Total en sesión: {_shots.Count}");
        }

        /// <summary>Devuelve una copia de todos los disparos de la sesión.</summary>
        public List<ShotData> GetAllShots() => new List<ShotData>(_shots);

        /// <summary>Total de disparos registrados en esta sesión.</summary>
        public int TotalShots => _shots.Count;

        /// <summary>Último disparo registrado, o null si no hay ninguno.</summary>
        public ShotData LastShot => _shots.Count > 0 ? _shots[^1] : null;

        /// <summary>Borra todos los datos de la sesión actual.</summary>
        public void ClearSession() => _shots.Clear();

        /// <summary>
        /// Estadísticas rápidas de la sesión.
        /// Útil para mostrar en UI o exportar un resumen.
        /// </summary>
        public (int total, int hits, float avgRange, float avgMaxHeight) GetSessionStats()
        {
            if (_shots.Count == 0) return (0, 0, 0f, 0f);

            int    hits      = _shots.Count(s => s.ImpactHit);
            float  avgRange  = _shots.Average(s => s.RangeM);
            float  avgHeight = _shots.Average(s => s.MaxHeightM);

            return (_shots.Count, hits, avgRange, avgHeight);
        }
    }
}
