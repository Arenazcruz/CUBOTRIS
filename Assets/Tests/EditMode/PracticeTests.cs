using System;
using System.IO;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
namespace Cubotris.Tests
{
    public class PracticeTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void SelectedBoardStaysActiveAndOtherBoardsStayEmpty(int face)
        {
            var session=new CubotrisSession(mode:DifficultyMode.Relaxed,practiceFace:face);
            for(int i=0;i<4;i++)session.Game.HardDrop();
            Assert.That(session.IsPractice,Is.True);Assert.That(session.ActiveFace,Is.EqualTo(face));Assert.That(session.Game.Board,Is.SameAs(session.Faces[face]));
            for(int other=0;other<4;other++)if(other!=face)for(int x=0;x<10;x++)for(int y=0;y<20;y++)Assert.That(session.Faces[other][x,y],Is.Null);
        }
        [Test] public void PracticeKeepsChosenGarbageRule()
        {
            var session=new CubotrisSession(mode:DifficultyMode.Challenge,practiceFace:3);session.AdvanceActiveTime(20);
            Assert.That(session.PendingGarbageRows(3),Is.EqualTo(3));session.Game.HardDrop();Assert.That(session.ActiveFace,Is.EqualTo(3));Assert.That(session.PendingGarbageRows(3),Is.Zero);
        }
        [Test] public void PracticeCannotBeInsertedIntoCompetitiveHistory()
        {
            string directory=Path.Combine(Path.GetTempPath(),"CubotrisPractice-"+Guid.NewGuid().ToString("N"));
            try{var store=new ScoreHistoryStore(Path.Combine(directory,"records.json"));Assert.That(store.TryAdd(new ScoreRecord{id=Guid.NewGuid().ToString("N"),practice=true,score=99999,finishedAtUtc=DateTimeOffset.UtcNow.ToString("O")},"Ana",out _),Is.False);Assert.That(store.Records,Is.Empty);}
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
