using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace BallisticSimulator.Data.Persistence
{
    /// <summary>
    /// Inicializa Unity Gaming Services y autentica al jugador anónimamente.
    /// Debe ejecutarse en Awake() antes que cualquier otro sistema intente
    /// usar Cloud Save.
    ///
    /// Coloca este componente en el mismo GameObject que UgsSimulationRepository
    /// (por ejemplo, un objeto llamado [Services] en la jerarquía).
    /// </summary>
    public class UgsInitializer : MonoBehaviour
    {
        // ── Estado global de disponibilidad ───────────────────────────────────────

        /// <summary>
        /// True cuando UGS está inicializado y el jugador autenticado.
        /// Verificar antes de cualquier operación de Cloud Save.
        /// </summary>
        public static bool IsReady { get; private set; }

        /// <summary>
        /// ID del jugador autenticado. Útil para debug y para consultar
        /// datos en el Unity Dashboard.
        /// </summary>
        public static string PlayerId { get; private set; }

        // ── Evento de disponibilidad ──────────────────────────────────────────────

        /// <summary>
        /// Se dispara cuando UGS termina de inicializar (exitoso o fallido).
        /// Parámetro bool: true = éxito, false = error.
        /// </summary>
        public static event Action<bool> OnReady;

        // ── Unity ────────────────────────────────────────────────────────────────

        // async void es aceptable aquí porque Awake es un callback de Unity,
        // no un método que otro sistema pueda await. Para operaciones reutilizables
        // siempre se debe devolver Task.
        private async void Awake()
        {
            IsReady   = false;
            PlayerId  = string.Empty;

            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                IsReady  = true;
                PlayerId = AuthenticationService.Instance.PlayerId;

                Debug.Log($"[UGS] Inicializado. Player ID: {PlayerId}");
                OnReady?.Invoke(true);
            }
            catch (Exception ex)
            {
                IsReady = false;
                Debug.LogError("[UGS] Error al inicializar Unity Gaming Services.");
                Debug.LogException(ex);
                OnReady?.Invoke(false);
            }
        }
    }
}
