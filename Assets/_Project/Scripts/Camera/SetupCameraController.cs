using UnityEngine;
using UnityEngine.InputSystem;

namespace BallisticSimulator.Camera
{
    /// <summary>
    /// Permite rotar, desplazar y hacer zoom con la SetupCamera durante la fase de configuración.
    /// Soporta detección combinada robusta (Nuevo Input System + Legacy Input System).
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
        [SerializeField] private Vector3 _targetPoint = new Vector3(3f, 1.5f, 0f);

        [Header("Velocidades")]
        [SerializeField] private float _orbitSensitivity = 0.3f;
        [SerializeField] private float _zoomSensitivity  = 3f;
        [SerializeField] private float _moveSpeed        = 18f;

        private float _yaw   = 57f;
        private float _pitch = 3.5f;
        private bool  _isRightClickPressed;

        private void OnEnable()
        {
            _targetPoint = transform.position;
            _yaw         = transform.eulerAngles.y;
            _pitch       = transform.eulerAngles.x;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // ── Leer Clic Derecho e Inputs de Mouse (Combinación Híbrida Robusta) ──
            bool rightPressed = ReadRightClick();
            Vector2 mouseDelta = ReadMouseDelta();
            float mouseScroll = ReadMouseScroll();

            // ── Estado de Orbitado ──
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

            // ── Scroll / Avance ──
            if (Mathf.Abs(mouseScroll) > 0.01f)
            {
                _targetPoint += transform.forward * Mathf.Sign(mouseScroll) * _zoomSensitivity;
            }

            // ── Movimiento WASD/QE (Detección Híbrida Combinada) ──
            Vector3 moveDir = Vector3.zero;

            if (IsKeyPressed(Key.W, KeyCode.W)) moveDir += transform.forward;
            if (IsKeyPressed(Key.S, KeyCode.S)) moveDir -= transform.forward;
            if (IsKeyPressed(Key.A, KeyCode.A)) moveDir -= transform.right;
            if (IsKeyPressed(Key.D, KeyCode.D)) moveDir += transform.right;
            if (IsKeyPressed(Key.E, KeyCode.E)) moveDir += Vector3.up;
            if (IsKeyPressed(Key.Q, KeyCode.Q)) moveDir -= Vector3.up;

            bool fast = IsKeyPressed(Key.LeftShift, KeyCode.LeftShift) || IsKeyPressed(Key.RightShift, KeyCode.RightShift);

            float speed = _moveSpeed * dt;
            if (fast) speed *= 2.5f;

            if (moveDir.sqrMagnitude > 0f)
            {
                _targetPoint += moveDir.normalized * speed;
            }

            // ── Calcular Posición y Rotación ──
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);

            transform.position = Vector3.Lerp(transform.position, _targetPoint, dt * 20f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, dt * 20f);
        }

        // ── Helpers Híbridos de Lectura de Input ──────────────────────────────

        private bool ReadRightClick()
        {
            var mouse = Mouse.current;
            bool newSys = mouse != null && mouse.rightButton.isPressed;
            bool oldSys = false;
            try { oldSys = UnityEngine.Input.GetMouseButton(1); } catch { }
            return newSys || oldSys;
        }

        private Vector2 ReadMouseDelta()
        {
            Vector2 delta = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                delta = mouse.delta.ReadValue();
            }

            // Fallback a legacy si New System da 0
            if (delta.sqrMagnitude < 0.001f)
            {
                try
                {
                    delta = new Vector2(UnityEngine.Input.GetAxis("Mouse X") * 15f, UnityEngine.Input.GetAxis("Mouse Y") * 15f);
                }
                catch { }
            }

            return delta;
        }

        private float ReadMouseScroll()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float val = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(val) > 0.01f) return val;
            }

            try { return UnityEngine.Input.GetAxis("Mouse ScrollWheel") * 120f; }
            catch { return 0f; }
        }

        private bool IsKeyPressed(Key newKey, KeyCode legacyKey)
        {
            var kb = Keyboard.current;
            bool newSys = kb != null && kb[newKey].isPressed;
            bool oldSys = false;
            try { oldSys = UnityEngine.Input.GetKey(legacyKey); } catch { }
            return newSys || oldSys;
        }
    }
}
