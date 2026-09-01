using UnityEngine;
using UnityEngine.InputSystem;

namespace BallisticSimulator.Camera
{
    /// <summary>
    /// Permite rotar, desplazar y hacer zoom con la SetupCamera durante la fase de configuración.
    /// Soporta tanto el Nuevo Input System como el Legacy Input System por fallback.
    ///
    /// Controles:
    /// - Mantener Clic Derecho + Mover Mouse: Orbitar la escena alrededor del punto focal
    /// - Rueda del Mouse: Acercar / Alejar (Zoom)
    /// - WASD / QE: Desplazar el punto focal del encuadre
    /// - Shift: Desplazamiento rápido (Turbo)
    /// </summary>
    public class SetupCameraController : MonoBehaviour
    {
        [Header("Centro de Foco")]
        [SerializeField] private Vector3 _targetPoint = new Vector3(20f, 2f, 0f);

        [Header("Velocidades")]
        [SerializeField] private float _orbitSensitivity = 0.3f;
        [SerializeField] private float _zoomSensitivity  = 5f;
        [SerializeField] private float _moveSpeed        = 25f;

        [Header("Límites de Zoom")]
        [SerializeField] private float _minDistance = 5f;
        [SerializeField] private float _maxDistance = 120f;

        private float _distance = 32f;
        private float _yaw      = 15f;
        private float _pitch    = 8f;
        private bool  _isRightClickPressed;

        private void OnEnable()
        {
            Vector3 dir = transform.position - _targetPoint;
            _distance   = Mathf.Clamp(dir.magnitude, _minDistance, _maxDistance);
            _yaw        = transform.eulerAngles.y;
            _pitch      = transform.eulerAngles.x;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // ── Leer Mouse (Híbrido: InputSystem + Legacy Fallback) ──
            bool rightPressed = false;
            Vector2 mouseDelta = Vector2.zero;
            float mouseScroll = 0f;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                rightPressed = mouse.rightButton.isPressed;
                mouseDelta   = mouse.delta.ReadValue();
                mouseScroll  = mouse.scroll.ReadValue().y;
            }
            else
            {
                rightPressed = UnityEngine.Input.GetMouseButton(1);
                mouseDelta   = new Vector2(UnityEngine.Input.GetAxis("Mouse X") * 15f, UnityEngine.Input.GetAxis("Mouse Y") * 15f);
                mouseScroll  = UnityEngine.Input.GetAxis("Mouse ScrollWheel") * 120f;
            }

            // ── Orbitar ──
            if (rightPressed && !_isRightClickPressed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
                _isRightClickPressed = true;
            }
            else if (!rightPressed && _isRightClickPressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
                _isRightClickPressed = false;
            }

            if (_isRightClickPressed)
            {
                _yaw   += mouseDelta.x * _orbitSensitivity;
                _pitch -= mouseDelta.y * _orbitSensitivity;
                _pitch  = Mathf.Clamp(_pitch, -10f, 85f);
            }

            // ── Zoom ──
            if (Mathf.Abs(mouseScroll) > 0.01f)
            {
                _distance -= Mathf.Sign(mouseScroll) * _zoomSensitivity;
                _distance  = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            }

            // ── Movimiento WASD/QE (Híbrido) ──
            Vector3 move = Vector3.zero;
            bool fast = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                fast = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                if (kb.wKey.isPressed) move += transform.forward;
                if (kb.sKey.isPressed) move -= transform.forward;
                if (kb.aKey.isPressed) move -= transform.right;
                if (kb.dKey.isPressed) move += transform.right;
                if (kb.eKey.isPressed) move += Vector3.up;
                if (kb.qKey.isPressed) move -= Vector3.up;
            }
            else
            {
                fast = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                if (UnityEngine.Input.GetKey(KeyCode.W)) move += transform.forward;
                if (UnityEngine.Input.GetKey(KeyCode.S)) move -= transform.forward;
                if (UnityEngine.Input.GetKey(KeyCode.A)) move -= transform.right;
                if (UnityEngine.Input.GetKey(KeyCode.D)) move += transform.right;
                if (UnityEngine.Input.GetKey(KeyCode.E)) move += Vector3.up;
                if (UnityEngine.Input.GetKey(KeyCode.Q)) move -= Vector3.up;
            }

            float speed = _moveSpeed * dt;
            if (fast) speed *= 2.5f;

            if (move.sqrMagnitude > 0f)
            {
                _targetPoint += move.normalized * speed;
            }

            // ── Calcular Posición y Rotación ──
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 targetPos = _targetPoint - (rot * Vector3.forward * _distance);

            transform.position = Vector3.Lerp(transform.position, targetPos, dt * 20f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, dt * 20f);
        }
    }
}
