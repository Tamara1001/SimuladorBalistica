# Simulador Balístico 3D

Simulador balístico interactivo desarrollado en Unity. Permite configurar parámetros físicos en tiempo real, proyectar trayectorias analíticas, resolver la simulación física de proyectiles e impactos contra estructuras destructibles de cuerpos rígidos, y gestionar la persistencia tanto local como remota mediante Unity Gaming Services (UGS).

---

## Guía de Uso

1. **Configuración de Disparo:** En el panel lateral derecho se ajustan el ángulo del cañón, la velocidad inicial (fuerza), la masa del proyectil, el radio de la bala y la gravedad.
2. **Estructuras Objetivo:** Permite configurar proceduralmente la cantidad de filas, columnas y profundidad de la pared de cajas, así como el tamaño y masa de cada bloque.
3. **Disparo:** El botón **DISPARAR** lanza el proyectil aplicando las fuerzas calculadas al Rigidbody.
4. **Cámara PiP (Picture in Picture):** Ventana secundaria que realiza un seguimiento continuo y automático del proyectil en tiempo real durante su trayectoria de vuelo.
5. **Pruebas en Lote (Batch Testing):** Menú desplegable para ejecutar simulaciones masivas iterando rangos de ángulo y velocidad con escala temporal acelerada (20x).
6. **Persistencia y Recuperación en Nube (UGS):**
   - **Guardar Configuración:** Sube el estado actual de los parámetros a Cloud Save.
   - **Cargar Configuración:** Descarga la configuración persistida y sincroniza automáticamente los controles y el cañón.
   - **Ver Historial:** Consulta en UGS todos los disparos realizados y despliega una lista detallada con resultados y estadísticas.
7. **Exportar Datos Locales:** El botón **EXPORTAR CSV** genera un reporte con el historial completo de la sesión compatible con hojas de cálculo.

---

## Controles de Navegación

Durante el modo de configuración, la cámara libre permite explorar el entorno de prueba:
- **Clic Derecho Sostenido + Movimiento de Ratón:** Rotación y orientación orbital de la cámara.
- **W, A, S, D:** Traslación hacia adelante, atrás, izquierda y derecha.
- **Q / E:** Desplazamiento vertical (descenso / ascenso).
- **Rueda del Ratón:** Control de zoom.
- **Shift Izquierdo:** Modificador de velocidad de desplazamiento rápido.

---

## Requisitos Técnicos

- **Versión de Unity:** 2022.3 LTS o superior recomendada (probado en Unity 6000.3).
- **Servicios:** Unity Gaming Services (paquetes `com.unity.services.core`, `com.unity.services.authentication`, `com.unity.services.cloudsave`).
- **Compatibilidad de Input:** Funciona tanto con el Input Manager clásico como con el nuevo Input System de Unity.

---

## Arquitectura de Software y Atributos de Calidad

El sistema implementa el patrón arquitectónico **Model-View-Controller (MVC)** desacoplado mediante eventos de C#, junto con el patrón **Repository** para la capa de persistencia de datos.

### Atributos de Calidad (Requisitos No Funcionales)

1. **Rendimiento (Performance):**
   - Estabilidad física mediante congelamiento de restricciones (`RigidbodyConstraints.FreezeAll`) en reposo para evitar cálculos innecesarios de microdesplazamiento en estructuras masivas, liberándose de forma reactiva (`UnfreezeAll`) al primer impacto.
   - Algoritmo de desvinculación de cajas en complejidad O(1) con caché de referencias locales, evitando búsquedas globales recursivas en escena.
   - Detección de colisiones optimizada en modo discreto para mantener una tasa de refresco estable (>60 FPS).
2. **Modificabilidad y Desacoplamiento:**
   - La vista (`SidePanelUI`) no interactúa de forma directa con el modelo de dominio (`BallisticParameters`), sino a través de la API pública del controlador (`SimulationManager`).
   - El subsistema de persistencia abstrae el proveedor de datos mediante la interfaz base `SimulationRepository`. Esto permite sustituir Unity Gaming Services por Firebase, AWS o almacenamiento en disco sin alterar la lógica de simulación.
3. **Testabilidad (Testability):**
   - El cálculo físico analítico se encuentra aislado en la clase pura `BulletPhysics.cs`, desacoplada del ciclo de vida de Unity (`MonoBehaviour`).
   - Cobertura de pruebas unitarias automatizadas con NUnit bajo el Unity Test Runner.
4. **Trazabilidad y Auditoría:**
   - Persistencia remota en la nube por cada disparo completado.
   - Almacenamiento local redundante en SQLite y exportación a formato CSV delimitado por punto y coma con codificación UTF-8 con BOM.

---

## Persistencia con Unity Gaming Services (UGS)

El proyecto integra los servicios de backend de **Unity Gaming Services (Cloud Save y Authentication)** para la gestión de datos persistentes:

