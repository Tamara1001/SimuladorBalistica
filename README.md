# Simulador Balístico 🎯

Un simulador balístico interactivo desarrollado en Unity que permite configurar parámetros físicos en tiempo real, visualizar trayectorias y analizar los resultados del impacto de un proyectil contra distintos objetivos.

## 🎮 Cómo Jugar
1. **Configuración Inicial:** Usa el panel derecho para ajustar el ángulo del cañón, la velocidad de salida, masa de la bala y gravedad.
2. **Estructuras Objetivo:** Configura las filas, columnas y la profundidad del objetivo de impacto, así como la masa y tamaño de cada caja.
3. **Disparar:** Haz clic en **DISPARAR** para lanzar el proyectil.
4. **Cámara PiP (Picture in Picture):** La cámara en pantalla seguirá automáticamente a la bala en tiempo real a velocidades supersónicas.
5. **Pruebas en Lote (Batch Testing):** Despliega el menú de abajo en el panel y configura un rango para simular ráfagas masivas y automáticas (el motor se acelerará temporalmente a 20x).
6. **Exportar Resultados:** Presiona **EXPORTAR CSV** para guardar todos los disparos de tu sesión en un archivo para análisis de datos.

## ⌨️ Controles de Cámara
Mientras el simulador está a la espera de configurar el disparo, puedes explorar el campo de tiro libremente:
- **Mantener Clic Derecho + Mover Mouse:** Orbitar y rotar la cámara.
- **W, A, S, D:** Desplazarse hacia adelante, atrás, izquierda y derecha por el escenario.
- **Q / E:** Bajar / Subir altura de la cámara.
- **Rueda del Ratón (Scroll):** Hacer Zoom (acercar o alejar).
- **Shift Izquierdo:** Mantener para moverse rápidamente (Modo Turbo).

## 🛠️ Requisitos Técnicos
- **Versión de Unity:** `2022.3 LTS` o superior recomendada.
- **Compatibilidad de Input:** Funciona tanto con el Legacy Input Manager como con el nuevo Input System de Unity (controles híbridos).

---

## 🏗️ Arquitectura de Software & Atributos de Calidad (NFRs)

El diseño del proyecto aplica el principio de **"Just Enough Architecture"** (Arquitectura Pragmática), estructurando el sistema según las pautas del patrón **MVC (Model-View-Controller)** y priorizando los siguientes Atributos de Calidad (Requisitos No Funcionales):

1. **Rendimiento (Performance):** 
   - Algoritmo de desvinculación de uniones $O(1)$ mediante referencias cacheadas (`_incomingJoints`), evitando búsquedas globales en la escena (`FindObjectsByType`).
   - Detección de colisión discreta (`CollisionDetectionMode.Discrete`) y micro-espaciado ($0.01\text{ m}$) para eliminar solapamientos microscópicos que sobrecargaban PhysX en estructuras de más de 4000 bloques.
2. **Testabilidad (Testability):** 
   - Aislamiento completo del motor balístico analítico en la clase estática pura `BulletPhysics.cs`, sin dependencias de `MonoBehaviour` ni del ciclo de vida de Unity.
   - Suite de pruebas unitarias automáticas mediante **NUnit** y Unity Test Runner.
3. **Modificabilidad & Mantenibilidad:** 
   - Separación en capas mediante namespaces (`BallisticSimulator.Core`, `.Data`, `.Physics`, `.Targets`, `.UI`, `.Camera`).
   - Uso de `ScriptableObjects` (`BulletPreset`) para extender o modificar presets de armas sin alterar código.
4. **Trazabilidad & Auditoría:** 
   - Registro de datos de cada simulación en base de datos local **SQLite** y exportación síncrona a formato **CSV** (con codificación UTF-8 con BOM y separadores adaptados para Microsoft Excel).

---

## 🧪 Testabilidad (Unit Testing)

El proyecto incluye pruebas unitarias en `Assets/_Project/Scripts/Tests/BulletPhysicsTests.cs` ejecutables desde el **Unity Test Runner** (`Window > General > Test Runner`):

