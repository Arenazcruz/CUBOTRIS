using System;
using System.IO;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
namespace Cubotris.Tests
{
    public class DifficultyTests
    {
        [Test] public void NormalKeepsOriginalRulesAndOtherModesChangeOnlyTiming()
        {
            var normal=new CubotrisSession();var easy=new CubotrisSession(mode:DifficultyMode.Relaxed);var hard=new CubotrisSession(mode:DifficultyMode.Challenge);
            Assert.That(normal.Game.GravityInterval,Is.EqualTo(.8f).Within(.001));Assert.That(easy.Game.GravityInterval,Is.GreaterThan(normal.Game.GravityInterval));Assert.That(hard.Game.GravityInterval,Is.LessThan(normal.Game.GravityInterval));
            Assert.That(normal.SecondsUntilGarbage(1),Is.EqualTo(30));Assert.That(hard.SecondsUntilGarbage(1),Is.EqualTo(20).Within(.001));
            easy.Game.HardDrop();easy.AdvanceActiveTime(10000);Assert.That(easy.PendingGarbageRows(1),Is.Zero);Assert.That(double.IsInfinity(easy.SecondsUntilGarbage(1)),Is.True);
            Assert.That(GarbageWarning.Message(1,easy.SecondsUntilGarbage(1),0),Does.Contain("SIN"));
        }
        [Test] public void RankingsAndDeletionRemainSeparateAfterReloadAndOldRecordsDefaultToNormal()
        {
            string directory=Path.Combine(Path.GetTempPath(),"CubotrisModes-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(directory,"records.json");
            try
            {
                var store=new ScoreHistoryStore(path);
                foreach(var mode in new[]{DifficultyMode.Normal,DifficultyMode.Relaxed,DifficultyMode.Challenge})
                    Assert.That(store.TryAdd(new ScoreRecord{id=Guid.NewGuid().ToString("N"),score=100+(int)mode,finishedAtUtc=DateTimeOffset.UtcNow.ToString("O"),mode=mode},"Ana",out _),Is.True);
                store=new ScoreHistoryStore(path);foreach(var mode in new[]{DifficultyMode.Normal,DifficultyMode.Relaxed,DifficultyMode.Challenge})Assert.That(store.BestFor(mode).score,Is.EqualTo(100+(int)mode));
                Assert.That(store.TryClear(DifficultyMode.Relaxed,out _),Is.True);store=new ScoreHistoryStore(path);Assert.That(store.Records.Count,Is.EqualTo(2));Assert.That(store.BestFor(DifficultyMode.Normal),Is.Not.Null);
                string bytes=File.ReadAllText(path);bytes=System.Text.RegularExpressions.Regex.Replace(bytes,",?\\s*\"mode\": 0","");File.WriteAllText(path,bytes);
                Assert.That(new ScoreHistoryStore(path).RecordsFor(DifficultyMode.Normal).Count,Is.EqualTo(1));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
