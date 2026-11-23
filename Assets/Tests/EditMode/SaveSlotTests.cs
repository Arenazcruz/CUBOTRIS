using System;
using System.IO;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
namespace Cubotris.Tests
{
    public sealed class SaveSlotTests
    {
        private string directory,path;
        [SetUp] public void Before(){directory=Path.Combine(Path.GetTempPath(),"CubotrisSave-"+Guid.NewGuid().ToString("N"));path=Path.Combine(directory,"continue.json");}
        [TearDown] public void After(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        private static void Clear(CubotrisSession s){s.Game.Board.TryLock(TetrominoType.I,Enumerable.Range(0,10).Select(x=>new Vector2Int(x,0)).ToArray());s.Game.HardDrop();}
        private static string State(CubotrisSession s)=>JsonUtility.ToJson(s.Capture());
        [Test] public void FullRoundTripKeepsBoardsHoldClockRewardsGravityAndFutureRandomDraws()
        {
            var session=new CubotrisSession(source:new SevenBag(73),garbageSeed:51);for(int i=0;i<4;i++)Clear(session);
            session.Game.Hold();session.Game.Move(-1);session.Game.RotateClockwise();session.Game.Tick(.27f);session.AdvanceActiveTime(4.25);
            var store=new SaveSlotStore(path);Assert.That(store.TrySave(session,out var error),Is.True,error);
            Assert.That(new SaveSlotStore(path).TryLoad(out var loaded,out error),Is.True,error);Assert.That(State(loaded),Is.EqualTo(State(session)));
            for(int i=0;i<16 && !session.Game.IsGameOver;i++)
            {
                Assert.That(loaded.Game.NextPiece,Is.EqualTo(session.Game.NextPiece));Assert.That(loaded.Game.NextPieceColor,Is.EqualTo(session.Game.NextPieceColor));
                session.AdvanceActiveTime(9.75);loaded.AdvanceActiveTime(9.75);session.Game.HardDrop();loaded.Game.HardDrop();
                Assert.That(loaded.Game.IsGameOver,Is.EqualTo(session.Game.IsGameOver));if(!session.Game.IsGameOver)Assert.That(State(loaded),Is.EqualTo(State(session)));
            }
        }
        [Test] public void PendingGarbageAndItsHolesAreIdenticalAfterContinuing()
        {
            var s=new CubotrisSession(source:new SevenBag(12),garbageSeed:4,mode:DifficultyMode.Challenge);s.Game.HardDrop();s.AdvanceActiveTime(41.5);
            Assert.That(s.PendingGarbageRows(1),Is.EqualTo(2));var copy=CubotrisSession.Restore(JsonUtility.FromJson<SessionSnapshot>(JsonUtility.ToJson(s.Capture())));
            s.Game.HardDrop();copy.Game.HardDrop();Assert.That(State(copy),Is.EqualTo(State(s)));
        }
        [Test] public void SavedGravityDoesNotGrantAnExtraFallInterval()
        {
            var s=new CubotrisSession();s.Game.Tick(.6f);var copy=CubotrisSession.Restore(s.Capture());
            s.Game.Tick(.21f);copy.Game.Tick(.21f);Assert.That(copy.Game.Active.Position,Is.EqualTo(s.Game.Active.Position));Assert.That(copy.Game.Active.Position.y,Is.EqualTo(17));
        }
        [Test] public void PracticeModeAndFaceSurviveLoad()
        {var s=new CubotrisSession(mode:DifficultyMode.Relaxed,practiceFace:3);s.Game.Hold();var copy=CubotrisSession.Restore(s.Capture());Assert.That(copy.IsPractice,Is.True);Assert.That(copy.ActiveFace,Is.EqualTo(3));copy.Game.HardDrop();Assert.That(copy.ActiveFace,Is.EqualTo(3));}
        [TestCase("invalid")] [TestCase("{}")] [TestCase("{\"version\":999,\"format\":\"CUBOTRIS_SAVE\",\"status\":\"active\"}")]
        public void CorruptOrFutureFilesAreRejectedAndPreserved(string bytes)
        {Directory.CreateDirectory(directory);File.WriteAllText(path,bytes);Assert.That(new SaveSlotStore(path).TryLoad(out _,out var error),Is.False);Assert.That(error,Is.Not.Empty);Assert.That(File.ReadAllText(path),Is.EqualTo(bytes));}
        [Test] public void InvalidBoardAndScoreAreRejectedBeforeChangingTheExistingSave()
        {
            var s=new CubotrisSession();var store=new SaveSlotStore(path);store.TrySave(s,out _);var snapshot=s.Capture();snapshot.boards[0].cells=new int[3];Assert.Throws<InvalidDataException>(()=>CubotrisSession.Restore(snapshot));
            snapshot=s.Capture();snapshot.game.score=99999;Assert.Throws<InvalidDataException>(()=>CubotrisSession.Restore(snapshot));
            snapshot=s.Capture();var cell=s.Game.Active.Cells[0];snapshot.boards[0].cells[cell.y*10+cell.x]=0;Assert.Throws<InvalidDataException>(()=>CubotrisSession.Restore(snapshot));
            Assert.That(store.TryLoad(out _,out _),Is.True);
        }
        [Test] public void FailedWriteKeepsPreviousSaveAndSuccessfulReplacementCreatesBackup()
        {
            var s=new CubotrisSession();var store=new SaveSlotStore(path);Assert.That(store.TrySave(s,out _),Is.True);string old=File.ReadAllText(path);
            Directory.CreateDirectory(path+".tmp");s.Game.Hold();Assert.That(store.TrySave(s,out _),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(old));
            Directory.Delete(path+".tmp");Assert.That(store.TrySave(s,out _),Is.True);Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(old));
        }
        [Test] public void FinishingOnlyClosesTheMatchingRunAndDoesNotReviveItsBackup()
        {
            var s=new CubotrisSession();var store=new SaveSlotStore(path);store.TrySave(s,out _);
            Assert.That(store.TryClose(Guid.NewGuid().ToString("N"),out _),Is.True);Assert.That(store.HasSave,Is.True);
            Assert.That(store.TryClose(s.RunId,out _),Is.True);Assert.That(File.Exists(path+".bak"),Is.True);Assert.That(new SaveSlotStore(path).HasSave,Is.False);Assert.That(store.TryLoad(out _,out _),Is.False);
        }
        [Test] public void SavedBagBoundaryKeepsTheNextSequence()
        {
            var s=new CubotrisSession(source:new SevenBag(1));var snapshot=s.Capture();snapshot.game.bag.cursor=7;
            var restored=CubotrisSession.Restore(snapshot);var reference=CubotrisSession.Restore(snapshot);
            for(int i=0;i<20;i++){Assert.That(restored.Game.NextPiece,Is.EqualTo(reference.Game.NextPiece));restored.Game.HardDrop();reference.Game.HardDrop();}
        }
    }
}
