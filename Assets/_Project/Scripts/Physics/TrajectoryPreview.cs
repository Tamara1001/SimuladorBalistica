using UnityEngine;

namespace BallisticSimulator.Physics
{
    /// <summary>
    /// Dibuja en tiempo real la trayectoria proyectada de la bala usando un LineRenderer.
    /// Se actualiza automáticamente cada vez que se llama a <see cref="UpdatePreview"/>.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Apariencia")]
        [Tooltip("Número de segmentos de la curva (más segmentos = curva más suave).")]
        [Range(30, 300)]
        [SerializeField] private int _steps = 120;

        [Tooltip("Ancho de la línea de trayectoria.")]
        [SerializeField] private float _lineWidth = 0.05f;

        [Tooltip("Color de la línea de trayectoria.")]
        [SerializeField] private Color _lineColor = new Color(0.2f, 1f, 0.4f, 0.85f);

        [Tooltip("Material para la línea punteada (asignar desde el inspector).")]
        [SerializeField] private Material _dottedMaterial;

        [Header("Mundo")]
        [Tooltip("Altura Y del suelo (world-space).")]
        [SerializeField] private float _groundY = 0f;

        // ── Componentes ───────────────────────────────────────────────────────────
        private LineRenderer _lr;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        private void ConfigureLineRenderer()
        {
            _lr.useWorldSpace    = true;
            _lr.startWidth       = _lineWidth;
            _lr.endWidth         = _lineWidth * 0.3f; // se afina al final
            _lr.startColor       = _lineColor;
            _lr.endColor         = new Color(_lineColor.r, _lineColor.g, _lineColor.b, 0.1f);
            _lr.numCornerVertices = 4;
            _lr.numCapVertices    = 4;

            if (_dottedMaterial != null)
                _lr.material = _dottedMaterial;
        }

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>
        /// Recalcula y redibuja la trayectoria con los parámetros actuales.
        /// Llamar cada vez que el usuario mueve un slider.
        /// </summary>
        public void UpdatePreview(Vector3 origin, float angleDeg, float v0, float gravity)
        {
            Vector3[] points = BulletPhysics.ComputeTrajectoryPoints(
                origin, angleDeg, v0, gravity, _groundY, _steps);

            _lr.positionCount = points.Length;
            _lr.SetPositions(points);
            _lr.enabled = true;
        }

        /// <summary>Oculta la línea de trayectoria (ej: durante el vuelo real).</summary>
        public void Hide() => _lr.enabled = false;

        /// <summary>Muestra la línea de trayectoria.</summary>
        public void Show() => _lr.enabled = true;

        // ── Propiedades ───────────────────────────────────────────────────────────
        public float GroundY
        {
            get => _groundY;
            set => _groundY = value;
        }
    }
}
