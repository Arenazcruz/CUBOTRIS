using System;
using System.IO;
using Cubotris.Gameplay;
using NUnit.Framework;
namespace Cubotris.Tests
{
    public sealed class StatisticsTests
    {
        private string directory,path;
        [SetUp] public void Before(){directory=Path.Combine(Path.GetTempPath(),"CubotrisStats-"+Guid.NewGuid().ToString("N"));path=Path.Combine(directory,"statistics.json");}
        [TearDown] public void After(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        private static RunStatistic Run(double seconds,int lines,int face,RunOutcome outcome=RunOutcome.Completed,bool practice=false)
        {
            var r=new RunStatistic{id=Guid.NewGuid().ToString("N"),seconds=seconds,lines=lines,score=lines*100,face=face,outcome=outcome,practice=practice,faceSeconds=new double[4],faceLines=new int[4]};r.faceSeconds[face]=seconds;r.faceLines[face]=lines;return r;
        }
        [Test] public void SaveResumeAndRepeatedCompletionCountTheRunOnlyOnceAcrossExecutions()
        {
            var store=new StatisticsStore(path);var run=Run(20,1,1,RunOutcome.Started);Assert.That(store.TryRecord(run,out _),Is.True);
            run.outcome=RunOutcome.Suspended;store.TryRecord(run,out _);run.outcome=RunOutcome.Started;new StatisticsStore(path).TryRecord(run,out _);
            store=new StatisticsStore(path);run.outcome=RunOutcome.Completed;store.TryRecord(run,out _);run.lines=run.faceLines[1]=4;store.TryRecord(run,out _);
            var loaded=new StatisticsStore(path);var summary=loaded.Summarize();Assert.That(summary.Played,Is.EqualTo(1));Assert.That(summary.Completed,Is.EqualTo(1));Assert.That(summary.Lines,Is.EqualTo(1));Assert.That(summary.AverageCompletedSeconds,Is.EqualTo(20));Assert.That(loaded.IsFinal(run.id),Is.True);
        }
        [Test] public void AverageAndHardestFaceUseCompletedRunsAndPracticeHasSeparateTotals()
        {
            var store=new StatisticsStore(path);store.TryRecord(Run(60,4,2),out _);store.TryRecord(Run(120,8,2),out _);store.TryRecord(Run(300,10,1,RunOutcome.Abandoned),out _);store.TryRecord(Run(900,30,3,practice:true),out _);
            var s=store.Summarize();Assert.That(s.Played,Is.EqualTo(3));Assert.That(s.Completed,Is.EqualTo(2));Assert.That(s.Abandoned,Is.EqualTo(1));Assert.That(s.Lines,Is.EqualTo(22));Assert.That(s.Seconds,Is.EqualTo(480));Assert.That(s.AverageCompletedSeconds,Is.EqualTo(90));Assert.That(s.HardestFaces,Is.EqualTo("CARA 3"));Assert.That(s.FaceLosses[2],Is.EqualTo(2));
            Assert.That(store.Summarize(true).Played,Is.EqualTo(1));Assert.That(store.Summarize(true).Lines,Is.EqualTo(30));
        }
        [Test] public void NoLossAndTiesAreReportedWithoutInventingAMostDifficultFace()
        {var store=new StatisticsStore(path);Assert.That(store.Summarize().HardestFaces,Is.EqualTo("Sin partidas terminadas"));store.TryRecord(Run(10,0,0),out _);store.TryRecord(Run(10,0,3),out _);Assert.That(store.Summarize().HardestFaces,Is.EqualTo("CARA 1, CARA 4"));}
        [Test] public void ClearingLeaderboardCannotEraseStatistics()
        {var store=new StatisticsStore(path);store.TryRecord(Run(10,1,0),out _);var scores=new ScoreHistoryStore(Path.Combine(directory,"records.json"));scores.TryClear(out _);Assert.That(new StatisticsStore(path).Summarize().Played,Is.EqualTo(1));}
        [Test] public void CorruptedStatisticsArePreservedAndDoNotGetOverwritten()
        {Directory.CreateDirectory(directory);File.WriteAllText(path,"damaged");var store=new StatisticsStore(path);Assert.That(store.Notice,Is.Not.Empty);Assert.That(store.TryRecord(Run(10,1,0),out _),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo("damaged"));}
        [Test] public void FailedWriteDoesNotChangeTheFileOrTotalsAndCopiesCannotMutateStoredData()
        {
            var store=new StatisticsStore(path);store.TryRecord(Run(10,1,0),out _);string old=File.ReadAllText(path);Directory.CreateDirectory(path+".tmp");Assert.That(store.TryRecord(Run(20,2,1),out _),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(old));Assert.That(store.Summarize().Played,Is.EqualTo(1));
            var copy=store.Runs[0];copy.faceLines[0]=999;Assert.That(store.Summarize().FaceLines[0],Is.EqualTo(1));
        }
    }
}
