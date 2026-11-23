using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace Cubotris.Gameplay
{
    public static class GameSave
    {
        private static SaveSlotStore store;
        public static SaveSlotStore Store=>store??=new SaveSlotStore(Path.Combine(Application.persistentDataPath,"continue.json"));
        public static CubotrisSession ResumeSession { get; set; }
        public static CubotrisSession TakeResume(){var session=ResumeSession;ResumeSession=null;return session;}
    }
    public sealed class SaveSlotStore
    {
        public string FilePath { get; }
        public SaveSlotStore(string path){FilePath=path;}
        public bool HasSave
        {
            get{if(!File.Exists(FilePath))return false;try{return Read().status!="closed";}catch(Exception){return true;}}
        }
        private SessionSnapshot Read()
        {
            if(new FileInfo(FilePath).Length>2*1024*1024)throw new InvalidDataException("Archivo demasiado grande.");
            string json=File.ReadAllText(FilePath);
            if(!json.Contains("\"version\"") || !json.Contains("\"format\"") || !json.Contains("\"status\""))throw new InvalidDataException("Archivo incompleto.");
            var data=JsonUtility.FromJson<SessionSnapshot>(json);
            if(data==null || data.version!=1 || data.format!="CUBOTRIS_SAVE")throw new InvalidDataException("Formato incompatible.");
            return data;
        }
        public bool TryLoad(out CubotrisSession session,out string error)
        {
            session=null;error=null;
            try
            {
                if(!File.Exists(FilePath)){error="No hay ninguna partida guardada.";return false;}
                var data=Read();if(data.status=="closed"){error="Esta partida ya terminó o se reinició.";return false;}
                session=CubotrisSession.Restore(data);return true;
            }
            catch(Exception){error="No se pudo leer la partida. El archivo se conserva sin cambios; revisa su integridad y los permisos.";return false;}
        }
        public bool TrySave(CubotrisSession session,out string error)
        {
            try{var data=session.Capture();CubotrisSession.Restore(data);return Write(data,out error);}
            catch(Exception){error="No se puede guardar este estado de partida. Tus datos anteriores se conservan.";return false;}
        }
        public bool TryClose(string runId,out string error)
        {
            error=null;
            try
            {
                if(!File.Exists(FilePath))return true;
                var data=Read();if(data.runId!=runId || data.status=="closed")return true;
                // Keep an explicit tombstone: a finished checkpoint must not be revived from its backup.
                return Write(new SessionSnapshot{runId=runId,status="closed"},out error);
            }
            catch(Exception){error="No se pudo marcar la partida guardada como terminada.";return false;}
        }
        private bool Write(SessionSnapshot data,out string error)
        {
            error=null;string temporary=FilePath+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));
                byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(data,true));
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(FilePath))File.Replace(temporary,FilePath,FilePath+".bak");else File.Move(temporary,FilePath);
                return true;
            }
            catch(Exception){error="No se pudo guardar. Comprueba el espacio y los permisos; la partida sigue abierta y el archivo anterior se conserva.";return false;}
        }
    }
}
