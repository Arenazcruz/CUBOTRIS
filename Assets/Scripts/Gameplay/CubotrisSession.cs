using System;
using System.Collections.Generic;

namespace Cubotris.Gameplay
{
    /// <summary>Four persistent boards, one active piece, one bag and shared progression.</summary>
    public sealed class CubotrisSession
    {
        public const int FaceCount = 4;
        public IReadOnlyList<BoardState> Faces { get; }
        public int ActiveFace { get; private set; }
        public TetrisGame Game { get; }

        public CubotrisSession(IPieceSource source = null, FallSettings settings = null)
        {
            var boards = new BoardState[FaceCount];
            for (int i = 0; i < boards.Length; i++) boards[i] = new BoardState();
            Faces = Array.AsReadOnly(boards);
            Game = new TetrisGame(boards[0], source, settings, AdvanceFace);
        }

        private BoardState AdvanceFace()
        {
            ActiveFace = (ActiveFace + 1) % FaceCount;
            return Faces[ActiveFace];
        }
    }
}
