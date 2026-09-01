using System;
using System.Collections.Generic;

namespace BallisticSimulator.Data
{
    /// <summary>
    /// Agrupa todos los disparos de una sesión de trabajo.
    /// Una sesión comienza al abrir el simulador y termina al cerrarlo o exportar.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public string SessionId;
        public string StartTimestamp;
        public string EndTimestamp;
        public List<ShotData> Shots = new List<ShotData>();

        public int ShotCount => Shots.Count;

        public SessionData()
        {
            SessionId      = Guid.NewGuid().ToString("N")[..8].ToUpper();
            StartTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void Close()
        {
            EndTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void AddShot(ShotData shot)
        {
            shot.ShotId    = Shots.Count + 1;
            shot.SessionId = SessionId;
            Shots.Add(shot);
        }
    }
}
