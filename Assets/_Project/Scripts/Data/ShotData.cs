using System;
using UnityEngine;

namespace BallisticSimulator.Data
{
    /// <summary>
    /// Datos completos de un disparo individual.
    /// Se persiste en SQLite y se exporta a CSV.
    /// </summary>
    [Serializable]
    public class ShotData
    {
        public int    ShotId;
        public string SessionId;
        public string Timestamp;
        public string PresetName;

        // --- Parámetros de la bala ---
        public float AngleDegrees;
        public float InitialVelocity;   // m/s
        public float BulletMassGrams;   // g
        public float BulletRadiusMm;    // mm
        public float Gravity;           // m/s²

        // --- Parámetros de los targets ---
        public int   BoxCount;
        public float BoxSizeM;          // m (lado del cubo)
        public float BoxMassKg;         // kg
        public float BoxDistanceM;      // m desde el origen
        public int   GridRows;
        public int   GridColumns;

        // --- Resultados ---
        public bool   ImpactHit;
        public float  ImpactX;
        public float  ImpactY;
        public float  ImpactZ;
        public float  FlightTimeSeconds;
        public float  MaxHeightM;
        public float  RangeM;           // distancia horizontal total
        public int    BoxesHit;

        /// <summary>Encabezado CSV</summary>
        public static string CsvHeader =>
            "ShotId;SessionId;Timestamp;PresetName;" +
            "AngleDeg;InitialVelocity_ms;BulletMass_g;BulletRadius_mm;Gravity_ms2;" +
            "BoxCount;BoxSize_m;BoxMass_kg;BoxDistance_m;GridRows;GridColumns;" +
            "ImpactHit;ImpactX;ImpactY;ImpactZ;FlightTime_s;MaxHeight_m;Range_m;BoxesHit";

        /// <summary>Fila CSV</summary>
        public string ToCsvRow() =>
            $"{ShotId};{SessionId};{Timestamp};{PresetName};" +
            $"{AngleDegrees};{InitialVelocity};{BulletMassGrams};{BulletRadiusMm};{Gravity};" +
            $"{BoxCount};{BoxSizeM};{BoxMassKg};{BoxDistanceM};{GridRows};{GridColumns};" +
            $"{ImpactHit};{ImpactX};{ImpactY};{ImpactZ};{FlightTimeSeconds};{MaxHeightM};{RangeM};{BoxesHit}";
    }
}
