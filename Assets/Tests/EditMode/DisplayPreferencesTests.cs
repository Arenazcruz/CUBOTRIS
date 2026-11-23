using Cubotris.Gameplay;
using NUnit.Framework;
namespace Cubotris.Tests
{
    public class DisplayPreferencesTests
    {
        [Test] public void TimeoutRestoresOriginalWithoutSaving()
        {
            var current=new DisplayChoice(1920,1080,true);var next=new DisplayChoice(1280,720,false);DisplayChoice applied=default;int saved=0;
            var preview=new DisplayPreview(c=>applied=c,c=>saved++);preview.Begin(current,next);
            Assert.That(applied.width,Is.EqualTo(1280));preview.Tick(11);
            Assert.That(preview.IsActive,Is.False);Assert.That(applied.width,Is.EqualTo(1920));Assert.That(saved,Is.Zero);
        }
        [Test] public void ConfirmSavesExactlyOnceAndCancelDoesNotUndoIt()
        {
            int applied=0,saved=0;var preview=new DisplayPreview(c=>applied++,c=>saved++);
            preview.Begin(new DisplayChoice(1920,1080,true),new DisplayChoice(1280,720,false));preview.Confirm();preview.Confirm();preview.Cancel();
            Assert.That(saved,Is.EqualTo(1));Assert.That(applied,Is.EqualTo(1));
        }
        [Test] public void CancelRestoresOriginalEvenBeforeTimeout()
        {
            DisplayChoice applied=default;var preview=new DisplayPreview(c=>applied=c,c=>Assert.Fail());
            preview.Begin(new DisplayChoice(1280,720,false),new DisplayChoice(1920,1080,true));preview.Cancel();
            Assert.That(applied.fullscreen,Is.False);Assert.That(preview.IsActive,Is.False);
        }
        [TestCase(0,0)] [TestCase(100,100)] [TestCase(20000,1080)]
        public void RejectsInvalidDimensions(int width,int height){Assert.That(new DisplayChoice(width,height,false).IsValid,Is.False);}
    }
}
