using System;
using UnityEngine;


namespace BallisticSimulator.Physics
{
    /// <summary>
    /// Biblioteca de cálculos de balística simplificada (sin drag).
    /// Todos los métodos son estáticos y puros (sin estado), aptos para
    /// ser llamados desde el preview en tiempo real y desde la bala en vuelo.
    ///
    /// Coordenadas: X = horizontal (hacia el blanco), Y = vertical, Z = lateral.
    /// La bala se dispara siempre en el plano XY (sin componente Z).
    /// </summary>
    public static class BulletPhysics
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Posición y velocidad en función del tiempo
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Posición de la bala en el tiempo <paramref name="t"/> (segundos).
        /// </summary>
        /// <param name="origin">Punto de disparo en world-space.</param>
        /// <param name="angleDeg">Ángulo de elevación en grados (0 = horizontal, 90 = vertical).</param>
        /// <param name="v0">Velocidad inicial en m/s.</param>
        /// <param name="gravity">Magnitud de la gravedad (valor positivo, se aplica hacia abajo).</param>
        /// <param name="t">Tiempo transcurrido en segundos.</param>
        public static Vector3 Position(Vector3 origin, float angleDeg, float v0, float gravity, float t)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float vx  = v0 * Mathf.Cos(rad);
            float vy  = v0 * Mathf.Sin(rad);

            float x = origin.x + vx * t;
            float y = origin.y + vy * t - 0.5f * gravity * t * t;
            float z = origin.z;

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Velocidad vectorial de la bala en el tiempo <paramref name="t"/>.
        /// </summary>
        public static Vector3 Velocity(float angleDeg, float v0, float gravity, float t)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float vx  = v0 * Mathf.Cos(rad);
            float vy  = v0 * Mathf.Sin(rad) - gravity * t;
            return new Vector3(vx, vy, 0f);
        }

        /// <summary>
        /// Rapidez (módulo de la velocidad) en el tiempo <paramref name="t"/>.
        /// </summary>
        public static float Speed(float angleDeg, float v0, float gravity, float t) =>
            Velocity(angleDeg, v0, gravity, t).magnitude;

        // ─────────────────────────────────────────────────────────────────────────
        // Métricas analíticas
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tiempo de vuelo hasta que la bala vuelve a la altura de origen (segundos).
        /// Devuelve 0 si la gravedad es 0.
        /// </summary>
        public static float TotalFlightTime(float angleDeg, float v0, float gravity)
        {
            if (gravity <= 0f) return 0f;
            float rad = angleDeg * Mathf.Deg2Rad;
            return 2f * v0 * Mathf.Sin(rad) / gravity;
        }

        /// <summary>
        /// Alcance horizontal máximo (metros) cuando la bala regresa a la altura de origen.
        /// </summary>
        public static float MaxRange(float angleDeg, float v0, float gravity)
        {
            if (gravity <= 0f) return 0f;
            float rad = angleDeg * Mathf.Deg2Rad;
            return v0 * v0 * Mathf.Sin(2f * rad) / gravity;
        }

        /// <summary>
        /// Altura máxima alcanzada por encima del punto de origen (metros).
        /// </summary>
        public static float MaxHeight(float angleDeg, float v0, float gravity)
        {
            if (gravity <= 0f) return 0f;
            float rad = angleDeg * Mathf.Deg2Rad;
            float vy  = v0 * Mathf.Sin(rad);
            return vy * vy / (2f * gravity);
        }

        /// <summary>
        /// Tiempo hasta alcanzar la altura máxima.
        /// </summary>
        public static float TimeToApex(float angleDeg, float v0, float gravity)
        {
            if (gravity <= 0f) return 0f;
            float rad = angleDeg * Mathf.Deg2Rad;
            return v0 * Mathf.Sin(rad) / gravity;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Generación de puntos para el preview de trayectoria
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Genera un array de posiciones que representan la trayectoria de la bala
        /// desde el origen hasta que toca el suelo (Y = groundY) o alcanza maxRange.
        /// </summary>
        /// <param name="origin">Punto de disparo.</param>
        /// <param name="angleDeg">Ángulo de elevación en grados.</param>
        /// <param name="v0">Velocidad inicial en m/s.</param>
        /// <param name="gravity">Gravedad (positivo, hacia abajo).</param>
        /// <param name="groundY">Altura Y del suelo en world-space.</param>
        /// <param name="steps">Número de segmentos de la curva (más = más suave).</param>
        public static Vector3[] ComputeTrajectoryPoints(
            Vector3 origin, float angleDeg, float v0, float gravity,
            float groundY = 0f, int steps = 120)
        {
            float totalTime = TotalFlightTime(angleDeg, v0, gravity);

            // Si el ángulo es 0 o la gravedad es 0, trazar línea recta corta
            if (totalTime <= 0f)
            {
                float dist = v0 * 2f; // 2 segundos de recorrido estimado
                return new[]
                {
                    origin,
                    origin + new Vector3(dist * Mathf.Cos(angleDeg * Mathf.Deg2Rad), 0, 0)
                };
            }

            var points = new Vector3[steps + 1];
            float dt = totalTime / steps;

            for (int i = 0; i <= steps; i++)
            {
                float t = dt * i;
                Vector3 p = Position(origin, angleDeg, v0, gravity, t);
                points[i] = p;

                // Cortar si tocó el suelo antes del tiempo calculado
                if (p.y < groundY && i > 0)
                {
                    // Interpolar punto exacto de impacto con el suelo
                    Vector3 prev = Position(origin, angleDeg, v0, gravity, dt * (i - 1));
                    float frac   = (groundY - prev.y) / (p.y - prev.y);
                    points[i]    = Vector3.Lerp(prev, p, frac);

                    // Redimensionar array al tamaño real
                    var trimmed = new Vector3[i + 1];
                    Array.Copy(points, trimmed, i + 1);
                    return trimmed;
                }
            }

            return points;
        }
    }
}
