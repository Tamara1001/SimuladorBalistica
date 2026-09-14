using UnityEngine;
using BallisticSimulator.Physics;

namespace BallisticSimulator.Camera
{
    /// <summary>
    /// Cámara Picture-in-Picture que sigue a la bala desde atrás (vista tercera persona).
    /// Renderiza a una RenderTexture que luego se muestra en la UI mediante UI Toolkit.
    /// 
    /// Flujo: Camera → RenderTexture → Material (RenderMat) → UI Toolkit Image
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class BulletPiPCamera : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuración de seguimiento")]
        [Tooltip("Distancia detrás de la bala (en la dirección opuesta a su vuelo)")]
        [SerializeField] private float _followDistance = 4f;

        [Tooltip("Altura sobre la bala")]
        [SerializeField] private float _heightOffset = 1.2f;

        [Tooltip("Suavizado de movimiento (0 = inmediato, 1 = muy suave)")]
        [SerializeField, Range(0f, 1f)] private float _smoothing = 0.08f;

        [Header("RenderTexture")]
        [Tooltip("RenderTexture donde esta cámara dibuja. Asignar desde el Inspector o desde el Editor Tool.")]
        [SerializeField] private RenderTexture _renderTexture;

        // ── Estado interno ────────────────────────────────────────────────────────
        private Transform         _target;
        private UnityEngine.Camera _cam;
        private bool              _active;

        // ── Propiedad pública ─────────────────────────────────────────────────────
        /// <summary>La RenderTexture a la que renderiza esta cámara.</summary>
        public RenderTexture RenderTexture => _renderTexture;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _cam.enabled = false; // Empieza desactivada

            // Asignar RenderTexture si ya está referenciada
            if (_renderTexture != null)
                _cam.targetTexture = _renderTexture;
        }

        private Vector3 _lockedForward = Vector3.right;

        private void LateUpdate()
        {
            if (!_active || _target == null) return;

            // Obtener el script de la bala para saber si sigue volando o ya chocó
            var bullet = _target.GetComponent<BulletController>();
            bool isFlying = bullet != null && bullet.IsFlying;

            // Si está volando, actualizamos nuestra dirección de persecución
            if (isFlying)
            {
                Vector3 bulletForward = _target.forward;
                bulletForward.y = 0f; // Aplanar para evitar volteretas verticales de cámara
                
                if (bulletForward.sqrMagnitude > 0.01f)
                    _lockedForward = bulletForward.normalized;
            }

            // Posición deseada: detrás de la bala usando el ángulo bloqueado + altura
            Vector3 desiredPos = _target.position
                               - _lockedForward * _followDistance
                               + Vector3.up * _heightOffset;

            // Suavizar el movimiento
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - _smoothing);

            // Siempre mirar al centro de la bala (con un ligero offset)
            transform.LookAt(_target.position + _lockedForward * 1.5f);
        }

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>
        /// Activa la cámara PiP y comienza a seguir al target indicado.
        /// </summary>
        /// <param name="bullet">Transform de la bala a seguir.</param>
        public void Activate(Transform bullet)
        {
            _target = bullet;
            _active = true;

            // Posicionar inmediatamente detrás de la bala para evitar salto inicial
            if (_target != null)
            {
                Vector3 bulletForward = Vector3.right; // la bala viaja en +X
                transform.position = _target.position
                                   - bulletForward * _followDistance
                                   + Vector3.up * _heightOffset;
                transform.LookAt(_target.position);
            }

            _cam.enabled = true;
        }

        /// <summary>
        /// Desactiva la cámara PiP y para el renderizado.
        /// </summary>
        public void Deactivate()
        {
            _active = false;
            _target = null;
            _cam.enabled = false;
        }

        /// <summary>
        /// Asigna programáticamente la RenderTexture (útil desde el Editor Tool).
        /// </summary>
        public void SetRenderTexture(RenderTexture rt)
        {
            _renderTexture = rt;
            if (_cam != null) _cam.targetTexture = rt;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            if (_target != null)
            {
                Gizmos.DrawLine(transform.position, _target.position);
            }
        }
#endif
    }
}
