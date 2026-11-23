using System;

namespace Cubotris.Gameplay
{
    public interface IPieceSource
    {
        TetrominoType Peek();
        TetrominoType Next();
    }

    public sealed partial class SevenBag : IPieceSource
    {
        private readonly StatefulRandom random;
        private readonly TetrominoType[] bag = new TetrominoType[7];
        private int cursor = 7;

        public SevenBag(int? seed = null)
        {
            random = new StatefulRandom(seed);
        }

        public TetrominoType Next()
        {
            var next = Peek();
            cursor++;
            return next;
        }

        public TetrominoType Peek()
        {
            if (cursor == bag.Length)
            {
                for (int i = 0; i < bag.Length; i++) bag[i] = (TetrominoType)i;
                for (int i = bag.Length - 1; i > 0; i--)
                {
                    int other = random.Next(i + 1);
                    (bag[i], bag[other]) = (bag[other], bag[i]);
                }
                cursor = 0;
            }
            return bag[cursor];
        }
    }
}