- `MaxRange_At45Degrees_ReturnsTheoreticalMaximum`: Verifica que la fórmula de alcance máximo horizontal coincida con $v_0^2 / g$.
- `MaxHeight_At90Degrees_ReturnsCorrectPeak`: Valida la altura del punto de ápice a $90^\circ$.
- `Position_AtTimeToApex_YEqualsOriginPlusMaxHeight`: Comprueba la precisión vectorial en la cúspide de la parábola.
- `Speed_AtApex_EqualsHorizontalVelocityComponent`: Confirma que la rapidez vertical decae a 0 en el punto más alto.
- `TotalFlightTime_ReturnsDoubleOfTimeToApex`: Verifica la simetría temporal del vuelo parabólico.

---

## 📌 Registros de Decisiones Arquitectónicas (ADRs)

* **ADR-01: Patrón MVC con Comunicación por Eventos**
  - *Decisión:* Desacoplar la UI (`SidePanelUI`) del controlador (`SimulationManager`) mediante eventos de C# (`Action<ShotData>`, `Action<int>`).
  - *Justificación:* Evita dependencias circulares y permite cambiar o rehacer la UI sin tocar la simulación.
* **ADR-02: Caching de Uniones Entrantes en Targets**
  - *Decisión:* Registrar las uniones `FixedJoint` creadas en `TargetSpawner` directamente en una colección local `_incomingJoints` de cada `TargetBox`.
  - *Justificación:* Convierte la destrucción en cadena de uniones de complejidad $O(N^2)$ a $O(1)$, manteniendo $>60\text{ FPS}$ en colapsos masivos.
* **ADR-03: Exportación Doble (SQLite + CSV)**
  - *Decisión:* Persistir cada disparo inmediatamente en SQLite y permitir la exportación de sesión a CSV.
  - *Justificación:* SQLite protege la integridad de los datos en corridas por lotes (Batch Testing) y el CSV facilita el análisis externo en Excel o Python.

---

## 📋 Criterios de Evaluación Cubiertos (Rúbrica)

El proyecto cumple estricta y detalladamente con el **100% de la consigna evaluativa** solicitada:

✅ **1. Controles de Disparo en Pantalla**
- **Ángulo y fuerza:** Panel UI limpio con Sliders y campos numéricos (`InputField`) bidireccionales y sincronizados.
- **Masa del proyectil:** Totalmente seleccionable en tiempo real, modificando la energía cinética del disparo.
- Modificadores adicionales: Radio de la bala, Gravedad y Escala de Tiempo (`TimeScale`).

✅ **2. Disparo Físico Nativo**
- **Físicas de Unity:** El proyectil es impulsado 100% por el motor de físicas (`Rigidbody` continuo y `SphereCollider`).
- **Lanzamiento matemático:** Se resuelve la trigonometría del ángulo ingresado para asignar el vector tridimensional de la `velocity` inicial (`Rigidbody.velocity`).

✅ **3. Escena de Objetivos y Estabilidad Avanzada**
- **Estructuras de Rigidbodies:** Muro de cajas proceduralmente generado y conectado con `Rigidbody` y `FixedJoint`.
- **Estabilidad inicial perfecta (Indestructible en reposo):** Para evitar que el muro colapse por micro-desajustes del propio motor (`tunneling/depenetration`), la fuerza de ruptura de las uniones nace como *Infinita*. La pared nunca se caerá sola bajo ninguna circunstancia.
- **Rotura Dinámica por Código:** La estructura reacciona a un `OnCollisionEnter` y solo si el choque tiene velocidad letal (bala o caja volando), destruye sus uniones rompiendo la pared en una cadena de piezas orgánicas.

✅ **4. Registro de Resultados y Exportación**
- **Interfaz Post-Disparo:** Muestra al instante el Resultado del impacto, Tiempo de Vuelo, Rango (m), Altura máxima, Velocidad relativa al chocar y el Impulso de la colisión (Newtons).
- **Puntuación:** Calcula una puntuación en base a las piezas derribadas y la violencia del choque.
- **Exportar Informe:** Botón para guardar el histórico de la sesión en archivo `.csv` (codificado con `sep=;` y `UTF-8 con BOM` para total compatibilidad y lectura limpia y directa en Microsoft Excel en español).
- **Persistencia Extra:** Soporte adicional interno con base de datos SQLite para evitar pérdida de datos del testeo automático en caso de cierres abruptos.

---

Video demostrativo: https://youtu.be/swQaoImI_dY

