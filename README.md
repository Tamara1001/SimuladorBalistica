# Simulador Balístico 🎯

Un simulador balístico interactivo desarrollado en Unity que permite configurar parámetros físicos en tiempo real, visualizar trayectorias y analizar los resultados del impacto de un proyectil contra distintos objetivos.

Este proyecto implementa y extiende los requisitos académicos propuestos, utilizando una arquitectura **MVC (Model-View-Controller)** para separar responsabilidades y añadiendo características avanzadas de simulación, persistencia de datos y testeo por lotes.

---

## 🚀 Características Principales

- **Simulación Física Precisa:** Cálculo de trayectoria en tiempo real tomando en cuenta ángulo, velocidad, masa y gravedad.
- **Arquitectura MVC Limpia:** Separación estricta entre la lógica de simulación, los datos y la interfaz de usuario.
- **UI Toolkit (Diseño Táctico):** Interfaz moderna estilo "Holodeck / Militar" desarrollada enteramente con UI Toolkit y USS.
- **Persistencia y Exportación:** Guardado de cada disparo en una base de datos local **SQLite** y exportación de sesiones completas a **CSV** (formateado para Excel).
- **Testeo Automático (Batch Testing):** Capacidad de ejecutar iteraciones automáticas barriendo rangos de ángulos y velocidades para obtener métricas masivas.
- **Cámara Picture-in-Picture (PiP):** Seguimiento del proyectil en vuelo mediante el uso de RenderTextures directamente en la UI.
- **Herramientas de Editor Custom:** Scripts de editor para crear la jerarquía de la escena, materiales y dependencias con un solo clic.

---

## 🏗️ Arquitectura MVC: Implementación Académica

El proyecto sigue rigurosamente el patrón **Model-View-Controller** exigido por la cátedra para desacoplar el estado, la visualización y la lógica de control.

### 1. Model (`BallisticParameters`, `ShotData`, `BulletPhysics`)
El modelo contiene los datos puros y las reglas de negocio. No hereda de `MonoBehaviour` y no tiene referencias a la escena ni a la interfaz.
- `BallisticParameters.cs`: Almacena el estado actual de los sliders (ángulo, velocidad, masa, gravedad).
- `ShotData.cs` / `SessionData.cs`: Clases de datos que guardan la información de cada disparo y sus resultados (rango, tiempo de vuelo, altura máxima).
- `BulletPhysics.cs`: Librería estática y matemática pura que resuelve analíticamente el vuelo parabólico sin mezclar lógica de Unity.

### 2. View (`SidePanelUI`, `SidePanel.uxml`, `MilitaryDark.uss`)
La vista es completamente agnóstica de las reglas de la simulación.
- Su única responsabilidad es detectar la interacción del usuario (mover sliders, hacer clics) y notificar al Controller mediante llamadas a sus métodos.
- Al recibir actualizaciones, muestra los resultados en la interfaz de manera visual (texto, UI Toolkit, cámara PiP).

### 3. Controller (`SimulationManager`)
El orquestador central que conecta la Vista con el Modelo.
- Posee la instancia de `BallisticParameters`.
- Expone métodos públicos (`SetAngle()`, `SetVelocity()`, `Fire()`) que la Vista llama.
- Modifica el Modelo y ordena recálculos en la vista (como actualizar la línea de trayectoria).

---

## 🔬 Simulador Físico vs. Rigidbody Tradicional

El material de la cátedra proponía el uso de `Rigidbody` con `AddForce` y `Continuous Dynamic` para la detección de colisiones (previniendo el *tunneling*).

En este proyecto decidimos **mejorar este enfoque** utilizando una aproximación analítica:
- **Cálculo Determinístico:** La trayectoria se calcula cuadro a cuadro matemáticamente (`BulletPhysics.cs`). Esto evita las pequeñas imprecisiones del motor de físicas de Unity (`PhysX`) causadas por la variación del framerate.
- **Detección de Colisiones Avanzada (Anti-Tunneling):** En lugar de depender de `OnCollisionEnter`, el proyectil realiza un `Physics.SphereCastAll` entre su posición en el frame anterior y el frame actual. Esto garantiza una precisión del 100% en la detección del impacto, incluso a velocidades extremas.
- **Jerarquía:** Tal como se recomendó, el origen del disparo (`Muzzle` / `SpawnPoint`) es hijo del cañón (`CannonPivot`), asegurando que al rotar el arma, el punto de origen y la dirección del disparo acompañen el movimiento automáticamente.
- **Tag Ground:** Se respeta la validación de colisiones contra elementos del entorno etiquetados con el Tag `Ground`.

---

## 🛠️ Estructura del Proyecto

```text
Assets/_Project/
├── Scripts/
│   ├── Core/        # Controller principal (SimulationManager, GameStateManager)
│   ├── Data/        # Modelos de datos puros (BallisticParameters, ShotData), SQLite, CSV
│   ├── Physics/     # Reglas matemáticas del motor balístico
│   ├── Camera/      # Manejo de cámara FreeCam y PiP RenderTexture
│   ├── Targets/     # Generación procedural de cajas y físicas de impacto
│   ├── UI/          # View y binding de UI Toolkit
│   └── Editor/      # Herramienta de automatización (Paso a paso)
├── UI/              # Archivos UXML y USS (Diseño Táctico Militar)
├── ScriptableObjects/# Presets de municiones reales (9mm, .50 BMG, etc.)
└── Materials/       # Materiales generados por la herramienta
```

---

## ⚙️ Cómo empezar

1. Abre el proyecto en Unity.
2. Crea una escena vacía.
3. Ve a la barra superior de Unity y selecciona:
   **`Tools > Ballistic Simulator > 🚀 Setup Completo`**
4. La herramienta construirá automáticamente la jerarquía completa de la escena, creará los materiales, inicializará el Canvas con UI Toolkit, configurará la cámara PiP y conectará todas las referencias necesarias.
5. ¡Presiona **Play** y comienza a simular!

---

## 📊 Exportación de Datos

Al finalizar una sesión (o durante la misma), el usuario puede presionar el botón **Exportar CSV**.
El sistema tomará todos los disparos registrados y generará un archivo `.csv` en la carpeta `persistentDataPath`. Este archivo incluye el encabezado `sep=;` y codificación **UTF-8 con BOM** para que pueda ser abierto nativamente por Microsoft Excel en español sin problemas de columnas o caracteres especiales.
