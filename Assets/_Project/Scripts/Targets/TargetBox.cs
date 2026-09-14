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

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            GetComponent<BoxCollider>().isTrigger = false;
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
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Ayuda a que la bala no las atraviese
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

            // Romper todas las uniones de esta caja y las que apunten a ella
            BreakAllJoints();

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

        private float _spawnTime;

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 1. Ignorar roturas durante la fase de Setup (antes de disparar)
            if (Core.GameStateManager.Instance != null && Core.GameStateManager.Instance.IsSetup) return;

            // 2. Proteccion adicional: Ignorar los primeros 0.5s tras el spawn/reset para dar tiempo a asentar las físicas
            if (Time.time < _spawnTime + 0.5f) return;

            // Si es la bala o un choque de alta velocidad (> 2 m/s), desvincular uniones
            if (collision.gameObject.CompareTag("Bullet") || collision.relativeVelocity.sqrMagnitude > 4.0f)
            {
                BreakAllJoints();
                if (!_wasHit)
                {
                    _wasHit = true;
                    Core.SimulationManager.Instance?.RegisterBoxHit();
                }
            }
        }

        /// <summary>
        /// Destruye de forma bidireccional todas las articulaciones asociadas a esta caja.
        /// </summary>
        public void BreakAllJoints()
        {
            // 1. Destruir joints montados en este GameObject
            foreach (var joint in GetComponents<Joint>())
            {
                Destroy(joint);
            }

            // 2. Destruir joints en otras cajas que estén conectadas a esta
            if (_rb != null)
            {
                var allJoints = FindObjectsByType<Joint>(FindObjectsSortMode.None);
                foreach (var j in allJoints)
                {
                    if (j.connectedBody == _rb)
                    {
                        Destroy(j);
                    }
                }
            }
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
