using UnityEngine;

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

        private void LateUpdate()
        {
            if (!_active || _target == null) return;

            // Calcular la dirección de vuelo de la bala en el plano XZ
            // La bala viaja en el eje +X con arco en Y, así que la cámara va detrás en -X
            Vector3 bulletForward = _target.forward;

            // Si el forward de la bala es casi vertical, usar X global como fallback
            if (Mathf.Abs(bulletForward.x) < 0.1f && Mathf.Abs(bulletForward.z) < 0.1f)
                bulletForward = Vector3.right;

            // Posición deseada: detrás de la bala + altura
            Vector3 desiredPos = _target.position
                               - bulletForward.normalized * _followDistance
                               + Vector3.up * _heightOffset;

            // Suavizar el movimiento
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - _smoothing);

            // Siempre mirar a la bala
            transform.LookAt(_target.position + bulletForward * 1.5f);
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
