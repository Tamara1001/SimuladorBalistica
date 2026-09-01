using UnityEngine;

namespace BallisticSimulator.UI
{
    /// <summary>
    /// ScriptableObject que representa un preset de munición real.
    /// Crear instancias via Assets > Create > BallisticSimulator > Bullet Preset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BulletPreset",
        menuName = "BallisticSimulator/Bullet Preset")]
    public class BulletPreset : ScriptableObject
    {
        [Header("Identificación")]
        public string PresetName = "9mm Parabellum";

        [Header("Parámetros físicos")]
        [Tooltip("Masa de la bala en gramos.")]
        public float MassGrams = 8f;

        [Tooltip("Radio de la bala en milímetros.")]
        public float RadiusMm = 4.5f;

        [Tooltip("Velocidad inicial (muzzle velocity) en m/s.")]
        public float MuzzleVelocity = 370f;

        [Header("Descripción")]
        [TextArea(2, 4)]
        public string Description = "";
    }
}
