using UnityEngine;

namespace BallisticSimulator.Targets
{
    /// <summary>
    /// Comportamiento de una caja target individual.
    /// Recibe impactos via <see cref="ReceiveHit"/> (llamado por SimulationManager)
    /// y puede resetearse a su posición original entre disparos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class TargetBox : MonoBehaviour
    {
        // ── Config (asignada por TargetSpawner) ────────────────────────────────────
        [HideInInspector] public float MassKg = 20f;
        [HideInInspector] public float SizeM  = 1f;

        // ── Estado inicial ────────────────────────────────────────────────────────
        private Vector3    _originPosition;
        private Quaternion _originRotation;
        private Rigidbody  _rb;
        private bool       _wasHit;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _originPosition = transform.position;
            _originRotation = transform.rotation;
            ApplyConfig();
        }

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>
        /// Aplica la configuración de masa y escala.
        /// Llamar después de cambiar MassKg o SizeM desde el spawner.
        /// </summary>
        public void ApplyConfig()
        {
            _rb.mass             = MassKg;
            _rb.interpolation    = RigidbodyInterpolation.Interpolate;
            transform.localScale = Vector3.one * SizeM;
            _originPosition      = transform.position;
        }

        /// <summary>
        /// Recibe el impacto de la bala. Llamado directamente por SimulationManager.
        /// </summary>
        /// <param name="hitPoint">Punto de impacto en world-space.</param>
        /// <param name="direction">Dirección de la bala en el momento del impacto.</param>
        /// <param name="impactForce">Magnitud de la fuerza de impacto (Newtons).</param>
        public void ReceiveHit(Vector3 hitPoint, Vector3 direction, float impactForce)
        {
            if (_wasHit) return;
            _wasHit = true;

            // Notificar al SimulationManager
            Core.SimulationManager.Instance?.RegisterBoxHit();

            // Aplicar impulso en la dirección de la bala
            _rb.AddForceAtPosition(
                direction.normalized * impactForce,
                hitPoint,
                ForceMode.Impulse);

            // Torque aleatorio para efecto ragdoll
            _rb.AddTorque(
                Random.insideUnitSphere * impactForce * 0.3f,
                ForceMode.Impulse);
        }

        /// <summary>Resetea la caja a su posición y rotación original, sin velocidad.</summary>
        public void ResetToOrigin()
        {
            transform.SetPositionAndRotation(_originPosition, _originRotation);
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _wasHit             = false;
            gameObject.SetActive(true);
        }
    }
}
