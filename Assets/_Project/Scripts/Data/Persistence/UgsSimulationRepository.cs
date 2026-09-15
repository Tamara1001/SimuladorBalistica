using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BallisticSimulator.Data;
using Unity.Services.CloudSave;
using UnityEngine;

namespace BallisticSimulator.Data.Persistence
{
    /// <summary>
    /// Implementación concreta de SimulationRepository usando UGS Cloud Save.
    ///
    /// Responsabilidades:
    ///   - Serializar BallisticParameters y List[ShotData] a JSON.
    ///   - Guardar y recuperar esos JSON mediante Cloud Save Player Data.
    ///   - Verificar que UGS esté listo antes de cada operación.
    ///
    /// Claves utilizadas en Cloud Save:
    ///   "sim_params_v1"    → BallisticParameters serializado como JSON
    ///   "shot_history_v1"  → Lista de ShotData serializada como JSON (últimos 50)
    ///
    /// El sufijo "_v1" permite migrar el formato en el futuro sin romper datos
    /// almacenados con versiones anteriores (ADR-05).
    /// </summary>
    public class UgsSimulationRepository : SimulationRepository
    {
        // ── Claves de Cloud Save (versionadas) ────────────────────────────────────
        private const string KeyParams  = "sim_params_v1";
        private const string KeyHistory = "shot_history_v1";

        // Límite de disparos a guardar en la nube para no superar 1 MB por clave
        private const int MaxShotsInCloud = 50;

        // ── Wrapper necesario para serializar List<ShotData> con JsonUtility ──────
        // JsonUtility no puede serializar listas directamente; necesita una clase contenedora.
        [Serializable]
        private class ShotHistoryWrapper
        {
            public List<ShotData> shots = new List<ShotData>();
        }

        // ── SaveParametersAsync ───────────────────────────────────────────────────

        public override async Task SaveParametersAsync(BallisticParameters parameters)
        {
            if (!UgsInitializer.IsReady)
            {
                Debug.LogWarning("[UGS Repo] SaveParametersAsync cancelado: UGS no está listo.");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(parameters);

                var data = new Dictionary<string, object>
                {
                    { KeyParams, json }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"[UGS Repo] Parámetros guardados. JSON: {json}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al guardar parámetros.");
                Debug.LogException(ex);
                throw; // Propaga la excepción para que el Controller pueda notificar a la UI
            }
        }

        // ── LoadParametersAsync ───────────────────────────────────────────────────

        public override async Task<BallisticParameters> LoadParametersAsync()
        {
            if (!UgsInitializer.IsReady)
            {
                Debug.LogWarning("[UGS Repo] LoadParametersAsync cancelado: UGS no está listo.");
                return null;
            }

            try
            {
                var keys = new HashSet<string> { KeyParams };
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (!result.ContainsKey(KeyParams))
                {
                    Debug.Log("[UGS Repo] No hay parámetros guardados en la nube.");
                    return null;
                }

                string json = result[KeyParams].Value.GetAs<string>();
                BallisticParameters loaded = JsonUtility.FromJson<BallisticParameters>(json);

                Debug.Log($"[UGS Repo] Parámetros cargados. JSON: {json}");
                return loaded;
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al cargar parámetros.");
                Debug.LogException(ex);
                throw;
            }
        }

        // ── SaveShotHistoryAsync ──────────────────────────────────────────────────

        public override async Task SaveShotHistoryAsync(List<ShotData> shots)
        {
            if (!UgsInitializer.IsReady)
            {
                Debug.LogWarning("[UGS Repo] SaveShotHistoryAsync cancelado: UGS no está listo.");
                return;
            }

            try
            {
                // Guardar solo los últimos N disparos para no superar el límite de 1 MB por clave
                var toSave = shots.Count > MaxShotsInCloud
                    ? shots.GetRange(shots.Count - MaxShotsInCloud, MaxShotsInCloud)
                    : shots;

                var wrapper = new ShotHistoryWrapper { shots = toSave };
                string json = JsonUtility.ToJson(wrapper);

                var data = new Dictionary<string, object>
                {
                    { KeyHistory, json }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"[UGS Repo] Historial guardado: {toSave.Count} disparos.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al guardar historial.");
                Debug.LogException(ex);
                throw;
            }
        }

        // ── LoadShotHistoryAsync ──────────────────────────────────────────────────

        public override async Task<List<ShotData>> LoadShotHistoryAsync()
        {
            if (!UgsInitializer.IsReady)
            {
                Debug.LogWarning("[UGS Repo] LoadShotHistoryAsync cancelado: UGS no está listo.");
                return new List<ShotData>();
            }

            try
            {
                var keys = new HashSet<string> { KeyHistory };
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (!result.ContainsKey(KeyHistory))
                {
                    Debug.Log("[UGS Repo] No hay historial guardado en la nube.");
                    return new List<ShotData>();
                }

                string json = result[KeyHistory].Value.GetAs<string>();
                var wrapper = JsonUtility.FromJson<ShotHistoryWrapper>(json);

                Debug.Log($"[UGS Repo] Historial cargado: {wrapper.shots.Count} disparos.");
                return wrapper.shots;
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al cargar historial.");
                Debug.LogException(ex);
                throw;
            }
        }
    }
}
