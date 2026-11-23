using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Cubotris.Tests
{
    public class ControlBindingsTests
    {
        private string previous; private bool had;
        [SetUp] public void Before() { had = PlayerPrefs.HasKey(ControlBindings.PreferencesKey); previous = PlayerPrefs.GetString(ControlBindings.PreferencesKey); ControlBindings.ResetDefaults(); }
        [TearDown] public void After() { if (had) PlayerPrefs.SetString(ControlBindings.PreferencesKey, previous); else PlayerPrefs.DeleteKey(ControlBindings.PreferencesKey); PlayerPrefs.Save(); ControlBindings.Reload(); }
        [Test] public void DefaultsHaveNoConflicts() { Assert.That(ControlBindings.Validate(ControlBindings.Defaults(), out _), Is.True); }
        [Test] public void RejectsConflictingAndReservedKeysWithoutChangingTheBinding()
        {
            Assert.That(ControlBindings.TryRebind(GameAction.Left, false, Key.D, out _), Is.False);
            Assert.That(ControlBindings.TryRebind(GameAction.Left, false, Key.Escape, out _), Is.False);
            Assert.That(ControlBindings.Current[0].primary, Is.EqualTo(Key.A));
        }
        [Test] public void SavesAndReloadsCustomKeysAndRestoresDefaults()
        {
            Assert.That(ControlBindings.TryRebind(GameAction.Left, false, Key.J, out _), Is.True);
            ControlBindings.Reload(); Assert.That(ControlBindings.Current[0].primary, Is.EqualTo(Key.J));
            ControlBindings.ResetDefaults(); Assert.That(ControlBindings.Current[0].primary, Is.EqualTo(Key.A));
        }
        [Test] public void SecondaryCanBeRemovedButPrimaryCannot()
        {
            Assert.That(ControlBindings.TryRebind(GameAction.Left, true, Key.None, out _), Is.True);
            Assert.That(ControlBindings.TryRebind(GameAction.Left, false, Key.None, out _), Is.False);
        }
        [Test] public void InvalidSavedDataFallsBackToUsableDefaults()
        {
            PlayerPrefs.SetString(ControlBindings.PreferencesKey, "invalid"); ControlBindings.Reload();
            Assert.That(ControlBindings.Validate(ControlBindings.Current, out _), Is.True);
        }
        [Test] public void PreviousSixActionsKeepCustomKeysWhenHoldIsAdded()
        {
            var defaults=ControlBindings.Defaults();defaults[0].primary=Key.C;
            string json="{\"bindings\":["+string.Join(",",System.Linq.Enumerable.Select(System.Linq.Enumerable.Take(defaults,6),b=>JsonUtility.ToJson(b)))+"]}";
            PlayerPrefs.SetString(ControlBindings.PreferencesKey,json);ControlBindings.Reload();
            Assert.That(ControlBindings.Current[0].primary,Is.EqualTo(Key.C));
            Assert.That(ControlBindings.Current[6].primary,Is.Not.EqualTo(Key.C));
            Assert.That(ControlBindings.Validate(ControlBindings.Current,out _),Is.True);
        }
    }
}
