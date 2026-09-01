using BallisticSimulator.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BallisticSimulator.Camera
{
    /// <summary>
    /// Controla la cámara libre (FPS) durante el vuelo o pausa de la simulación.
    /// Soporta tanto el Nuevo Input System como el Legacy Input System por fallback.
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
            if (fast) speed *= _fastMultiplier;

            if (move.sqrMagnitude > 0f)
                _targetPosition += move.normalized * speed;

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
    }
}
