using NUnit.Framework;
using UnityEngine;
using BallisticSimulator.Physics;

namespace BallisticSimulator.Tests
{
    /// <summary>
    /// Pruebas unitarias para validar las ecuaciones balísticas analíticas (BulletPhysics.cs).
    /// Ejecutables en el Unity Test Runner (EditMode).
    /// </summary>
    public class BulletPhysicsTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void MaxRange_At45Degrees_ReturnsTheoreticalMaximum()
        {
            float v0 = 100f;
            float gravity = 9.81f;
            float angleDeg = 45f;

            // R = (v0^2 * sin(90°)) / g = 10000 / 9.81 = 1019.367m
            float expectedRange = (v0 * v0) / gravity;
            float actualRange = BulletPhysics.MaxRange(angleDeg, v0, gravity);

            Assert.AreEqual(expectedRange, actualRange, Tolerance,
                "El alcance máximo a 45° debe coincidir exactamente con v0^2 / g.");
        }

        [Test]
        public void MaxHeight_At90Degrees_ReturnsCorrectPeak()
        {
            float v0 = 100f;
            float gravity = 9.81f;
            float angleDeg = 90f;

            // H = (v0^2 * sin^2(90°)) / (2g) = 10000 / 19.62 = 509.684m
            float expectedHeight = (v0 * v0) / (2f * gravity);
            float actualHeight = BulletPhysics.MaxHeight(angleDeg, v0, gravity);

            Assert.AreEqual(expectedHeight, actualHeight, Tolerance,
                "La altura máxima a 90° debe ser v0^2 / (2g).");
        }

        [Test]
        public void Position_AtTimeToApex_YEqualsOriginPlusMaxHeight()
        {
            Vector3 origin = new Vector3(10f, 5f, 0f);
            float v0 = 80f;
            float gravity = 9.81f;
            float angleDeg = 30f;

            float timeToApex = BulletPhysics.TimeToApex(angleDeg, v0, gravity);
            float maxHeight = BulletPhysics.MaxHeight(angleDeg, v0, gravity);
            Vector3 positionAtApex = BulletPhysics.Position(origin, angleDeg, v0, gravity, timeToApex);

            Assert.AreEqual(origin.y + maxHeight, positionAtApex.y, Tolerance,
                "La coordenada Y de la posición en el tiempo del ápice debe coincidir con origen + MaxHeight.");
        }

        [Test]
        public void Speed_AtApex_EqualsHorizontalVelocityComponent()
        {
            float v0 = 120f;
            float gravity = 9.81f;
            float angleDeg = 60f;

            float timeToApex = BulletPhysics.TimeToApex(angleDeg, v0, gravity);
            float speedAtApex = BulletPhysics.Speed(angleDeg, v0, gravity, timeToApex);

            // En el ápice, vy = 0, por ende la rapidez es únicamente la velocidad horizontal vx = v0 * cos(angle)
            float expectedSpeed = v0 * Mathf.Cos(angleDeg * Mathf.Deg2Rad);

            Assert.AreEqual(expectedSpeed, speedAtApex, Tolerance,
                "En el punto más alto, la velocidad vertical es 0 y la rapidez debe ser v0 * cos(θ).");
        }

        [Test]
        public void TotalFlightTime_ReturnsDoubleOfTimeToApex()
        {
            float v0 = 50f;
            float gravity = 9.81f;
            float angleDeg = 40f;

            float apexTime = BulletPhysics.TimeToApex(angleDeg, v0, gravity);
            float totalTime = BulletPhysics.TotalFlightTime(angleDeg, v0, gravity);

            Assert.AreEqual(apexTime * 2f, totalTime, Tolerance,
                "El tiempo total de vuelo sobre suelo plano debe ser exactamente el doble del tiempo al ápice.");
        }
    }
}
