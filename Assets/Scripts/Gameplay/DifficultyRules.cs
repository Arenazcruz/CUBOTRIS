using System;
using UnityEngine;
namespace Cubotris.Gameplay
{
    public enum DifficultyMode { Normal, Relaxed, Challenge }
    public static class GameSelection
    {
        public const string ModeKey="Cubotris.Difficulty.v1";
        public static int PracticeFace { get; set; } = -1;
        public static DifficultyMode Mode
        {
            get { int value=PlayerPrefs.GetInt(ModeKey);return Enum.IsDefined(typeof(DifficultyMode),value)?(DifficultyMode)value:DifficultyMode.Normal; }
            set { if(!Enum.IsDefined(typeof(DifficultyMode),value))throw new ArgumentOutOfRangeException(nameof(value));PlayerPrefs.SetInt(ModeKey,(int)value);PlayerPrefs.Save(); }
        }
    }
    public static class DifficultyRules
    {
        public static string Label(DifficultyMode mode)=>mode switch { DifficultyMode.Relaxed=>"RELAJADO",DifficultyMode.Challenge=>"DESAFÍO",_=>"NORMAL" };
        public static string Description(DifficultyMode mode)=>mode switch
        { DifficultyMode.Relaxed=>"Caída más lenta. Sin filas de basura en ninguna cara.",DifficultyMode.Challenge=>"Caída más rápida. Basura cada 20 segundos activos por cara.",_=>"Ritmo original. Basura cada 30 segundos activos por cara." };
        public static FallSettings Fall(DifficultyMode mode,FallSettings original)
        {
            original??=new FallSettings();float factor=mode==DifficultyMode.Relaxed?1.4f:mode==DifficultyMode.Challenge?.7f:1;
            return new FallSettings { initialInterval=original.initialInterval*factor,minimumInterval=original.minimumInterval*factor,
                softDropInterval=original.softDropInterval,speedMultiplier=original.speedMultiplier,linesPerStep=original.linesPerStep };
        }
        public static double GarbageInterval(DifficultyMode mode,double interval)=>mode==DifficultyMode.Relaxed?double.PositiveInfinity:mode==DifficultyMode.Challenge?interval*(2.0/3):interval;
    }
}
