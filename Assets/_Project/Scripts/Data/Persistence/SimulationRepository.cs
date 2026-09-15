using System.Collections.Generic;
using System.Threading.Tasks;
using BallisticSimulator.Data;
using UnityEngine;

namespace BallisticSimulator.Data.Persistence
{
    /// <summary>
    /// Contrato abstracto para la capa de persistencia del simulador.
    ///
    /// El Controller (SimulationManager) habla únicamente con esta clase
    /// y nunca conoce el proveedor concreto (UGS, Firebase, local, etc.).
    ///
    /// ADR-04: Patrón Repository — aísla la infraestructura de persistencia
    /// del modelo de dominio, permitiendo reemplazar el proveedor sin tocar
    /// el resto del sistema.
    /// </summary>
    public abstract class SimulationRepository : MonoBehaviour
    {
        // ── Parámetros activos del simulador ──────────────────────────────────────

        /// <summary>
        /// Persiste el estado actual de los parámetros de la simulación.
        /// </summary>
        public abstract Task SaveParametersAsync(BallisticParameters parameters);

        /// <summary>
        /// Recupera los parámetros de la última simulación guardada.
        /// Devuelve null si no existen datos previos.
        /// </summary>
        public abstract Task<BallisticParameters> LoadParametersAsync();

        // ── Historial de disparos ─────────────────────────────────────────────────

        /// <summary>
        /// Persiste el historial de disparos de la sesión actual.
        /// </summary>
        public abstract Task SaveShotHistoryAsync(List<ShotData> shots);

        /// <summary>
        /// Recupera el historial de disparos de la última sesión guardada.
        /// Devuelve una lista vacía si no existen datos previos.
        /// </summary>
        public abstract Task<List<ShotData>> LoadShotHistoryAsync();
    }
}
