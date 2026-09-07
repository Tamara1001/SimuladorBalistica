using BallisticSimulator.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BallisticSimulator.Camera
{
    /// <summary>
    /// Controla la cámara libre (FPS) durante el vuelo o pausa de la simulación.
    /// Soporta detección combinada robusta (Nuevo Input System + Legacy Input System).
    /// Usa unscaledDeltaTime para funcionar perfectamente en Pausa y Slow-Motion.
    ///
    /// Controles:
    /// - Mantener Clic Derecho + Mover Mouse: Rotar vista (Mouse Look)
    /// - WASD: Mover adelante / atrás / izquierda / derecha
    /// - E / Q: Subir / Bajar
    /// - Shift: Velocidad rápida (Turbo)
    /// - Rueda del Mouse: Zoom rápido
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Velocidades")]
        [SerializeField] private float _moveSpeed        = 20f;
        [SerializeField] private float _fastMultiplier   = 3f;
        [SerializeField] private float _mouseSensitivity = 0.3f;

        [Header("Suavizado")]
        [SerializeField] private float _positionSmoothing = 15f;
        [SerializeField] private float _rotationSmoothing = 20f;

        // ── Estado interno ────────────────────────────────────────────────────────
        private float      _yaw;
        private float      _pitch;
        private Vector3    _targetPosition;
        private Quaternion _targetRotation;
        private bool       _active;
        private bool       _isRightClickPressed;

        // ── Unity ────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            SyncTargetToTransform();
            _active = true;
        }

        private void OnDisable()
        {
            UnlockCursor();
            _active = false;
        }

        private void Update()
        {
            if (!_active) return;

            float dt = Time.unscaledDeltaTime;

            // ── Leer Clic Derecho e Inputs de Mouse (Combinación Híbrida Robusta) ──
            bool rightPressed = ReadRightClick();
            Vector2 mouseDelta = ReadMouseDelta();
            float mouseScroll = ReadMouseScroll();

            if (rightPressed && !_isRightClickPressed)
            {
                LockCursor();
                _isRightClickPressed = true;
            }
            else if (!rightPressed && _isRightClickPressed)
            {
                UnlockCursor();
                _isRightClickPressed = false;
            }

            if (_isRightClickPressed)
            {
                _yaw   += mouseDelta.x * _mouseSensitivity;
                _pitch -= mouseDelta.y * _mouseSensitivity;
                _pitch  = Mathf.Clamp(_pitch, -89f, 89f);

                _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            if (Mathf.Abs(mouseScroll) > 0.01f)
            {
                _targetPosition += transform.forward * Mathf.Sign(mouseScroll) * 3f;
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
            if (fast) speed *= _fastMultiplier;

            if (moveDir.sqrMagnitude > 0f)
                _targetPosition += moveDir.normalized * speed;

            ApplySmoothing();
        }

        private void ApplySmoothing()
        {
            float dt = Time.unscaledDeltaTime;

            transform.position = Vector3.Lerp(
                transform.position, _targetPosition,
                _positionSmoothing * dt);

            transform.rotation = Quaternion.Slerp(
                transform.rotation, _targetRotation,
                _rotationSmoothing * dt);
        }

        // ── Cursor ────────────────────────────────────────────────────────────────
        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ── Helper ────────────────────────────────────────────────────────────────
        public void SyncTargetToTransform()
        {
            _targetPosition = transform.position;
            _yaw            = transform.eulerAngles.y;
            _pitch          = transform.eulerAngles.x;
            _targetRotation = transform.rotation;
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
