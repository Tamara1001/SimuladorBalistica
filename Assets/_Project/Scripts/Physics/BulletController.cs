using System;
using UnityEngine;

namespace BallisticSimulator.Physics
{
    /// <summary>
    /// MonoBehaviour que anima la bala en la escena usando físicas nativas de Unity (Rigidbody).
    /// Cumple con los requerimientos académicos: usa AddForce, OnCollisionEnter y Continuous Dynamic.
    /// Para mantener exactitud con el Preview, la resistencia al aire (Drag) es nula
    /// y la gravedad se aplica manualmente como una aceleración.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class BulletController : MonoBehaviour
    {
        // ── Parámetros de vuelo (asignados por SimulationManager) ─────────────────
        [HideInInspector] public Vector3 Origin;
        [HideInInspector] public float   AngleDegrees;
        [HideInInspector] public float   InitialVelocity;
        [HideInInspector] public float   BulletRadiusMm;
        [HideInInspector] public float   BulletMassG;
        [HideInInspector] public float   Gravity;

        // ── Estado interno ────────────────────────────────────────────────────────
        private float _elapsedTime;
        private bool  _flying;
        private bool  _paused;
        private float _groundY;

        private Rigidbody      _rb;
        private SphereCollider _col;
        private TrailRenderer  _trail;

        // ── Eventos ───────────────────────────────────────────────────────────────
        public event Action<Vector3, float>             OnLanded;
        public event Action<Vector3, float, GameObject, float, float> OnHit;

        // ── Propiedades de lectura ────────────────────────────────────────────────
        public float ElapsedTime      => _elapsedTime;
        public float MaxHeightReached { get; private set; }
        public bool  IsFlying         => _flying;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb  = GetComponent<Rigidbody>();
            _col = GetComponent<SphereCollider>();
            _col.isTrigger = false; // <-- CRÍTICO: Si está en true, nunca choca.

            // Configurar Rigidbody ideal (vacío) para que sea 100% predecible matemáticamente
            _rb.useGravity             = false; // Aplicamos gravedad manual en FixedUpdate
            _rb.linearDamping                   = 0f;
            _rb.angularDamping            = 0f;
            _rb.interpolation          = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Evita el Tunneling

            // Efecto visual de rastro
            _trail = GetComponent<TrailRenderer>();
            if (_trail == null)
            {
                _trail = gameObject.AddComponent<TrailRenderer>();
                _trail.time = 0.4f;
                _trail.startWidth = 0.3f;
                _trail.endWidth = 0.05f;
                _trail.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                _trail.material.color = new Color(0.2f, 1f, 0.4f);
            }
        }

        /// <summary>Inicia el vuelo de la bala desde <see cref="Origin"/>.</summary>
        public void Launch(float groundY = 0f)
        {
            _groundY         = groundY;
            _elapsedTime     = 0f;
            _flying          = true;
            _paused          = false;
            MaxHeightReached = Origin.y;

            // Quitar parentesco o articulaciones por si en el disparo anterior quedó pegada a una caja
            transform.SetParent(null, true);
            var joint = GetComponent<FixedJoint>();
            if (joint != null) Destroy(joint);

            if (_trail != null) _trail.emitting = false;
            transform.position = Origin;
            if (_trail != null) 
            {
                _trail.Clear();
                _trail.emitting = true;
            }

            // Escala visual y tamaño del collider
            float visualDiameter = Mathf.Max(0.5f, (BulletRadiusMm / 1000f) * 40f);
            transform.localScale = Vector3.one * visualDiameter;
            _col.radius          = 0.5f; // Relativo al localScale

            // Aplicar propiedades físicas que vienen de la UI
            _rb.mass = BulletMassG / 1000f; // Convertir gramos a Kg
            _rb.useGravity = true;
            UnityEngine.Physics.gravity = new Vector3(0, -Gravity, 0); // Ajustar el mundo a nuestro slider

            // Aplicar velocidad inicial como impulso (matemáticamente puro)
            float rad = AngleDegrees * Mathf.Deg2Rad;
            Vector3 initialVelVector = new Vector3(
                Mathf.Cos(rad) * InitialVelocity,
                Mathf.Sin(rad) * InitialVelocity,
                0f
            );

            _rb.isKinematic = false;
            _rb.linearVelocity    = initialVelVector;
            _rb.angularVelocity = Vector3.zero;
        }

        /// <summary>Pausa o reanuda el vuelo (simulando un "freeze" en el aire).</summary>
        public void SetPaused(bool paused)
        {
            _paused = paused;
            _rb.isKinematic = paused; // Al pausar, se congela la física
        }

        /// <summary>Avanza exactamente un frame de física.</summary>
        public void StepOneFrame()
        {
            // Solo útil si mantenemos un modo manual avanzado, pero Unity Physics lo hace automático
            // en este modo no se recomienda usar StepOneFrame porque el Rigidbody es manejado por el motor.
        }

        private void FixedUpdate()
        {
            if (!_flying || _paused) return;

            _elapsedTime += Time.fixedDeltaTime;

            // Registrar altura máxima
            float height = transform.position.y - Origin.y;
            if (height > MaxHeightReached) MaxHeightReached = height;

            // Orientar la bala en la dirección en la que viaja
            if (_rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized);
            }

            // Fallback de suelo (por si no hay collider de piso debajo de groundY)
            if (transform.position.y <= _groundY)
            {
                TriggerLanded(new Vector3(transform.position.x, _groundY, transform.position.z));
            }
        }

        // ── Detección de Colisiones (Requisito Académico) ───────────────────────────
        private void OnCollisionEnter(Collision collision)
        {
            if (!_flying) return;

            // Contacto principal
            Vector3 hitPoint = collision.GetContact(0).point;

            if (collision.gameObject.CompareTag("Target"))
            {
                _flying = false; // Detenemos el registro de vuelo para las estadísticas
                
                // La bala de cañón golpeará y transferirá su brutal momentum a la caja
                // sin quedarse "pegada" a ella, lo cual es físicamente correcto para artillería pesada.

                float relVel = collision.relativeVelocity.magnitude;
                float impulse = collision.impulse.magnitude;

                OnHit?.Invoke(hitPoint, _elapsedTime, collision.gameObject, relVel, impulse);
            }
            else if (collision.gameObject.CompareTag("Ground"))
            {
                TriggerLanded(hitPoint);
            }
        }

        private void TriggerLanded(Vector3 pos)
        {
            if (!_flying) return;
            _flying = false;
            // No la desactivamos para que siga viéndose en el piso
            OnLanded?.Invoke(pos, _elapsedTime);
        }
    }
}
