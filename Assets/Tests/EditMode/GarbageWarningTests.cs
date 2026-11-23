using Cubotris.Gameplay;
using NUnit.Framework;

namespace Cubotris.Tests
{
    public class GarbageWarningTests
    {
        [TestCase(0, 0, 0, GarbageWarningLevel.None)]
        [TestCase(1, 6, 0, GarbageWarningLevel.Countdown)]
        [TestCase(1, 5, 0, GarbageWarningLevel.Imminent)]
        [TestCase(3, 1, 0, GarbageWarningLevel.Imminent)]
        [TestCase(2, 29, 4, GarbageWarningLevel.Pending)]
        public void StateExplainsTheUpcomingPenalty(int face, double seconds, int pending, GarbageWarningLevel expected)
        { Assert.That(GarbageWarning.Level(face, seconds, pending), Is.EqualTo(expected)); }

        [Test] public void PendingMessageExplainsWhenRowsArrive()
        { Assert.That(GarbageWarning.Message(2, 29.2, 4), Does.Contain("+4 AL FIJAR")); }

        [Test] public void WarningsDoNotAdvanceTheSessionOrApplyGarbage()
        {
            var session = new CubotrisSession(); session.Game.HardDrop(); session.AdvanceActiveTime(26);
            int revision = session.Game.Revision;
            for (int i = 0; i < 20; i++) Assert.That(GarbageWarning.Level(1, session.SecondsUntilGarbage(1), 0), Is.EqualTo(GarbageWarningLevel.Imminent));
            Assert.That(session.SurvivalSeconds, Is.EqualTo(26));
            Assert.That(session.Game.Revision, Is.EqualTo(revision));
            Assert.That(session.PendingGarbageRows(1), Is.Zero);
        }
    }
}
