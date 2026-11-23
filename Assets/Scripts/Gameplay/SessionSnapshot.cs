using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Cubotris.Gameplay
{
    [Serializable] public sealed class BoardSnapshot {public int[] cells;public Color[] colors;}
    [Serializable] public sealed class BagSnapshot {public TetrominoType[] pieces;public int cursor;public uint random;}
    [Serializable] public sealed class PieceSnapshot {public TetrominoType type;public int rotation,x,y;public Color color;}
    [Serializable] public sealed class TetrisSnapshot
    {
        public FallSettings settings;
        public BagSnapshot bag;
        public PieceSnapshot active;
        public int held=-1,lastColor;
        public Color heldColor,nextColor;
        public bool holdUsed,softDrop;
        public uint colorRandom;
        public float fallTime;
        public int score,lineScore,lines,piecesLocked,lastClear,combo,maxCombo,lastComboBonus,comboScore;
    }
    [Serializable] public sealed class SessionSnapshot
    {
        public string format="CUBOTRIS_SAVE",status="active",runId;
        public int version=1,activeFace,cycleMask,rotations,successfulRotations,lastRotationBonus;
        public DifficultyMode mode;
        public bool practice;
        public double seconds;
        public double[] elapsed,intervals,faceTime;
        public int[] pending,faceLines;
        public uint garbageRandom;
        public BoardSnapshot[] boards;
        public TetrisSnapshot game;
    }
    internal static class SnapshotCheck
    {
        internal static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
        internal static bool Number(double value,double min=0,double max=1e9)=>!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=min&&value<=max;
        internal static bool Color(Color value)=>Number(value.r,0,1)&&Number(value.g,0,1)&&Number(value.b,0,1)&&Number(value.a,0,1);
        internal static void Validate(SessionSnapshot s)
        {
            Require(s!=null && s.format=="CUBOTRIS_SAVE" && s.version==1 && s.status=="active","Formato de partida incompatible.");
            Require(Guid.TryParse(s.runId,out _) && Enum.IsDefined(typeof(DifficultyMode),s.mode) && s.activeFace>=0 && s.activeFace<4,"Identidad o modo inválido.");
            Require(s.boards?.Length==4 && s.elapsed?.Length==4 && s.intervals?.Length==4 && s.pending?.Length==4 && s.faceTime?.Length==4 && s.faceLines?.Length==4,"Faltan tableros o temporizadores.");
            Require(Number(s.seconds) && s.garbageRandom!=0,"Tiempo o generador inválido.");
            for(int i=0;i<4;i++)
            {
                bool disabled=i==0 || s.mode==DifficultyMode.Relaxed;
                Require(Number(s.elapsed[i]) && Number(s.intervals[i]) && (disabled?s.intervals[i]==0 && s.elapsed[i]==0:s.intervals[i]>=.01 && s.elapsed[i]<s.intervals[i]),"Temporizador de basura inválido.");
                Require(s.pending[i]>=0 && (!disabled || s.pending[i]==0) && Number(s.faceTime[i]) && s.faceLines[i]>=0,"Progreso de cara inválido.");
                var b=s.boards[i];Require(b!=null && b.cells?.Length==200 && b.colors?.Length==200,"Tablero incompleto.");
                for(int j=0;j<200;j++)Require(b.cells[j]>=-1 && b.cells[j]<=6 && Color(b.colors[j]),"Celda inválida.");
            }
            Require(Math.Abs(s.faceTime.Sum()-s.seconds)<.01,"El tiempo total no coincide.");
            Require(s.cycleMask>=0 && s.cycleMask<=15 && s.rotations>=0 && s.successfulRotations>=0 && s.successfulRotations<=s.rotations && (s.lastRotationBonus==0 || s.lastRotationBonus==CubotrisSession.RotationBonus),"Objetivo de vuelta inválido.");
            Require(!s.practice || (s.cycleMask==0 && s.rotations==0 && s.successfulRotations==0),"La práctica no tiene vueltas competitivas.");
            var g=s.game;Require(g!=null && g.settings!=null && g.bag!=null && g.active!=null,"Falta el estado de la pieza.");
            var f=g.settings;Require(Number(f.initialInterval,.01,60) && Number(f.minimumInterval,.01,60) && Number(f.softDropInterval,.01,60) && Number(f.speedMultiplier,.1,1) && f.linesPerStep>0,"Velocidad inválida.");
            Require(g.bag.pieces?.Length==7 && g.bag.cursor>=0 && g.bag.cursor<=7 && g.bag.random!=0 && g.bag.pieces.All(t=>Enum.IsDefined(typeof(TetrominoType),t)) && g.bag.pieces.Distinct().Count()==7,"Bolsa de piezas inválida.");
            Require(g.colorRandom!=0 && g.lastColor>=0 && g.lastColor<7 && Color(g.nextColor) && Color(g.heldColor) && Number(g.fallTime,0,60),"Color o gravedad inválidos.");
            Require(g.held>=-1 && g.held<=6 && (!g.holdUsed || g.held>=0),"Reserva inválida.");
            Require(Enum.IsDefined(typeof(TetrominoType),g.active.type) && g.active.rotation>=0 && g.active.rotation<4 && g.active.x>=-3 && g.active.x<=12 && g.active.y>=-3 && g.active.y<=22 && Color(g.active.color),"Pieza activa inválida.");
            Require(g.score>=0 && g.lineScore>=0 && g.lines>=0 && g.piecesLocked>=0 && g.lastClear>=0 && g.lastClear<=4 && g.combo>=0 && g.maxCombo>=g.combo && g.maxCombo<=g.piecesLocked && g.comboScore>=0,"Contadores inválidos.");
            Require(g.lastComboBonus==(g.combo>1?50L*(g.combo-1):0) && g.score==(long)g.lineScore+g.comboScore+(long)s.successfulRotations*CubotrisSession.RotationBonus,"Puntuación inconsistente.");
            Require(s.faceLines.Sum(v=>(long)v)==g.lines && g.lines<=4L*g.piecesLocked,"Líneas inconsistentes.");
            Require(s.practice || (s.rotations==g.piecesLocked/4 && s.activeFace==g.piecesLocked%4 && s.cycleMask<(1<<s.activeFace)),"Vuelta inconsistente.");
        }
    }
    public sealed partial class BoardState
    {
        internal BoardSnapshot Capture()
        {
            if(Width!=10 || Height!=20)throw new InvalidOperationException("Solo se guardan tableros de 10×20.");
            var result=new BoardSnapshot{cells=new int[200],colors=new Color[200]};
            for(int y=0;y<20;y++)for(int x=0;x<10;x++){int i=y*10+x;result.cells[i]=cells[x,y].HasValue?(int)cells[x,y].Value:-1;result.colors[i]=blockColors[x,y]??UnityEngine.Color.clear;}return result;
        }
        internal static BoardState Restore(BoardSnapshot snapshot)
        {
            var board=new BoardState();for(int y=0;y<20;y++)for(int x=0;x<10;x++){int i=y*10+x;if(snapshot.cells[i]<0)continue;board.cells[x,y]=(TetrominoType)snapshot.cells[i];board.blockColors[x,y]=snapshot.colors[i];}board.Version=1;return board;
        }
    }
    public sealed partial class SevenBag
    {
        internal BagSnapshot Capture()=>new BagSnapshot{pieces=(TetrominoType[])bag.Clone(),cursor=cursor,random=random.State};
        internal SevenBag(BagSnapshot snapshot){random=StatefulRandom.Restore(snapshot.random);Array.Copy(snapshot.pieces,bag,7);cursor=snapshot.cursor;}
    }
    public sealed partial class TetrisGame
    {
        internal TetrisSnapshot Capture()
        {
            if(IsGameOver || !HasActivePiece || !(source is SevenBag bagSource))throw new InvalidOperationException("La partida no se puede guardar.");
            return new TetrisSnapshot{settings=JsonUtility.FromJson<FallSettings>(JsonUtility.ToJson(settings)),bag=bagSource.Capture(),active=new PieceSnapshot{type=Active.Type,rotation=Active.Rotation,x=Active.Position.x,y=Active.Position.y,color=Active.Color},
                held=HeldPiece.HasValue?(int)HeldPiece.Value:-1,heldColor=HeldPieceColor,holdUsed=HoldUsed,nextColor=nextPieceColor,lastColor=lastPieceColorIndex,colorRandom=colorRandom.State,
                fallTime=fallTime,softDrop=wasSoftDropping,score=Score,lineScore=LineScore,lines=LinesCleared,piecesLocked=PiecesLocked,lastClear=LastClearCount,combo=ComboChain,maxCombo=MaxCombo,lastComboBonus=LastComboBonus,comboScore=ComboBonusScore};
        }
        internal TetrisGame(TetrisSnapshot s,BoardState board,Func<BoardState> next,Func<bool> after)
        {
            Board=board;source=new SevenBag(s.bag);settings=JsonUtility.FromJson<FallSettings>(JsonUtility.ToJson(s.settings));nextBoard=next;afterLock=after;
            colorRandom=StatefulRandom.Restore(s.colorRandom);lastPieceColorIndex=s.lastColor;nextPieceColor=s.nextColor;fallTime=s.fallTime;wasSoftDropping=s.softDrop;
            Active=new ActivePiece(s.active.type,s.active.rotation,new Vector2Int(s.active.x,s.active.y),s.active.color);
            SnapshotCheck.Require(board.CanPlace(Active.Cells),"La pieza guardada no cabe en el tablero.");HasActivePiece=true;
            HeldPiece=s.held<0?(TetrominoType?)null:(TetrominoType)s.held;HeldPieceColor=s.heldColor;HoldUsed=s.holdUsed;
            Score=s.score;LineScore=s.lineScore;LinesCleared=s.lines;PiecesLocked=s.piecesLocked;LastClearCount=s.lastClear;ComboChain=s.combo;MaxCombo=s.maxCombo;LastComboBonus=s.lastComboBonus;ComboBonusScore=s.comboScore;Revision=1;
        }
    }
    public sealed partial class CubotrisSession
    {
        public SessionSnapshot Capture()=>new SessionSnapshot{runId=RunId,mode=Mode,practice=IsPractice,activeFace=ActiveFace,seconds=SurvivalSeconds,boards=Faces.Select(b=>b.Capture()).ToArray(),game=Game.Capture(),
            elapsed=(double[])elapsed.Clone(),intervals=intervals.Select(v=>double.IsInfinity(v)?0:v).ToArray(),pending=(int[])pendingRows.Clone(),faceTime=(double[])totalFaceTime.Clone(),faceLines=(int[])totalFaceLines.Clone(),garbageRandom=garbageRandom.State,
            cycleMask=CycleClearMask,rotations=CompletedRotations,successfulRotations=SuccessfulRotations,lastRotationBonus=LastRotationBonus};
        public static CubotrisSession Restore(SessionSnapshot snapshot){SnapshotCheck.Validate(snapshot);return new CubotrisSession(snapshot);}
        private CubotrisSession(SessionSnapshot s)
        {
            RunId=s.runId;Mode=s.mode;IsPractice=s.practice;ActiveFace=s.activeFace;SurvivalSeconds=s.seconds;
            Faces=Array.AsReadOnly(s.boards.Select(BoardState.Restore).ToArray());Array.Copy(s.elapsed,elapsed,4);Array.Copy(s.pending,pendingRows,4);Array.Copy(s.faceTime,totalFaceTime,4);Array.Copy(s.faceLines,totalFaceLines,4);
            for(int i=0;i<4;i++)intervals[i]=s.intervals[i]==0?double.PositiveInfinity:s.intervals[i];garbageRandom=StatefulRandom.Restore(s.garbageRandom);
            CycleClearMask=s.cycleMask;CompletedRotations=s.rotations;SuccessfulRotations=s.successfulRotations;LastRotationBonus=s.lastRotationBonus;
            Game=new TetrisGame(s.game,Faces[ActiveFace],AdvanceFace,ApplyPendingGarbage);
        }
    }
}
