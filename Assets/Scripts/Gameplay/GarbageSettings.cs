using System;
using UnityEngine;

namespace Cubotris.Gameplay
{
    [Serializable]
    public sealed class GarbageSettings
    {
        [Min(0.1f)] public float face2Interval = 30;
        [Min(0.1f)] public float face3Interval = 30;
        [Min(0.1f)] public float face4Interval = 30;

        public double IntervalFor(int face) => face switch
        {
            1 => Math.Max(0.1, face2Interval),
            2 => Math.Max(0.1, face3Interval),
            3 => Math.Max(0.1, face4Interval),
            _ => double.PositiveInfinity
        };
    }
}
