using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BallisticSimulator.Data
{
    /// <summary>
    /// Exporta una sesión completa a un archivo CSV.
    /// El archivo se guarda en Application.persistentDataPath/Sessions/.
    /// </summary>
    public static class CSVExporter
    {
        private static readonly string ExportFolder =
            Path.Combine(Application.persistentDataPath, "Sessions");

        /// <summary>
        /// Exporta la sesión y devuelve la ruta completa del archivo generado.
        /// </summary>
        public static string Export(SessionData session)
        {
            if (session == null || session.Shots.Count == 0)
            {
                Debug.LogWarning("[CSVExporter] No hay disparos en la sesión para exportar.");
                return null;
            }

            // Asegurar que existe la carpeta
            if (!Directory.Exists(ExportFolder))
                Directory.CreateDirectory(ExportFolder);

            session.Close();

            string fileName = $"Session_{session.SessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string fullPath = Path.Combine(ExportFolder, fileName);

            var sb = new StringBuilder();
            // "sep=;" le indica a Excel qué separador usar, sin importar el locale del sistema
            sb.AppendLine("sep=;");
            sb.AppendLine(ShotData.CsvHeader);
            foreach (var shot in session.Shots)
                sb.AppendLine(shot.ToCsvRow());

            // UTF-8 con BOM: Excel lo necesita para mostrar tildes y ñ correctamente
            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            Debug.Log($"[CSVExporter] Sesión exportada → {fullPath}");
            return fullPath;
        }

        /// <summary>
        /// Abre la carpeta de exportación en el explorador de archivos del SO.
        /// </summary>
        public static void OpenExportFolder()
        {
            if (!Directory.Exists(ExportFolder))
                Directory.CreateDirectory(ExportFolder);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", ExportFolder.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", ExportFolder);
#else
            Debug.Log($"[CSVExporter] Carpeta de sesiones: {ExportFolder}");
#endif
        }
    }
}
