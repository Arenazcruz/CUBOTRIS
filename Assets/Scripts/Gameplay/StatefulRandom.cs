using System;
namespace Cubotris.Gameplay
{
    // Explicit state makes future bag, color and garbage draws reproducible after loading.
    public sealed class StatefulRandom
    {
        public uint State { get; private set; }
        public StatefulRandom(int? seed=null){State=unchecked((uint)(seed??Guid.NewGuid().GetHashCode()));if(State==0)State=0x9e3779b9;}
        public static StatefulRandom Restore(uint state)
        {if(state==0)throw new ArgumentOutOfRangeException(nameof(state));return new StatefulRandom(1){State=state};}
        private uint Draw(){uint value=State;value^=value<<13;value^=value>>17;value^=value<<5;return State=value;}
        public int Next(int exclusiveMaximum)
        {
            if(exclusiveMaximum<=0)throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            uint bound=(uint)exclusiveMaximum,limit=uint.MaxValue-uint.MaxValue%bound,value;
            do{value=Draw()-1;}while(value>=limit);
            return (int)(value%bound);
        }
    }
}
