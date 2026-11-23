using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace Cubotris.Gameplay
{
    public enum RunOutcome { Started, Suspended, Completed, Abandoned }
    [Serializable] public sealed class RunStatistic
    {
        public string id;
        public DifficultyMode mode;
        public bool practice;
        public RunOutcome outcome;
        public int score,lines,maxCombo,face;
        public double seconds;
        public int[] faceLines;
        public double[] faceSeconds;
        public RunStatistic Copy(){var copy=(RunStatistic)MemberwiseClone();copy.faceLines=(int[])faceLines.Clone();copy.faceSeconds=(double[])faceSeconds.Clone();return copy;}
        public static RunStatistic From(CubotrisSession s,RunOutcome result)=>new RunStatistic{id=s.RunId,mode=s.Mode,practice=s.IsPractice,outcome=result,score=s.Game.Score,lines=s.Game.LinesCleared,maxCombo=s.Game.MaxCombo,face=s.ActiveFace,seconds=s.SurvivalSeconds,
            faceLines=Enumerable.Range(0,4).Select(s.TotalFaceLines).ToArray(),faceSeconds=Enumerable.Range(0,4).Select(s.TotalFaceSeconds).ToArray()};
    }
    public sealed class StatisticsSummary
    {
        public int Played,Completed,Abandoned,Suspended,MaxCombo;
        public long Lines;
        public double Seconds,AverageCompletedSeconds;
        public readonly long[] FaceLines=new long[4];
        public readonly double[] FaceSeconds=new double[4];
        public readonly int[] FaceLosses=new int[4];
        public string HardestFaces
        {
            get{int maximum=FaceLosses.Max();return maximum==0?"Sin partidas terminadas":string.Join(", ",Enumerable.Range(0,4).Where(i=>FaceLosses[i]==maximum).Select(i=>"CARA "+(i+1)));}
        }
    }
    public static class PersonalStats
    {
        private static StatisticsStore store;
        public static StatisticsStore Store=>store??=new StatisticsStore(Path.Combine(Application.persistentDataPath,"statistics.json"));
    }
    public sealed class StatisticsStore
    {
        [Serializable] private sealed class Document{public int version=1;public List<RunStatistic> runs=new List<RunStatistic>();}
        private Document data=new Document();
        private bool blocked;
        public string FilePath { get; }
        public string Notice { get; private set; }
        public IReadOnlyList<RunStatistic> Runs=>data.runs.Select(r=>r.Copy()).ToArray();
        public StatisticsStore(string path)
        {
            FilePath=path;
            if(!File.Exists(path))return;
            try
            {
                if(new FileInfo(path).Length>64*1024*1024)throw new InvalidDataException();
                string json=File.ReadAllText(path);if(!json.Contains("\"version\"") || !json.Contains("\"runs\""))throw new InvalidDataException();
                var loaded=JsonUtility.FromJson<Document>(json);
                if(loaded==null || loaded.version!=1 || loaded.runs==null || loaded.runs.Any(r=>!Valid(r)) || loaded.runs.Select(r=>r.id).Distinct().Count()!=loaded.runs.Count)throw new InvalidDataException();
                data=loaded;
            }
            catch(Exception){blocked=true;Notice="No se pudieron leer las estadísticas. El archivo original se conserva; restaura una copia válida antes de registrar más partidas.";}
        }
        private static bool Final(RunOutcome outcome)=>outcome==RunOutcome.Completed || outcome==RunOutcome.Abandoned;
        public bool IsFinal(string id)=>data.runs.Any(r=>r.id==id && Final(r.outcome));
        public bool TryUpdate(CubotrisSession session,RunOutcome outcome,out string error)=>TryRecord(RunStatistic.From(session,outcome),out error);
        public bool TryRecord(RunStatistic record,out string error)
        {
            error=null;if(blocked){error=Notice;return false;}
            if(!Valid(record)){error="Los datos estadísticos no son válidos.";return false;}
            if(IsFinal(record.id))return true;
            var next=new Document{runs=data.runs.Select(r=>r.Copy()).ToList()};int index=next.runs.FindIndex(r=>r.id==record.id);
            if(index<0)next.runs.Add(record.Copy());else next.runs[index]=record.Copy();
            string temporary=FilePath+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(next,true));
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(FilePath))File.Replace(temporary,FilePath,FilePath+".bak");else File.Move(temporary,FilePath);
                data=next;Notice=null;return true;
            }
            catch(Exception){error=Notice="No se pudieron guardar las estadísticas. Los datos anteriores se conservan; comprueba el espacio y los permisos.";return false;}
        }
        private static bool Valid(RunStatistic r)=>r!=null && Guid.TryParse(r.id,out _) && Enum.IsDefined(typeof(DifficultyMode),r.mode) && Enum.IsDefined(typeof(RunOutcome),r.outcome)
            && r.score>=0 && r.lines>=0 && r.maxCombo>=0 && r.face>=0 && r.face<4 && SnapshotCheck.Number(r.seconds) && r.faceLines?.Length==4 && r.faceSeconds?.Length==4
            && r.faceLines.All(v=>v>=0) && r.faceSeconds.All(v=>SnapshotCheck.Number(v)) && r.faceLines.Sum(v=>(long)v)==r.lines && Math.Abs(r.faceSeconds.Sum()-r.seconds)<.01;
        public StatisticsSummary Summarize(bool practice=false)
        {
            var summary=new StatisticsSummary();double completedTime=0;
            foreach(var r in data.runs.Where(r=>r.practice==practice))
            {
                summary.Played++;summary.Lines+=r.lines;summary.Seconds+=r.seconds;summary.MaxCombo=Math.Max(summary.MaxCombo,r.maxCombo);
                if(r.outcome==RunOutcome.Completed){summary.Completed++;completedTime+=r.seconds;summary.FaceLosses[r.face]++;}
                else if(r.outcome==RunOutcome.Abandoned)summary.Abandoned++;else summary.Suspended++;
                for(int i=0;i<4;i++){summary.FaceLines[i]+=r.faceLines[i];summary.FaceSeconds[i]+=r.faceSeconds[i];}
            }
            summary.AverageCompletedSeconds=summary.Completed==0?0:completedTime/summary.Completed;return summary;
        }
    }
}
