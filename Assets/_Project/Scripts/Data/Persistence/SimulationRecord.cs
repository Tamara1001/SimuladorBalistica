using System;

namespace BallisticSimulator.Data.Persistence
{
    /// <summary>
    /// Modelo de registro individual para el historial de simulaciones en UGS Cloud Save.
    /// Estructura acorde al material de cátedra (Persistencia II), extendida con los
    /// resultados de impacto y objetos afectados solicitados en la consigna.
    /// </summary>
    [Serializable]
    public class SimulationRecord
    {
        public string id;
        public string timestamp;

        // --- Parámetros de lanzamiento ---
        public float angle;
        public float force;
        public float mass;
        public float gravity;

        // --- Resultados de la simulación ---
        public bool  impactHit;
        public float distance;
        public int   boxesHit;

        public SimulationRecord() { }

        public SimulationRecord(ShotData shot)
        {
            id = Guid.NewGuid().ToString("N");
            timestamp = string.IsNullOrEmpty(shot.Timestamp)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                : shot.Timestamp;

            angle     = shot.AngleDegrees;
            force     = shot.InitialVelocity;
            mass      = shot.BulletMassGrams;
            gravity   = shot.Gravity;
            impactHit = shot.ImpactHit;
            distance  = shot.RangeM;
            boxesHit  = shot.BoxesHit;
        }

        /// <summary>Convierte este registro al modelo ShotData del simulador.</summary>
        public ShotData ToShotData()
        {
            return new ShotData
            {
                Timestamp         = this.timestamp,
                AngleDegrees      = this.angle,
                InitialVelocity   = this.force,
                BulletMassGrams   = this.mass,
                Gravity           = this.gravity,
                ImpactHit         = this.impactHit,
                RangeM            = this.distance,
                BoxesHit          = this.boxesHit
            };
        }
    }
}
