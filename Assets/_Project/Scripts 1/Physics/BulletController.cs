using System;
using BallisticSimulator.Physics;
using UnityEngine;

namespace BallisticSimulator.Physics
{
    /// <summary>
    /// MonoBehaviour que anima la bala en la escena.
    /// Usa Physics.OverlapSphere para detectar colisiones con targets (más confiable
    /// que OnCollisionEnter para objetos que se mueven por código).
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        // ── Parámetros de vuelo (asignados por SimulationManager) ─────────────────
        [HideInInspector] public Vector3 Origin;
        [HideInInspector] public float   AngleDegrees;
        [HideInInspector] public float   InitialVelocity;
        [HideInInspector] public float   BulletRadiusMm;
        [HideInInspector] public float   Gravity;

        // ── Estado interno ────────────────────────────────────────────────────────
        private float _elapsedTime;
        private bool  _flying;
        private bool  _paused;
        private float _groundY;

        // ── Eventos ───────────────────────────────────────────────────────────────
        /// <summary>La bala aterrizó en el suelo sin golpear ningún target.</summary>
        public event Action<Vector3, float> OnLanded;

        /// <summary>La bala golpeó un objeto con tag "Target".</summary>
        public event Action<Vector3, float, GameObject> OnHit;

        // ── Propiedades de lectura ────────────────────────────────────────────────
        public float ElapsedTime      => _elapsedTime;
        public float MaxHeightReached { get; private set; }
        public bool  IsFlying         => _flying;

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>Inicia el vuelo de la bala desde <see cref="Origin"/>.</summary>
        public void Launch(float groundY = 0f)
        {
            _groundY         = groundY;
            _elapsedTime     = 0f;
            _flying          = true;
            _paused          = false;
            MaxHeightReached = 0f;

            transform.position   = Origin;
            // Escala visual aumentada para que la bala sea claramente visible en 3D
            float visualDiameter = Mathf.Max(0.5f, (BulletRadiusMm / 1000f) * 40f);
            transform.localScale = Vector3.one * visualDiameter;
        }

        /// <summary>Pausa o reanuda el vuelo.</summary>
        public void SetPaused(bool paused) => _paused = paused;

        /// <summary>Avanza exactamente un frame de física (útil en modo pausa).</summary>
        public void StepOneFrame() => AdvanceTime(Time.fixedDeltaTime);

        // ── Update ────────────────────────────────────────────────────────────────
        private void FixedUpdate()
        {
            if (!_flying || _paused) return;
            AdvanceTime(Time.fixedDeltaTime);
        }

        private void AdvanceTime(float dt)
        {
            Vector3 prevPos = transform.position;
            _elapsedTime += dt;

            Vector3 newPos = BulletPhysics.Position(
                Origin, AngleDegrees, InitialVelocity, Gravity, _elapsedTime);

            // Rastrear altura máxima sobre el origen
            float height = newPos.y - Origin.y;
            if (height > MaxHeightReached) MaxHeightReached = height;

            // Orientar en la dirección de la velocidad
            Vector3 vel = BulletPhysics.Velocity(
                AngleDegrees, InitialVelocity, Gravity, _elapsedTime);
            if (vel.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(vel.normalized);

            // ── Continuous Collision Detection via SphereCastAll (Evita atravesar cajas a alta velocidad) ──
            float visualDiameter = Mathf.Max(0.5f, (BulletRadiusMm / 1000f) * 40f);
            float hitRadius      = visualDiameter * 0.5f;

            Vector3 movement = newPos - prevPos;
            float moveDist   = movement.magnitude;

            if (moveDist > 0.001f)
            {
                RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(
                    prevPos, hitRadius, movement.normalized, moveDist);

                foreach (var hit in hits)
                {
                    if (!hit.collider.CompareTag("Target")) continue;

                    transform.position = hit.point;
                    _flying = false;
                    OnHit?.Invoke(hit.point, _elapsedTime, hit.collider.gameObject);
                    gameObject.SetActive(false);
                    return;
                }
            }

            transform.position = newPos;

            // ── Detectar suelo ───────────────────────────────────────────────────
            if (newPos.y <= _groundY)
            {
                Vector3 landPos = new Vector3(newPos.x, _groundY, newPos.z);
                transform.position = landPos;
                _flying = false;
                OnLanded?.Invoke(landPos, _elapsedTime);
                gameObject.SetActive(false);
            }
        }
    }
}
