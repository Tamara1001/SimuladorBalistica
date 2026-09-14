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
*(Nota: Si deseas construir la escena desde cero, el proyecto trae un instalador propio. Crea una escena vacía y haz clic en `Tools > Ballistic Simulator > 🚀 Setup Completo`)*
