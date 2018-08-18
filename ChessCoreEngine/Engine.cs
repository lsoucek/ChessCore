using ChessCoreEngine.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ChessEngine.Engine
{
    public sealed class Engine
    {
        #region InternalMembers
        static Logger logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal Book CurrentGameBook;
        internal Book UndoGameBook;
        
        #endregion

        #region PrivateMembers

        private Board ChessBoard;
        private Board PreviousChessBoard;
        private Board UndoChessBoard;
        
        private Stack<MoveContent> MoveHistory;
        private Book OpeningBook = null;

        private string pvLine;
        public string PvLine { get { return pvLine; } set { pvLine = value; } }

        #endregion

        #region PublicMembers

        public enum Difficulty
        {
            Easy,
            Medium,
            Hard,
            VeryHard
        }

        public enum TimeSettings
        {
            Moves40In1Minute,
            Moves40In5Minutes,
            Moves40In10Minutes,
            Moves40In20Minutes,
            Moves40In30Minutes,
            Moves40In40Minutes,
            Moves40In60Minutes,
            Moves40In90Minutes,
        }

        public ChessPieceType PromoteToPieceType = ChessPieceType.Queen;

        public PiecesTaken PiecesTakenCount = new PiecesTaken();

        //State Variables
        public ChessPieceColor HumanPlayer;
        public bool Thinking;
        public bool TrainingMode;

        //Stats
        public int NodesSearched;
        public int NodesQuiescence;
        public byte PlyDepthSearched;
        public byte PlyDepthReached;
        public byte RootMovesSearched;

        public TimeSettings GameTimeSettings;
        public bool TrySetTimeControl(string numMoves, string timeControl)
        {
            if (!int.TryParse(numMoves, out int movesCount)) { logger.Error($"Parameter numMoves not int value -> {numMoves}"); return false; }
            if (!int.TryParse(timeControl, out int timeControlValue)) { logger.Error($"Parameter timeControl not int value -> {timeControl}"); return false; }

            if (movesCount == 40)
            {
                if (timeControlValue >= 0 && timeControlValue < 5) { GameTimeSettings = TimeSettings.Moves40In1Minute; return true; }
                if (timeControlValue >= 5 && timeControlValue < 10) { GameTimeSettings = TimeSettings.Moves40In5Minutes; return true; }
                if (timeControlValue >= 10 && timeControlValue < 20) { GameTimeSettings = TimeSettings.Moves40In10Minutes; return true; }
                if (timeControlValue >= 20 && timeControlValue < 30) { GameTimeSettings = TimeSettings.Moves40In20Minutes; return true; }
                if (timeControlValue >= 30 && timeControlValue < 40) { GameTimeSettings = TimeSettings.Moves40In30Minutes; return true; }
                if (timeControlValue >= 40 && timeControlValue < 60) { GameTimeSettings = TimeSettings.Moves40In40Minutes; return true; }
                if (timeControlValue >= 60 && timeControlValue < 90) { GameTimeSettings = TimeSettings.Moves40In60Minutes; return true; }
                if (timeControlValue >= 90) GameTimeSettings = TimeSettings.Moves40In90Minutes;
                return true;
            }

            logger.Error($"Supplied values {numMoves} {timeControl} don't match supported values.");
            return false;
        }
        public string FEN => ChessBoard.Fen(false);

        public MoveContent LastMove => ChessBoard.LastMove; 

        public Difficulty GameDifficulty
        {
            get
            {
                switch (PlyDepthSearched)
                {
                    case 3: return Difficulty.Easy;
                    case 5: return Difficulty.Medium;
                    case 6: return Difficulty.Hard;
                    case 7: return Difficulty.VeryHard;
                    default: return Difficulty.Medium;
                }
			}
			set
            {
                switch (value)
                {
                    case Difficulty.Easy:
                        PlyDepthSearched = 3;
                        GameTimeSettings = TimeSettings.Moves40In10Minutes;
                        break;
                    case Difficulty.Medium:
                        PlyDepthSearched = 5;
                        //PlyDepthSearched = 10;
                        GameTimeSettings = TimeSettings.Moves40In20Minutes;
                        break;
                    case Difficulty.Hard:
                        PlyDepthSearched = 6;
                        //PlyDepthSearched = 12;
                        GameTimeSettings = TimeSettings.Moves40In60Minutes;
                        break;
                    case Difficulty.VeryHard:
                        PlyDepthSearched = 7;
                        //PlyDepthSearched = 14;
                        GameTimeSettings = TimeSettings.Moves40In90Minutes;
                        break;
                }
            }
        }

        public ChessPieceColor WhoseMove
        {
            get { return ChessBoard.WhoseMove; }
            set { ChessBoard.WhoseMove = value; }
        }

        public bool StaleMate
        {
            get { return ChessBoard.StaleMate; }
            set { ChessBoard.StaleMate = value; }
        }

        public bool RepeatedMove => ChessBoard.RepeatedMove >= 3;

        public bool FiftyMove => ChessBoard.FiftyMove >= 50;

        public bool InsufficientMaterial => ChessBoard.InsufficientMaterial;
        #endregion

        #region Constructors

        public Engine() { NewGame(); }

        public Engine(string fen) { NewGame(fen); }

        public void NewGame(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
        {
            InitiateEngine();
            InitiateBoard(fen);
        }

        public void InitiateBoard(string fen)
        {
            ChessBoard = new Board(fen);

            if (!string.IsNullOrEmpty(fen))
            {
                GenerateValidMoves();
                ChessBoard.EvaluateBoardScore();
            }
        }

        private void InitiateEngine()
        {
            GameDifficulty = Difficulty.Medium;

            MoveHistory = new Stack<MoveContent>();
            HumanPlayer = ChessPieceColor.White;
            if (OpeningBook == null) OpeningBook = new Book(true);
            CurrentGameBook = new Book();
            PieceMoves.InitiateChessPieceMotion();
        }
        #endregion

        #region Methods
        public void SetChessPieceSelection(byte boardColumn, byte boardRow, bool selection)
        {
            byte index = GetBoardIndex(boardColumn, boardRow);
            Piece piece = ChessBoard.Squares[index].Piece;

            if ((piece == null) || (piece.PieceColor != HumanPlayer) || (piece.PieceColor != WhoseMove)) return;

            piece.Selected = selection;
        }

        private static bool CheckForMate(ChessPieceColor whosTurn, ref Board chessBoard)
        {
            chessBoard.SearchForMate(whosTurn);

            return ((chessBoard.BlackMate || chessBoard.WhiteMate || chessBoard.StaleMate) ? true : false);
        }

        public void Undo()
        {
            if (UndoChessBoard != null)
            {
                PieceTakenRemove(ChessBoard.LastMove);
                PieceTakenRemove(PreviousChessBoard.LastMove);

                ChessBoard = new Board(UndoChessBoard);
                CurrentGameBook = UndoGameBook.MakeClone();

                ChessBoard.GenerateValidMoves();
                ChessBoard.EvaluateBoardScore();
            }
        }

        private static byte GetBoardIndex(byte boardColumn, byte boardRow) => (byte)(boardColumn + (boardRow * 8));

        public byte[] GetEnPassantMoves()
        {
            if (ChessBoard == null)
            {
                return null;
            }

            var returnArray = new byte[2];

            returnArray[0] = (byte)(ChessBoard.EnPassantPosition % 8);
            returnArray[1] = (byte)(ChessBoard.EnPassantPosition / 8);

            return returnArray;
        }

        public bool GetBlackMate()
        {
            if (ChessBoard == null) return false;

            return ChessBoard.BlackMate;
        }

        public bool GetWhiteMate() => ChessBoard.WhiteMate;

        public bool GetBlackCheck() => ChessBoard.BlackCheck;

        public bool GetWhiteCheck() => ChessBoard.WhiteCheck;

        public byte GetRepeatedMove() => ChessBoard.RepeatedMove;

        public byte GetFiftyMoveCount() => ChessBoard.FiftyMove;

        public Stack<MoveContent> GetMoveHistory() => MoveHistory;

        public ChessPieceType GetPieceTypeAt(byte boardColumn, byte boardRow)
        {
            byte index = GetBoardIndex(boardColumn, boardRow);

            if (ChessBoard.Squares[index].Piece == null)
            {
                return ChessPieceType.None;
            }

            return ChessBoard.Squares[index].Piece.PieceType;
        }

        public ChessPieceType GetPieceTypeAt(byte index) => ((ChessBoard.Squares[index].Piece == null) ? ChessPieceType.None : ChessBoard.Squares[index].Piece.PieceType);

        public ChessPieceColor GetPieceColorAt(byte boardColumn, byte boardRow)
        {
            byte index = GetBoardIndex(boardColumn, boardRow);

            return ((ChessBoard.Squares[index].Piece == null) ? ChessPieceColor.White : ChessBoard.Squares[index].Piece.PieceColor);
        }

        public ChessPieceColor GetPieceColorAt(byte index) => ((ChessBoard.Squares[index].Piece == null) ? ChessPieceColor.White : ChessBoard.Squares[index].Piece.PieceColor);

        public bool GetChessPieceSelected(byte boardColumn, byte boardRow)
        {
            byte index = GetBoardIndex(boardColumn, boardRow);

            return ((ChessBoard.Squares[index].Piece == null) ? false : ChessBoard.Squares[index].Piece.Selected);
        }

        public void GenerateValidMoves() => ChessBoard.GenerateValidMoves();
        public int EvaluateBoardScore() => ChessBoard.EvaluateBoardScore();

        public byte[][] GetValidMoves(byte boardColumn, byte boardRow)
        {
            byte index = GetBoardIndex(boardColumn, boardRow);

            if (ChessBoard.Squares[index].Piece ==
                null)
            {
                return null;
            }

            var returnArray = new byte[ChessBoard.Squares[index].Piece.ValidMoves.Count][];
            int counter = 0;

            foreach (byte square in ChessBoard.Squares[index].Piece.ValidMoves)
            {
                returnArray[counter] = new byte[2];
                returnArray[counter][0] = (byte)(square % 8);
                returnArray[counter][1] = (byte)(square /8);
                counter++;
            }

            return returnArray;
        }

        public int GetScore()
        {
            return ChessBoard.Score;
        }

        public byte FindSourcePositon(ChessPieceType chessPieceType, ChessPieceColor chessPieceColor, byte dstPosition, bool capture, int forceCol, int forceRow)
        {
            Square square;

            if (dstPosition == ChessBoard.EnPassantPosition && chessPieceType == ChessPieceType.Pawn)
            {
                if (chessPieceColor == ChessPieceColor.White)
                {
                    square = ChessBoard.Squares[dstPosition + 7];

                    if (square.Piece != null)
                    {
                        if (square.Piece.PieceType == ChessPieceType.Pawn)
                        {
                            if (square.Piece.PieceColor == chessPieceColor)
                            {
                                if ((dstPosition + 7) % 8 == forceCol || forceCol == -1)
                                {
                                    return (byte)(dstPosition + 7);
                                }
                                
                            }
                        }
                    }

                    square = ChessBoard.Squares[dstPosition + 9];

                    if (square.Piece != null)
                    {
                        if (square.Piece.PieceType == ChessPieceType.Pawn)
                        {
                            if (square.Piece.PieceColor == chessPieceColor)
                            {
                                if ((dstPosition + 9) % 8 == forceCol || forceCol == -1)
                                {
                                    return (byte) (dstPosition + 9);
                                }
                            }
                        }
                    }
                }
                else 
                {
                    if (dstPosition - 7 >= 0)
                    {
                        square = ChessBoard.Squares[dstPosition - 7];

                        if (square.Piece != null)
                        {
                            if (square.Piece.PieceType == ChessPieceType.Pawn)
                            {
                                if (square.Piece.PieceColor == chessPieceColor)
                                {
                                    if ((dstPosition - 7)%8 == forceCol || forceCol == -1)
                                    {
                                        return (byte) (dstPosition - 7);
                                    }
                                }
                            }
                        }
                    }
                    if (dstPosition - 9 >= 0)
                    {
                        square = ChessBoard.Squares[dstPosition - 9];

                        if (square.Piece != null)
                        {
                            if (square.Piece.PieceType == ChessPieceType.Pawn)
                            {
                                if (square.Piece.PieceColor == chessPieceColor)
                                {
                                    if ((dstPosition - 9)%8 == forceCol || forceCol == -1)
                                    {
                                        return (byte) (dstPosition - 9);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (byte x = 0; x < 64; x++)
            {
                square = ChessBoard.Squares[x];

                if (square.Piece == null)
                    continue;
                if (square.Piece.PieceType != chessPieceType)
                    continue;
                if (square.Piece.PieceColor != chessPieceColor)
                    continue;
               
                foreach (byte move in square.Piece.ValidMoves)
                {
                    if (move == dstPosition)
                    {
                        if (!capture)
                        {
                            if ((byte)(x / 8) == (forceRow) || forceRow == -1)
                            {
                                if (x%8 == forceCol || forceCol == -1)
                                {
                                    return x;
                                }
                            }
                        }
                                
                        //Capture
                        if (ChessBoard.Squares[dstPosition].Piece != null)
                        {
                            if (ChessBoard.Squares[dstPosition].Piece.PieceColor != chessPieceColor)
                            {
                                if (x % 8 == forceCol || forceCol == -1)
                                {
                                    if ((byte)(x / 8) == (forceRow) || forceRow == -1)
                                    {
                                        return x;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return 0;
        }

        public bool IsValidMove(byte srcPosition, byte dstPosition)
        {
            if (ChessBoard == null)
            {
                return false;
            }

            if (ChessBoard.Squares == null)
            {
                return false;
            }

            if (ChessBoard.Squares[srcPosition].Piece == null)
            {
                return false;
            }

            foreach (byte bs in ChessBoard.Squares[srcPosition].Piece.ValidMoves)
            {
                if (bs == dstPosition)
                {
                    return true;
                }
            }

            if (dstPosition == ChessBoard.EnPassantPosition)
            {
                return true;
            }

            return false;
        }

        public bool IsValidMove(byte sourceColumn, byte sourceRow, byte destinationColumn, byte destinationRow)
        {
            if (ChessBoard == null)
            {
                return false;
            }

            if (ChessBoard.Squares == null)
            {
                return false;
            }

            byte index = GetBoardIndex(sourceColumn, sourceRow);

            if (ChessBoard.Squares[index].Piece == null)
            {
                return false;
            }

            foreach (byte bs in ChessBoard.Squares[index].Piece.ValidMoves)
            {
                if (bs % 8 == destinationColumn)
                {
                    if ((byte)(bs / 8) == destinationRow)
                    {
                        return true;
                    }
                }
            }

            /*index = GetBoardIndex(destinationColumn, destinationRow);

            if (index == ChessBoard.EnPassantPosition && ChessBoard.EnPassantPosition > 0)
            {
                return true;
            }*/

            return false;
        }

        public bool IsGameOver()
        {
            if (ChessBoard.StaleMate)
            {
                return true;
            }
            if (ChessBoard.WhiteMate || ChessBoard.BlackMate)
            {
                return true;
            }
            if (ChessBoard.FiftyMove >= 50)
            {
                return true;
            }
            if (ChessBoard.RepeatedMove >= 3)
            {
                return true;
            }

            if (ChessBoard.InsufficientMaterial)
            {
                return true;
            }
            return false;
        }

        public bool IsTie()
        {
            if (ChessBoard.StaleMate)
            {
                return true;
            }
            
            if (ChessBoard.FiftyMove >= 50)
            {
                return true;
            }
            if (ChessBoard.RepeatedMove >= 3)
            {
                return true;
            }

            if (ChessBoard.InsufficientMaterial)
            {
                return true;
            }

            return false;
        }
        public bool MovePiece(byte srcPosition, byte dstPosition)
        {
            Piece piece = ChessBoard.Squares[srcPosition].Piece;

            PreviousChessBoard = new Board(ChessBoard);
            UndoChessBoard = new Board(ChessBoard);
            UndoGameBook = new Book(CurrentGameBook);

            ChessBoard.MovePiece(srcPosition, dstPosition, PromoteToPieceType);

            ChessBoard.LastMove.GeneratePGNString(ChessBoard);

            GenerateValidMoves();
            ChessBoard.EvaluateBoardScore();

            //If there is a check in place, check if this is still true;
            if (piece.PieceColor == ChessPieceColor.White)
            {
                if (ChessBoard.WhiteCheck)
                {
                    //Invalid Move
                    ChessBoard = new Board(PreviousChessBoard);
                    ChessBoard.GenerateValidMoves();
                    return false;
                }
            }
            else if (piece.PieceColor == ChessPieceColor.Black)
            {
                if (ChessBoard.BlackCheck)
                {
                    //Invalid Move
                    ChessBoard = new Board(PreviousChessBoard);
                    ChessBoard.GenerateValidMoves();
                    return false;
                }
            }
   
            MoveHistory.Push(ChessBoard.LastMove);
            FileIO.SaveCurrentGameMove(ChessBoard, PreviousChessBoard, CurrentGameBook, ChessBoard.LastMove);

            CheckForMate(WhoseMove, ref ChessBoard);
            PieceTakenAdd(ChessBoard.LastMove);

            if (ChessBoard.WhiteMate || ChessBoard.BlackMate)
            {
                LastMove.PgnMove += "#";
            }
            else if (ChessBoard.WhiteCheck || ChessBoard.BlackCheck)
            {
                LastMove.PgnMove += "+";
            }

            return true;
        }

        private void PieceTakenAdd(MoveContent lastMove)
        {
            if (lastMove.TakenPiece.PieceType != ChessPieceType.None)
            {
                if (lastMove.TakenPiece.PieceColor == ChessPieceColor.White)
                {
                    if (lastMove.TakenPiece.PieceType == ChessPieceType.Queen)
                    {
                        PiecesTakenCount.WhiteQueen++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Rook)
                    {
                        PiecesTakenCount.WhiteRook++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Bishop)
                    {
                        PiecesTakenCount.WhiteBishop++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Knight)
                    {
                        PiecesTakenCount.WhiteKnight++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Pawn)
                    {
                        PiecesTakenCount.WhitePawn++;
                    }
                }
                if (ChessBoard.LastMove.TakenPiece.PieceColor == ChessPieceColor.Black)
                {
                    if (lastMove.TakenPiece.PieceType == ChessPieceType.Queen)
                    {
                        PiecesTakenCount.BlackQueen++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Rook)
                    {
                        PiecesTakenCount.BlackRook++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Bishop)
                    {
                        PiecesTakenCount.BlackBishop++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Knight)
                    {
                        PiecesTakenCount.BlackKnight++;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Pawn)
                    {
                        PiecesTakenCount.BlackPawn++;
                    }
                }
            }
        }

        private void PieceTakenRemove(MoveContent lastMove)
        {
            if (lastMove.TakenPiece.PieceType != ChessPieceType.None)
            {
                if (lastMove.TakenPiece.PieceColor == ChessPieceColor.White)
                {
                    if (lastMove.TakenPiece.PieceType == ChessPieceType.Queen)
                    {
                        PiecesTakenCount.WhiteQueen--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Rook)
                    {
                        PiecesTakenCount.WhiteRook--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Bishop)
                    {
                        PiecesTakenCount.WhiteBishop--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Knight)
                    {
                        PiecesTakenCount.WhiteKnight--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Pawn)
                    {
                        PiecesTakenCount.WhitePawn--;
                    }
                }
                if (lastMove.TakenPiece.PieceColor == ChessPieceColor.Black)
                {
                    if (lastMove.TakenPiece.PieceType == ChessPieceType.Queen)
                    {
                        PiecesTakenCount.BlackQueen--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Rook)
                    {
                        PiecesTakenCount.BlackRook--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Bishop)
                    {
                        PiecesTakenCount.BlackBishop--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Knight)
                    {
                        PiecesTakenCount.BlackKnight--;
                    }
                    else if (lastMove.TakenPiece.PieceType == ChessPieceType.Pawn)
                    {
                        PiecesTakenCount.BlackPawn--;
                    }
                }
            }
        }

        public bool MovePiece(byte sourceColumn, byte sourceRow, byte destinationColumn, byte destinationRow)
        {
            byte srcPosition = (byte)(sourceColumn + (sourceRow * 8));
            byte dstPosition = (byte)(destinationColumn + (destinationRow * 8));

            return MovePiece(srcPosition, dstPosition);
        }

        internal void SetChessPiece(Piece piece, byte index)
        {
            ChessBoard.Squares[index].Piece = new Piece(piece);

        }

        #endregion

        #region Search

        public void AiPonderMove()
        {
            Thinking = true;
            NodesSearched = 0;
			
			var resultBoards = new ResultBoards();
            resultBoards.Positions = new List<Board>();

            if (CheckForMate(WhoseMove, ref ChessBoard))
            {
                Thinking = false;
				return;
            }

            MoveContent bestMove = new MoveContent();
           
            //If there is no playbook move search for the best move
            if (OpeningBook.TryGetMove(ChessBoard, ref bestMove) == false || ChessBoard.FiftyMove > 45 || ChessBoard.RepeatedMove >= 2)
            {
                if (CurrentGameBook.TryGetMove(ChessBoard, ref bestMove) == false || ChessBoard.FiftyMove > 45 || ChessBoard.RepeatedMove >= 2)
                {
					bestMove = ChessBoard.IterativeSearch(PlyDepthSearched, ref NodesSearched, ref NodesQuiescence, ref pvLine, ref PlyDepthReached, ref RootMovesSearched, CurrentGameBook.MoveList);
                }
            }
 
            //Make the move 
            PreviousChessBoard = new Board(ChessBoard);

            RootMovesSearched = (byte)resultBoards.Positions.Count;

            ChessBoard.MovePiece(bestMove.MovingPiecePrimary.SrcPosition, bestMove.MovingPiecePrimary.DstPosition, ChessPieceType.Queen);

            ChessBoard.LastMove.GeneratePGNString(ChessBoard);

            FileIO.SaveCurrentGameMove(ChessBoard, PreviousChessBoard, CurrentGameBook, bestMove);

            for (byte x = 0; x < 64; x++)
            {
                Square sqr = ChessBoard.Squares[x];

                if (sqr.Piece == null)
                    continue;

                sqr.Piece.DefendedValue = 0;
                sqr.Piece.AttackedValue = 0;
            }

            ChessBoard.GenerateValidMoves();
            ChessBoard.EvaluateBoardScore();

            PieceTakenAdd(ChessBoard.LastMove);

            MoveHistory.Push(ChessBoard.LastMove);

            if (CheckForMate(WhoseMove, ref ChessBoard))
            {
                Thinking = false;

                if (ChessBoard.WhiteMate || ChessBoard.BlackMate)
                {
                    LastMove.PgnMove += "#";
                }
				
                return;
            }

            if (ChessBoard.WhiteCheck || ChessBoard.BlackCheck)
            {
                LastMove.PgnMove += "+";
            }

            Thinking = false;
		}
        #endregion

        #region Test

        public Test.PerformanceResult RunPerformanceTest()
        {
            return Test.RunPerfTest(5, ChessBoard);
        }

        #endregion

        #region FileIO
        public bool SaveGame(string filePath) => FileIO.SaveGame(filePath, ChessBoard, WhoseMove, MoveHistory);

        public bool LoadGame(String filePath) => FileIO.LoadGame(filePath, ChessBoard, WhoseMove, MoveHistory, CurrentGameBook, UndoGameBook);
        #endregion

        #region Show Board
        public string DrawBoard()
        {
            //Console.Clear();
            StringBuilder result = new StringBuilder();

            for (byte i = 0; i < 64; i++)
            {
                if (i % 8 == 0)
                {
                    result.AppendLine();
                    result.AppendLine(" ---------------------------------");
                    result.Append((8 - (i / 8)));
                }

                ChessPieceType PieceType = GetPieceTypeAt(i);
                ChessPieceColor PieceColor = GetPieceColorAt(i);

                switch (PieceType)
                {
                    case ChessPieceType.Pawn: result.Append(FormatPiece("Pp", PieceColor)); break;
                    case ChessPieceType.Knight: result.Append(FormatPiece("Nn", PieceColor)); break;
                    case ChessPieceType.Bishop: result.Append(FormatPiece("Bb", PieceColor)); break;
                    case ChessPieceType.Rook: result.Append(FormatPiece("Rr", PieceColor)); break;
                    case ChessPieceType.Queen: result.Append(FormatPiece("Qq", PieceColor)); break;
                    case ChessPieceType.King: result.Append(FormatPiece("Kk", PieceColor)); break;
                    default: result.Append(FormatPiece("  ", PieceColor)); break;
                }

                if (i % 8 == 7) result.Append("|");
            }

            result.AppendLine();
            result.AppendLine(" ---------------------------------");
            result.AppendLine("   A   B   C   D   E   F   G   H");

            return result.ToString();

            string FormatPiece(string pieceOnConsole, ChessPieceColor pieceColor) => $"| {pieceOnConsole[((pieceColor == ChessPieceColor.Black) ? 0 : 1)]} ";
        }
        #endregion
    }
}
