using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Cubotris.Gameplay
{
    public enum GameAction { Left, Right, Rotate, SoftDrop, HardDrop, Pause }
    [Serializable] public sealed class KeyBinding
    {
        public GameAction action;
        public Key primary, secondary;
        public KeyBinding(GameAction action, Key primary, Key secondary = Key.None)
        { this.action = action; this.primary = primary; this.secondary = secondary; }
        public KeyBinding Copy() => new KeyBinding(action, primary, secondary);
    }
    public static class ControlBindings
    {
        public const string PreferencesKey = "Cubotris.Controls.v1";
        [Serializable] private class Document { public KeyBinding[] bindings; }
        private static KeyBinding[] bindings;
        public static int Version { get; private set; }
        public static bool Capturing { get; set; }
        public static KeyBinding[] Current => (bindings ??= Read()).Select(b => b.Copy()).ToArray();
        public static KeyBinding[] Defaults() => new[]
        {
            new KeyBinding(GameAction.Left, Key.A, Key.LeftArrow), new KeyBinding(GameAction.Right, Key.D, Key.RightArrow),
            new KeyBinding(GameAction.Rotate, Key.W, Key.UpArrow), new KeyBinding(GameAction.SoftDrop, Key.S, Key.DownArrow),
            new KeyBinding(GameAction.HardDrop, Key.Space), new KeyBinding(GameAction.Pause, Key.P)
        };
        public static string Label(GameAction action) => action switch
        { GameAction.Left => "IZQUIERDA", GameAction.Right => "DERECHA", GameAction.Rotate => "GIRAR",
            GameAction.SoftDrop => "BAJAR", GameAction.HardDrop => "CAÍDA RÁPIDA", _ => "PAUSA (también ESC)" };
        public static string KeyName(Key key) => key == Key.None ? "—" : Keyboard.current?[key]?.displayName ?? key.ToString();
        public static bool Held(GameAction action) => Match(action, false);
        public static bool Pressed(GameAction action) => Match(action, true);
        private static bool Match(GameAction action, bool edge)
        {
            if (Capturing || Keyboard.current == null) return false;
            var binding = (bindings ??= Read()).First(b => b.action == action);
            return IsDown(binding.primary, edge) || IsDown(binding.secondary, edge);
        }
        private static bool IsDown(Key key, bool edge) => key != Key.None && (edge ? Keyboard.current[key].wasPressedThisFrame : Keyboard.current[key].isPressed);
        public static bool AnyGameplayHeld() => Enum.GetValues(typeof(GameAction)).Cast<GameAction>().Where(a => a != GameAction.Pause).Any(Held);
        public static bool TryRebind(GameAction action, bool secondary, Key key, out string error)
        {
            var next = Current; var target = next.FirstOrDefault(b => b.action == action);
            if (target == null) { error = "Acción desconocida."; return false; }
            if (secondary) target.secondary = key; else target.primary = key;
            if (!Validate(next, out error)) return false;
            bindings = next; Save(); return true;
        }
        public static bool Validate(KeyBinding[] value, out string error)
        {
            error = null;
            if (value == null || value.Length != Defaults().Length || value.Select(b => b?.action).Distinct().Count() != value.Length)
            { error = "Configuración incompleta."; return false; }
            var used = new System.Collections.Generic.HashSet<Key>();
            foreach (var b in value)
            {
                if (b == null || !Enum.IsDefined(typeof(GameAction), b.action) || b.primary == Key.None)
                { error = "Cada acción necesita una tecla principal."; return false; }
                foreach (var key in new[] { b.primary, b.secondary })
                {
                    if (key == Key.None) continue;
                    if (!Enum.IsDefined(typeof(Key), key) || key == Key.Escape || key == Key.Enter || key == Key.NumpadEnter || key == Key.Tab)
                    { error = "ESC, Enter y Tab se reservan para navegar por los menús."; return false; }
                    if (!used.Add(key)) { error = "Esa tecla ya está asignada. Libérala antes de reutilizarla."; return false; }
                }
            }
            return true;
        }
        public static void ResetDefaults() { bindings = Defaults(); Save(); }
        public static void Reload() { bindings = Read(); Version++; Capturing = false; }
        private static void Save()
        { PlayerPrefs.SetString(PreferencesKey, JsonUtility.ToJson(new Document { bindings = bindings })); PlayerPrefs.Save(); Version++; }
        private static KeyBinding[] Read()
        {
            try
            {
                var doc = JsonUtility.FromJson<Document>(PlayerPrefs.GetString(PreferencesKey, ""));
                if (doc != null && Validate(doc.bindings, out _)) return doc.bindings;
            }
            catch (Exception) { }
            return Defaults();
        }
    }
}
