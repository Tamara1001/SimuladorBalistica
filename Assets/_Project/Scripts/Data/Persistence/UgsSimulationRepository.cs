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
        // ── Claves y prefijos de Cloud Save (alineados con material de cátedra) ───
        private const string HistoryPrefix = "simulation_";
        private const string KeyParams     = "sim_params_v1";
        private const string KeyHistory    = "shot_history_v1";

        // Límite de disparos a guardar en modo bloque para no superar 1 MB por clave
        private const int MaxShotsInCloud = 50;

        // ── Wrapper necesario para serializar List<ShotData> con JsonUtility ──────
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

                // Guardamos tanto las 4 claves directas de Persistencia I (angle, force, mass, gravity)
                // como el JSON estructurado completo de la simulación.
                var data = new Dictionary<string, object>
                {
                    { "angle", parameters.AngleDegrees },
                    { "force", parameters.InitialVelocity },
                    { "mass", parameters.BulletMassG },
                    { "gravity", parameters.Gravity },
                    { KeyParams, json }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"[UGS Repo] Parámetros guardados en UGS. JSON: {json}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al guardar parámetros.");
                Debug.LogException(ex);
                throw;
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
                var keys = new HashSet<string> { KeyParams, "angle", "force", "mass", "gravity" };
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (result.ContainsKey(KeyParams))
                {
                    string json = result[KeyParams].Value.GetAs<string>();
                    BallisticParameters loaded = JsonUtility.FromJson<BallisticParameters>(json);
                    Debug.Log($"[UGS Repo] Parámetros cargados desde JSON: {json}");
                    return loaded;
                }

                if (result.ContainsKey("angle"))
                {
                    var loaded = new BallisticParameters
                    {
                        AngleDegrees    = result["angle"].Value.GetAs<float>(),
                        InitialVelocity = result["force"].Value.GetAs<float>(),
                        BulletMassG     = result["mass"].Value.GetAs<float>(),
                        Gravity         = result["gravity"].Value.GetAs<float>()
                    };
                    Debug.Log("[UGS Repo] Parámetros cargados desde claves directas (angle, force, mass, gravity).");
                    return loaded;
                }

                Debug.Log("[UGS Repo] No hay parámetros guardados en la nube.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al cargar parámetros.");
                Debug.LogException(ex);
                throw;
            }
        }

        // ── AddToHistoryAsync (Persistencia II: 1 clave por registro con simulation_) ──

        public override async Task AddToHistoryAsync(ShotData shot)
        {
            if (!UgsInitializer.IsReady)
            {
                Debug.LogWarning("[UGS Repo] AddToHistoryAsync cancelado: UGS no está listo.");
                return;
            }

            try
            {
                var record = new SimulationRecord(shot);
                string json = JsonUtility.ToJson(record);
                string key = HistoryPrefix + record.id;

                var data = new Dictionary<string, object>
                {
                    { key, json }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"[UGS Repo] Disparo #{shot.ShotId} persistido en UGS con clave única: '{key}'");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al persistir disparo individual en UGS.");
                Debug.LogException(ex);
            }
        }

        // ── SaveShotHistoryAsync (Compatibilidad en bloque) ────────────────────────

        public override async Task SaveShotHistoryAsync(List<ShotData> shots)
        {
            if (!UgsInitializer.IsReady)
            {
                Debug.LogWarning("[UGS Repo] SaveShotHistoryAsync cancelado: UGS no está listo.");
                return;
            }

            try
            {
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
                Debug.Log($"[UGS Repo] Historial en bloque guardado: {toSave.Count} disparos.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al guardar historial en bloque.");
                Debug.LogException(ex);
                throw;
            }
        }

        // ── LoadShotHistoryAsync (Recupera todas las claves con prefijo simulation_) ──

        public override async Task<List<ShotData>> LoadShotHistoryAsync()
        {
            if (!UgsInitializer.IsReady)
            {
                Debug.LogWarning("[UGS Repo] LoadShotHistoryAsync cancelado: UGS no está listo.");
                return new List<ShotData>();
            }

            try
            {
                var allData = await CloudSaveService.Instance.Data.Player.LoadAllAsync();
                List<ShotData> history = new List<ShotData>();

                // 1. Priorizar registros individuales con prefijo simulation_ (Persistencia II)
                foreach (var item in allData)
                {
                    if (!item.Key.StartsWith(HistoryPrefix)) continue;

                    string json = item.Value.Value.GetAs<string>();
                    
                    // Deserializar usando SimulationRecord
                    var record = JsonUtility.FromJson<SimulationRecord>(json);
                    if (record != null)
                    {
                        history.Add(record.ToShotData());
                    }
                    else
                    {
                        // Fallback por si fue serializado como ShotData directamente
                        var directShot = JsonUtility.FromJson<ShotData>(json);
                        if (directShot != null) history.Add(directShot);
                    }
                }

                if (history.Count > 0)
                {
                    // Ordenar cronológicamente
                    history.Sort((a, b) => string.Compare(a.Timestamp, b.Timestamp, StringComparison.Ordinal));
                    
                    // Asignar IDs correlativos para visualización limpia
                    for (int i = 0; i < history.Count; i++)
                    {
                        history[i].ShotId = i + 1;
                    }

                    Debug.Log($"[UGS Repo] Historial recuperado desde UGS: {history.Count} registros (claves individuales '{HistoryPrefix}*').");
                    return history;
                }

                // 2. Fallback: Si no hay claves simulation_, buscar si existe el bloque shot_history_v1
                if (allData.ContainsKey(KeyHistory))
                {
                    string json = allData[KeyHistory].Value.GetAs<string>();
                    var wrapper = JsonUtility.FromJson<ShotHistoryWrapper>(json);
                    if (wrapper?.shots != null && wrapper.shots.Count > 0)
                    {
                        Debug.Log($"[UGS Repo] Historial recuperado desde bloque '{KeyHistory}': {wrapper.shots.Count} registros.");
                        return wrapper.shots;
                    }
                }

                Debug.Log("[UGS Repo] No se encontraron registros de historial en UGS.");
                return history;
            }
            catch (Exception ex)
            {
                Debug.LogError("[UGS Repo] Error al cargar historial desde UGS.");
                Debug.LogException(ex);
                throw;
            }
        }
    }
}
