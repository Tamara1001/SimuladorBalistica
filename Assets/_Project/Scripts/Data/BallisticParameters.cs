using System;
using UnityEngine;

namespace BallisticSimulator.Data
{
    /// <summary>
    /// Model puro: contiene el estado actual de los parámetros de la simulación.
    /// No hereda de MonoBehaviour y no tiene referencias a escena, UI ni input.
    ///
    /// MVC: este es el MODEL. El Controller (SimulationManager) lo posee y
    /// expone métodos para mutarlo. La View (SidePanelUI) nunca toca esta clase
    /// directamente — solo llama métodos del Controller.
    /// </summary>
    [Serializable]
    public class BallisticParameters
    {
        [Header("Munición")]
        public float  AngleDegrees    = 10f;
        public float  InitialVelocity = 45f;   // m/s
        public float  BulletMassG     = 8f;    // gramos
        public float  BulletRadiusMm  = 4.5f;  // milímetros
        public string PresetName      = "9mm Parabellum";

        [Header("Entorno")]
        public float Gravity   = 9.81f; // m/s²
        public float TimeScale = 1.0f;  // multiplicador de tiempo
    }
}
