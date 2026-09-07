using BallisticSimulator.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BallisticSimulator.Camera
{
    /// <summary>
    /// Sistema de Debug de Cámaras e Inputs.
    /// Muestra una superposición (GUI) en pantalla en tiempo real durante Play Mode
    /// para diagnosticar qué cámara está activa, cuáles inputs (New Input System / Legacy)
    /// se están detectando, si la UI tiene el foco y si las transformaciones están cambiando.
    /// 
    /// Atajo: Presiona F1 para mostrar/ocultar el panel de debug.
    /// </summary>
    public class CameraDebugger : MonoBehaviour
    {
        [Header("Configuración de Debug")]
        [SerializeField] private bool _showOnGUI = false;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;

        private SetupCameraController _setupController;
        private FreeCameraController _freeController;
        private UnityEngine.Camera _setupCam;
        private UnityEngine.Camera _freeCam;

        private int _frameCount;
        private float _fps;
        private float _fpsTimer;

        private void Awake()
        {
            _showOnGUI = false;
            enabled = false; // Desactivar script completamente por defecto
            FindReferences();
        }

        private void OnEnable()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            var setupGO = GameObject.Find("SetupCamera");
            if (setupGO != null)
            {
                _setupController = setupGO.GetComponent<SetupCameraController>();
                _setupCam = setupGO.GetComponent<UnityEngine.Camera>();
            }

            var freeGO = GameObject.Find("FreeCamera");
            if (freeGO != null)
            {
                _freeController = freeGO.GetComponent<FreeCameraController>();
                _freeCam = freeGO.GetComponent<UnityEngine.Camera>();
            }
        }

        private void Update()
        {
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _fps = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer = 0f;
            }

            // Tecla F1 o New Input System F1 para conmutar HUD
            var kb = Keyboard.current;
            if ((kb != null && kb.f1Key.wasPressedThisFrame) || TryGetKeyDown(_toggleKey))
            {
                _showOnGUI = !_showOnGUI;
            }
        }

        private bool TryGetKeyDown(KeyCode key)
        {
            try { return UnityEngine.Input.GetKeyDown(key); }
            catch { return false; }
        }

        private void OnGUI()
        {
            if (!_showOnGUI) return;

            // Estilo visual del panel de debug (militar / ciberpunk oscuro)
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f * 1.2f, Screen.height / 1080f * 1.2f, 1f));

            GUILayout.BeginArea(new Rect(10, 10, 480, 720), GUI.skin.box);
            
            // Header
            GUI.color = Color.cyan;
            GUILayout.Label("<b>[ CAMERA & INPUT DEBUGGER ]</b> (F1 para ocultar)", GUI.skin.label);
            GUI.color = Color.white;
            GUILayout.Label($"FPS: {_fps:F1} | Estado Simulación: {(GameStateManager.Instance != null ? GameStateManager.Instance.CurrentState.ToString() : "N/A")}");
            GUILayout.Space(5);

            // ── 1. ESTADO DE CÁMARAS ──────────────────────────────────────────
            GUI.color = Color.yellow;
            GUILayout.Label("<b>1. ESTADO DE CÁMARAS</b>");
            GUI.color = Color.white;

            bool setupActive = _setupCam != null && _setupCam.gameObject.activeInHierarchy;
            bool freeActive  = _freeCam != null && _freeCam.gameObject.activeInHierarchy;

            GUILayout.Label($" • SetupCamera GO: {(setupActive ? "<color=green>ACTIVO</color>" : "<color=red>INACTIVO</color>")}");
            if (_setupController != null)
            {
                GUILayout.Label($"   - Script Enabled: {_setupController.enabled}");
                GUILayout.Label($"   - Pos: {_setupController.transform.position}");
                GUILayout.Label($"   - Rot: {_setupController.transform.eulerAngles}");
            }

            GUILayout.Label($" • FreeCamera GO: {(freeActive ? "<color=green>ACTIVO</color>" : "<color=red>INACTIVO</color>")}");
            if (_freeController != null)
            {
                GUILayout.Label($"   - Script Enabled: {_freeController.enabled}");
                GUILayout.Label($"   - Pos: {_freeController.transform.position}");
                GUILayout.Label($"   - Rot: {_freeController.transform.eulerAngles}");
            }

            GUILayout.Space(5);

            // ── 2. DIAGNÓSTICO DE INPUTS (NUEVO INPUT SYSTEM) ────────────────
            GUI.color = Color.yellow;
            GUILayout.Label("<b>2. NEW INPUT SYSTEM (UnityEngine.InputSystem)</b>");
            GUI.color = Color.white;

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            GUILayout.Label($" • Keyboard.current: {(kb != null ? "<color=green>DETECTADO</color>" : "<color=red>NULL</color>")}");
            if (kb != null)
            {
                string keysPressed = "";
                if (kb.wKey.isPressed) keysPressed += "W ";
                if (kb.aKey.isPressed) keysPressed += "A ";
                if (kb.sKey.isPressed) keysPressed += "S ";
                if (kb.dKey.isPressed) keysPressed += "D ";
                if (kb.qKey.isPressed) keysPressed += "Q ";
                if (kb.eKey.isPressed) keysPressed += "E ";
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) keysPressed += "SHIFT ";
                if (string.IsNullOrEmpty(keysPressed)) keysPressed = "(Ninguna)";
                GUILayout.Label($"   - Teclas Presionadas: <color=cyan>{keysPressed}</color>");
            }

            GUILayout.Label($" • Mouse.current: {(mouse != null ? "<color=green>DETECTADO</color>" : "<color=red>NULL</color>")}");
            if (mouse != null)
            {
                GUILayout.Label($"   - Clic Derecho: {(mouse.rightButton.isPressed ? "<color=lime>PRESIONADO</color>" : "Soltado")}");
                GUILayout.Label($"   - Clic Izquierdo: {(mouse.leftButton.isPressed ? "<color=lime>PRESIONADO</color>" : "Soltado")}");
                GUILayout.Label($"   - Mouse Delta: {mouse.delta.ReadValue()}");
                GUILayout.Label($"   - Scroll Y: {mouse.scroll.ReadValue().y}");
            }

            GUILayout.Space(5);

            // ── 3. DIAGNÓSTICO DE INPUTS (LEGACY INPUT SYSTEM) ───────────────
            GUI.color = Color.yellow;
            GUILayout.Label("<b>3. LEGACY INPUT SYSTEM (UnityEngine.Input)</b>");
            GUI.color = Color.white;

            bool legacyRMB = false;
            bool legacyLMB = false;
            string legacyKeys = "";
            Vector2 legacyMouseDelta = Vector2.zero;

            try
            {
                legacyRMB = UnityEngine.Input.GetMouseButton(1);
                legacyLMB = UnityEngine.Input.GetMouseButton(0);
                if (UnityEngine.Input.GetKey(KeyCode.W)) legacyKeys += "W ";
                if (UnityEngine.Input.GetKey(KeyCode.A)) legacyKeys += "A ";
                if (UnityEngine.Input.GetKey(KeyCode.S)) legacyKeys += "S ";
                if (UnityEngine.Input.GetKey(KeyCode.D)) legacyKeys += "D ";
                if (UnityEngine.Input.GetKey(KeyCode.Q)) legacyKeys += "Q ";
                if (UnityEngine.Input.GetKey(KeyCode.E)) legacyKeys += "E ";
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift)) legacyKeys += "SHIFT ";
                if (string.IsNullOrEmpty(legacyKeys)) legacyKeys = "(Ninguna)";

                legacyMouseDelta = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
                GUILayout.Label($" • Legacy Inputs Activo: <color=green>SI</color>");
                GUILayout.Label($"   - Clic Derecho: {(legacyRMB ? "<color=lime>PRESIONADO</color>" : "Soltado")}");
                GUILayout.Label($"   - Teclas: <color=cyan>{legacyKeys}</color>");
                GUILayout.Label($"   - Axis Mouse X/Y: {legacyMouseDelta}");
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($" • Legacy Inputs: <color=red>DESACTIVADO O CON ERROR</color> ({ex.Message})");
            }

            GUILayout.Space(5);

            // ── 4. ESTADO DE CURSOR Y VENTANA ────────────────────────────────
            GUI.color = Color.yellow;
            GUILayout.Label("<b>4. ESTADO DE CURSOR Y COMPONENTES</b>");
            GUI.color = Color.white;
            GUILayout.Label($" • Cursor LockState: {UnityEngine.Cursor.lockState}");
            GUILayout.Label($" • Cursor Visible: {UnityEngine.Cursor.visible}");

            GUILayout.Space(10);

            // ── 5. ACCIONES DE PRUEBA Y REPARACIÓN ───────────────────────────
            GUI.color = Color.cyan;
            GUILayout.Label("<b>CONTROLES DE MANUAL PRUEBA</b>");
            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Alternar Cámara (Setup ↔ Free)"))
            {
                if (setupActive && _freeCam != null)
                {
                    _setupCam.gameObject.SetActive(false);
                    _freeCam.gameObject.SetActive(true);
                }
                else if (_setupCam != null)
                {
                    if (_freeCam != null) _freeCam.gameObject.SetActive(false);
                    _setupCam.gameObject.SetActive(true);
                }
            }

            if (GUILayout.Button("Resetear Posición Vista"))
            {
                if (_setupCam != null && setupActive)
                {
                    _setupCam.transform.SetPositionAndRotation(new Vector3(12f, 6f, -30f), Quaternion.Euler(8f, 15f, 0f));
                }
                if (_freeCam != null && freeActive)
                {
                    _freeCam.transform.SetPositionAndRotation(new Vector3(12f, 6f, -30f), Quaternion.Euler(8f, 15f, 0f));
                    if (_freeController != null) _freeController.SyncTargetToTransform();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Liberar Cursor (Unlock)"))
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            if (GUILayout.Button("Re-Detectar Referencias"))
            {
                FindReferences();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
