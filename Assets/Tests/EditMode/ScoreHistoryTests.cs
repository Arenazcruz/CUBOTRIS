using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Cubotris.Tests
{
    public sealed class ScoreHistoryTests
    {
        private string directory, path;
        [SetUp] public void SetUp() { directory=Path.Combine(Path.GetTempPath(),"CubotrisHistory-"+Guid.NewGuid().ToString("N")); path=Path.Combine(directory,"records.json"); }
        [TearDown] public void TearDown() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
        private static ScoreRecord Run(int score=100,double seconds=125,string date="2026-10-19T10:00:00.0000000+00:00") => new ScoreRecord {id=Guid.NewGuid().ToString("N"),score=score,survivalSeconds=seconds,finishedAtUtc=date};
        [Test] public void MissingFileIsCreatedAndFinishedRecordsPersistInANewStore()
        {
            var store=new ScoreHistoryStore(path,800); Assert.That(File.Exists(path),Is.True); Assert.That(store.Records,Is.Empty);
            var run=Run(); Assert.That(store.TryAdd(run,"  Ana 7  ",out _),Is.True);
            var loaded=new ScoreHistoryStore(path,99999); var saved=loaded.Records.Single();
            Assert.That(saved.id,Is.EqualTo(run.id)); Assert.That(saved.playerName,Is.EqualTo("Ana 7"));
            Assert.That(saved.score,Is.EqualTo(100)); Assert.That(saved.survivalSeconds,Is.EqualTo(125)); Assert.That(saved.finishedAtUtc,Is.EqualTo(run.finishedAtUtc));
            Assert.That(loaded.HistoricalUnattributedScore,Is.EqualTo(800));
        }
        [TestCase("")] [TestCase("   ")] [TestCase("\nAna")] [TestCase("Ana\r\n7")] [TestCase("Ana\t7")]
        [TestCase("Ana!")] [TestCase("123456789012345678901")]
        public void InvalidNamesNeverWriteARecord(string name)
        {var store=new ScoreHistoryStore(path); string before=File.ReadAllText(path); Assert.That(store.TryAdd(Run(),name,out string error),Is.False); Assert.That(error,Is.Not.Empty); Assert.That(File.ReadAllText(path),Is.EqualTo(before));}
        [TestCase("Ángela 2026")] [TestCase("12345678901234567890")]
        public void LettersNumbersAndTwentyCharactersAreAccepted(string name)
        {Assert.That(new ScoreHistoryStore(path).TryAdd(Run(),name,out _),Is.True);}
        [Test] public void DuplicateRunIdIsRejectedEvenAfterReload()
        {var store=new ScoreHistoryStore(path);var run=Run();store.TryAdd(run,"Ana",out _);store=new ScoreHistoryStore(path);Assert.That(store.TryAdd(run,"Luis",out _),Is.False);Assert.That(store.Records.Count,Is.EqualTo(1));}
        [Test] public void OrderingUsesScoreTimeChronologicalDateThenId()
        {
            var store=new ScoreHistoryStore(path);
            store.TryAdd(Run(200,20),"Ganador",out _);store.TryAdd(Run(100,40),"Tiempo",out _);
            store.TryAdd(Run(100,30,"2026-10-19T11:00:00.0000000+02:00"),"Fecha temprana",out _);
            store.TryAdd(Run(100,30),"Fecha tardía",out _);
            CollectionAssert.AreEqual(new[]{"Ganador","Tiempo","Fecha temprana","Fecha tardía"},store.Records.Select(r=>r.playerName));
            CollectionAssert.AreEqual(store.Records.Select(r=>r.id),new ScoreHistoryStore(path).Records.Select(r=>r.id));
        }
        [Test] public void DeletionTargetsUniqueIdAndUpdatesBestWithoutRevivingLegacyValue()
        {
            var store=new ScoreHistoryStore(path,9000);var first=Run(300);var second=Run(100);
            store.TryAdd(first,"Mismo nombre",out _);store.TryAdd(second,"Mismo nombre",out _);
            Assert.That(store.TryDelete(first.id,out _),Is.True);Assert.That(store.Best.id,Is.EqualTo(second.id));
            Assert.That(store.TryClear(out _),Is.True);store=new ScoreHistoryStore(path,99999);
            Assert.That(store.Best,Is.Null);Assert.That(store.HistoricalUnattributedScore,Is.EqualTo(9000));
        }
        [Test] public void StorageHasNoTopTenLimitAndExposedRecordsCannotMutateIt()
        {
            var store=new ScoreHistoryStore(path);for(int i=0;i<37;i++)store.TryAdd(Run(i),"Jugador "+i,out _);
            var exposed=store.Records;exposed[0].score=-10;
            var loaded=new ScoreHistoryStore(path);Assert.That(loaded.Records.Count,Is.EqualTo(37));Assert.That(store.Best.score,Is.EqualTo(36));Assert.That(loaded.Best.score,Is.EqualTo(36));
        }
        [TestCase("")] [TestCase("broken {")] [TestCase("{}")]
        public void DamagedFilesArePreservedWithoutCrashing(string bytes)
        {
            Directory.CreateDirectory(directory);File.WriteAllText(path,bytes);
            LogAssert.Expect(LogType.Warning,new Regex("Historial dañado:"));
            var store=new ScoreHistoryStore(path,500);Assert.That(store.Records,Is.Empty);Assert.That(store.Notice,Is.Not.Empty);
            Assert.That(Directory.GetFiles(directory,"*.corrupt-*").Length,Is.EqualTo(1));
            Assert.That(File.ReadAllText(Directory.GetFiles(directory,"*.corrupt-*")[0]),Is.EqualTo(bytes));
            Assert.That(store.TryAdd(Run(),"Ana",out _),Is.True);
        }
        [Test] public void ValidBackupRecoversTheLastCommittedHistory()
        {
            var store=new ScoreHistoryStore(path);store.TryAdd(Run(100),"Ana",out _);store.TryAdd(Run(200),"Luis",out _);
            File.WriteAllText(path,"invalid");LogAssert.Expect(LogType.Warning,new Regex("Historial dañado:"));
            var restored=new ScoreHistoryStore(path);Assert.That(restored.Records.Count,Is.EqualTo(1));Assert.That(restored.Best.playerName,Is.EqualTo("Ana"));
            Assert.That(new ScoreHistoryStore(path).Best.playerName,Is.EqualTo("Ana"));
        }
        [Test] public void FailedWritesDoNotChangePreviousFileOrInMemoryRecords()
        {
            var store=new ScoreHistoryStore(path);var first=Run();store.TryAdd(first,"Ana",out _);string previous=File.ReadAllText(path);
            Directory.CreateDirectory(path+".tmp");
            LogAssert.Expect(LogType.Error,new Regex("No se pudo guardar el historial:"));Assert.That(store.TryAdd(Run(900),"Luis",out string error),Is.False);
            Assert.That(error,Does.Contain("No se pudo guardar"));Assert.That(store.Records.Count,Is.EqualTo(1));Assert.That(File.ReadAllText(path),Is.EqualTo(previous));
            LogAssert.Expect(LogType.Error,new Regex("No se pudo guardar el historial:"));Assert.That(store.TryDelete(first.id,out _),Is.False);
            LogAssert.Expect(LogType.Error,new Regex("No se pudo guardar el historial:"));Assert.That(store.TryClear(out _),Is.False);
            Assert.That(new ScoreHistoryStore(path).Records.Count,Is.EqualTo(1));
        }
        [Test] public void LegacyAndVolumePreferencesAreNeverDeletedOrOverwritten()
        {
            const string key="Cubotris.Audio.MusicVolumePercent"; bool had=PlayerPrefs.HasKey(key);int before=PlayerPrefs.GetInt(key);
            try{PlayerPrefs.SetInt(key,37);var store=new ScoreHistoryStore(path,1234);store.TryAdd(Run(),"Ana",out _);store.TryClear(out _);
                Assert.That(PlayerPrefs.GetInt(key),Is.EqualTo(37));Assert.That(store.HistoricalUnattributedScore,Is.EqualTo(1234));}
            finally{if(had)PlayerPrefs.SetInt(key,before);else PlayerPrefs.DeleteKey(key);}
        }
    }
}
