using System;

namespace Cubotris.Gameplay
{
    public enum GarbageWarningLevel { None, Countdown, Imminent, Pending }

    public static class GarbageWarning
    {
        public static GarbageWarningLevel Level(int face, double seconds, int pending, int warningSeconds = 5)
        {
            if (face == 0 || double.IsInfinity(seconds)) return GarbageWarningLevel.None;
            if (pending > 0) return GarbageWarningLevel.Pending;
            return seconds <= Math.Max(1, warningSeconds) ? GarbageWarningLevel.Imminent : GarbageWarningLevel.Countdown;
        }

        public static string Message(int face, double seconds, int pending, int warningSeconds = 5)
        {
            int remaining = double.IsInfinity(seconds) ? 0 : (int)Math.Ceiling(Math.Max(0, seconds));
            return Level(face, seconds, pending, warningSeconds) switch
            {
                GarbageWarningLevel.None => "SIN\nPENALIZACIÓN",
                GarbageWarningLevel.Pending => $"¡+{pending} AL FIJAR!\n+{face} EN {remaining:00} s",
                GarbageWarningLevel.Imminent => $"¡+{face} {(face == 1 ? "FILA" : "FILAS")}!\nEN {remaining:00} s",
                _ => $"+{face} {(face == 1 ? "FILA" : "FILAS")}\nEN {remaining:00} s"
            };
        }
    }
}
