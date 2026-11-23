using System.Collections;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest] public IEnumerator HoldKeyUpdatesEditablePreviewAndRespectsPauseAndRestart()
        {
            var game=Object.FindAnyObjectByType<GameController>();var first=game.Model.Active;
            yield return Tap(Key.C);Assert.That(game.Model.HeldPiece,Is.EqualTo(first.Type));
            var view=Object.FindAnyObjectByType<HoldView>();Assert.That(view.blocks.Length,Is.EqualTo(4));
            foreach(var block in view.blocks){ Assert.That(block.enabled,Is.True);Assert.That(block.color,Is.EqualTo(first.Color));Assert.That(block.sprite,Is.Not.Null); }
            int revision=game.Model.Revision;yield return Tap(Key.C);Assert.That(game.Model.Revision,Is.EqualTo(revision));
            yield return Tap(Key.Escape);yield return Tap(Key.C);Assert.That(game.Model.Revision,Is.EqualTo(revision));
            game.Restart();yield return null;Assert.That(game.Model.HeldPiece,Is.Null);Assert.That(game.Model.CanHold,Is.True);Assert.That(Time.timeScale,Is.EqualTo(1));
        }
    }
}