- **Autenticación Anónima:** Al iniciar la aplicación, `UgsInitializer` inicializa los servicios del SDK de Unity y autentica la sesión del cliente de manera transparente mediante `AuthenticationService.Instance.SignInAnonymouslyAsync()`.
- **Persistencia de Parámetros de Estado:** Permite almacenar y recuperar los parámetros de la simulación activa (`angle`, `force`, `mass`, `gravity` y esquema versionado en JSON).
- **Registro Automático de Historial por Disparo:** Cada vez que un disparo concluye o colisiona, se serializa un registro (`SimulationRecord`) y se persiste de manera asincrónica e independiente en Cloud Save:
  - Ángulo de tiro.
  - Fuerza / Velocidad inicial aplicada.
  - Masa del proyectil.
  - Resultado del impacto (acierto o fallo junto con la distancia horizontal alcanzada).
  - Cantidad de cajas/objetos afectados por el choque.
- **Estrategia de Claves Individuales:** Cada ejecución genera una clave única con el prefijo `simulation_<GUID>`, asegurando que los nuevos resultados no sobrescriban registros previos.
- **Consulta de Historial:** A través del botón **VER HISTORIAL**, el cliente ejecuta una consulta asincrónica (`LoadAllAsync`), filtra los registros con el prefijo correspondiente, los deserializa y los presenta en un panel modal con interfaz desplazable, imprimiendo además el reporte consolidado en la consola de Unity.

---

## Pruebas Unitarias (Unit Testing)

Ubicadas en `Assets/_Project/Scripts/Tests/BulletPhysicsTests.cs`, ejecutables desde el entorno Unity Test Runner (`Window > General > Test Runner`):

- `MaxRange_At45Degrees_ReturnsTheoreticalMaximum`: Comprueba que el alcance máximo horizontal a 45 grados concuerde teóricamente con la fórmula (v0^2 / g).
- `MaxHeight_At90Degrees_ReturnsCorrectPeak`: Valida la altura máxima vertical alcanzada a 90 grados.
- `Position_AtTimeToApex_YEqualsOriginPlusMaxHeight`: Comprueba la posición vectorial exacta en el vértice superior de la parábola.
- `Speed_AtApex_EqualsHorizontalVelocityComponent`: Verifica que la componente vertical de velocidad sea nula en el punto más alto del recorrido.
- `TotalFlightTime_ReturnsDoubleOfTimeToApex`: Valida la simetría del tiempo total de vuelo en superficie nivelada.

---

## Registros de Decisiones Arquitectónicas (ADRs)

- **ADR-01: Patrón MVC con Comunicación por Eventos**
  - *Contexto:* Se requiere evitar dependencias cíclicas entre la interfaz de usuario y la simulación física.
  - *Decisión:* Desacoplar la vista del controlador mediante eventos C# (`Action<ShotData>`, `Action<int>`).
  - *Consecuencia:* La interfaz puede modificarse o reemplazarse sin afectar la lógica de negocio.

- **ADR-02: Manejo de Estabilidad en Estructuras Rígidas**
  - *Contexto:* Estructuras con gran cantidad de cuerpos rígidos o torres elevadas sufrían inestabilidades acumulativas por tolerancias numéricas del motor de físicas.
  - *Decisión:* Mantener los bloques en reposo con restricciones congeladas (`FreezeAll`) y desinhibirlos reactivamente en el momento del impacto (`UnfreezeAll`).
  - *Consecuencia:* Estabilidad estática absoluta en la etapa de configuración y comportamiento destructible dinámico al colisionar.

- **ADR-03: Abstracción de Persistencia mediante el Patrón Repository**
  - *Contexto:* La aplicación debe soportar persistencia remota en UGS sin acoplar la lógica central al SDK del proveedor.
  - *Decisión:* Definir la clase abstracta `SimulationRepository` como contrato intermediario entre el controlador y la implementación concreta `UgsSimulationRepository`.
  - *Consecuencia:* Permite intercambiar el backend (por ejemplo, hacia Firebase o almacenamiento local) sin modificar el simulador ni la interfaz.

- **ADR-04: Estrategia de Claves para Historial en Cloud Save**
  - *Contexto:* Necesidad de registrar múltiples disparos independientes sin sobrescribir las claves de configuración del usuario.
  - *Decisión:* Asignar identificadores únicos a cada registro con prefijo común (`simulation_<GUID>`) y recuperarlos mediante filtrado con `LoadAllAsync()`.
  - *Consecuencia:* Trazabilidad completa de simulaciones pasadas cumpliendo con los estándares de diseño de persistencia de cátedra.

- **ADR-05: Redundancia de Exportación Local (SQLite y CSV)**
  - *Contexto:* Permitir la auditoría y análisis de datos fuera del entorno de Unity.
  - *Decisión:* Registro en base de datos local SQLite combinado con exportación a CSV delimitado por punto y coma y codificación UTF-8 con BOM.
  - *Consecuencia:* Compatibilidad directa e inmediata con Microsoft Excel y herramientas de procesamiento de datos externas.

---

## Demostración

Enlace al video demostrativo: https://youtu.be/swQaoImI_dY
