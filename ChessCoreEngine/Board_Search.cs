using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ChessEngine.Engine
{
    public partial class Board
    {
        internal static int progress;
		
		private static int piecesRemaining;
		

        private struct Position
        {
            internal byte SrcPosition;
            internal byte DstPosition;
            internal int Score;
            //internal bool TopSort;
            internal string Move;

            public new string ToString() => Move;
        }

        private static readonly Position[,] KillerMove = new Position[3,20];
        private static int kIndex;

        private static int ComparePositionScore(Position s2, Position s1) => (s1.Score).CompareTo(s2.Score);

        private static int CompareBoardScore(Board s2, Board s1) => (s1.Score).CompareTo(s2.Score);

        private int SideToMoveScore(int score, ChessPieceColor color)
        {
            if (color == ChessPieceColor.Black)
                return -score;

            return score;
        }

        public MoveContent IterativeSearch(byte depth, ref int nodesSearched, ref int nodesQuiessence, ref string pvLine, ref byte plyDepthReached, ref byte rootMovesSearched, IDictionary<string,OpeningMove> currentGameBook)
        {
            List<Position> pvChild = new List<Position>();
            int alpha = -400000000;
            const int beta = 400000000;
			
            
            MoveContent bestMove = new MoveContent();

            //We are going to store our result boards here           
            ResultBoards succ = GetSortValidMoves();

            rootMovesSearched = (byte)succ.Positions.Count;

            if (rootMovesSearched == 1)
            {
                //I only have one move
                return succ.Positions[0].LastMove;
            }

            //Can I make an instant mate?
            //Parallel.ForEach<Board>(succ.Positions, pos => 
            foreach (Board pos in succ.Positions)
            {
                int value = -AlphaBeta(pos, 1, -beta, -alpha, ref nodesSearched, ref nodesQuiessence, ref pvChild, true);

                if (value >= 32767)
                {
                    return pos.LastMove;
                }
            }

            int currentBoard = 0;

            alpha = -400000000;

            succ.Positions.Sort(CompareBoardScore);

            depth--;

            plyDepthReached = ModifyDepth(depth, succ.Positions.Count);

            foreach (Board pos in succ.Positions)
            {
                currentBoard++;

				progress = (int)((currentBoard / (decimal)succ.Positions.Count) * 100);

                pvChild = new List<Position>();

                int value = -AlphaBeta(pos, depth, -beta, -alpha, ref nodesSearched, ref nodesQuiessence, ref pvChild, false);

                if (value >= 32767)
                {
                    return pos.LastMove;
                }

                if (RepeatedMove == 2)
                {
                    string fen = pos.Fen(true);

                    foreach (OpeningMove move in currentGameBook.Values)
                    {
                        if (move.EndingFEN == fen)
                        {
                            value = 0;
                            break;
                        }
                    }
                }

                pos.Score = value;

                //If value is greater then alpha this is the best board
                if (value > alpha || alpha == -400000000)
                {
                    pvLine = pos.LastMove.ToString();

                    foreach (Position pvPos in pvChild)
                    {
                        pvLine += " " + pvPos.ToString();
                    }

                    alpha = value;
                    bestMove = pos.LastMove;
                }
            }

            plyDepthReached++;
			progress=100;
		
            return bestMove;
        }

        private ResultBoards GetSortValidMoves()
        {
            ResultBoards succ = new ResultBoards { Positions = new List<Board>(30) };

            piecesRemaining = 0;

#if (USETPL)
            Parallel.For(0,64, (ii) =>
#else
            for (byte ii = 0; ii < 64; ii++)
#endif
            {
                byte x = (byte)ii;
                Square sqr = Squares[x];

                //Make sure there is a piece on the square
                if (sqr.Piece == null)
#if (USETPL)
                    return;
#else
                    continue;
#endif

                piecesRemaining++;

                //Make sure the color is the same color as the one we are moving.
                if (sqr.Piece.PieceColor != WhoseMove)
#if (USETPL)
                    return;
#else
                    continue;
#endif

                    //For each valid move for this piece
                foreach (byte dst in sqr.Piece.ValidMoves)
                {
                    //We make copies of the board and move so that we can move it without effecting the parent board
                    Board board = FastBoardCopy();

                    //Make move so we can examine it
                    board.MovePiece(x, dst, ChessPieceType.Queen);

                    //We Generate Valid Moves for Board
                    board.GenerateValidMoves();

                    //Invalid Move
                    if (board.WhiteCheck && WhoseMove == ChessPieceColor.White)
                    {
                        continue;
                    }

                    //Invalid Move
                    if (board.BlackCheck && WhoseMove == ChessPieceColor.Black)
                    {
                        continue;
                    }

                    //We calculate the board score
                    board.EvaluateBoardScore();

                    //Invert Score to support Negamax
                    board.Score = SideToMoveScore(board.Score, board.WhoseMove);

                    succ.Positions.Add(board);
                }

            }

#if (USETPL)
            );
#endif
            succ.Positions.Sort(CompareBoardScore);
            return succ;
        }

        private int AlphaBeta(Board examineBoard, byte depth, int alpha, int beta, ref int nodesSearched, ref int nodesQuiessence, ref List<Position> pvLine, bool extended)
        {
            nodesSearched++;

            if (examineBoard.FiftyMove >= 50 || examineBoard.RepeatedMove >= 3)
                return 0;

            //End Main Search with Quiescence
            if (depth == 0)
            {
                if (!extended && examineBoard.BlackCheck || examineBoard.WhiteCheck)
                {
                    depth++;
                    extended = true;
                }
                else
                {
                    //Perform a Quiessence Search
                    return Quiescence(examineBoard, alpha, beta, ref nodesQuiessence);
                }
            }

            List<Position> positions = examineBoard.EvaluateMoves(depth);

            if (examineBoard.WhiteCheck || examineBoard.BlackCheck || positions.Count == 0)
            {
                if (examineBoard.SearchForMate(examineBoard.WhoseMove))
                {
                    if (examineBoard.BlackMate)
                    {
                        if (examineBoard.WhoseMove == ChessPieceColor.Black)
                            return -32767-depth;

                        return 32767 + depth;
                    }
                    if (examineBoard.WhiteMate)
                    {
                        if (examineBoard.WhoseMove == ChessPieceColor.Black)
                            return 32767 + depth;

                        return -32767 - depth;
                    }

                    //If Not Mate then StaleMate
                    return 0;
                }
            }

            positions.Sort(ComparePositionScore);

            foreach (Position move in positions)
            {
                List<Position> pvChild = new List<Position>();

                //Make a copy
                Board board = examineBoard.FastBoardCopy();

                //Move Piece
                board.MovePiece(move.SrcPosition, move.DstPosition, ChessPieceType.Queen);

                //We Generate Valid Moves for Board
                board.GenerateValidMoves();

                if (board.BlackCheck)
                {
                    if (examineBoard.WhoseMove == ChessPieceColor.Black)
                    {
                        //Invalid Move
                        continue;
                    }
                }

                if (board.WhiteCheck)
                {
                    if (examineBoard.WhoseMove == ChessPieceColor.White)
                    {
                        //Invalid Move
                        continue;
                    }
                }

                //if (logger.IsInfoLevelLog) logger.Info($"Go in depth {depth - 1} for board {board.Fen(true)}.");

                int value = -AlphaBeta(board, (byte)(depth - 1), -beta, -alpha, ref nodesSearched, ref nodesQuiessence, ref pvChild, extended);

                if (value >= beta)
                {
                    KillerMove[kIndex, depth].SrcPosition = move.SrcPosition;
                    KillerMove[kIndex, depth].DstPosition = move.DstPosition;

                    kIndex = ((kIndex + 1) % 2);

                    
                    return beta;
                }
                if (value > alpha)
                {
                    Position pvPos = new Position
                    {
                        SrcPosition = board.LastMove.MovingPiecePrimary.SrcPosition,
                        DstPosition = board.LastMove.MovingPiecePrimary.DstPosition,
                        Move = board.LastMove.ToString()
                    };

                    pvChild.Insert(0, pvPos);

                    pvLine = pvChild;

                    alpha = (int)value;
                }
            }

            return alpha;
        }

        private int Quiescence(Board examineBoard, int alpha, int beta, ref int nodesSearched)
        {
            nodesSearched++;

            //Evaluate Score
            examineBoard.EvaluateBoardScore();

            //Invert Score to support Negamax
            examineBoard.Score = SideToMoveScore(examineBoard.Score, examineBoard.WhoseMove);

            if (examineBoard.Score >= beta)
                return beta;

            if (examineBoard.Score > alpha)
                alpha = examineBoard.Score;

            
            List<Position> positions;


            positions = ((examineBoard.WhiteCheck || examineBoard.BlackCheck) ? examineBoard.EvaluateMoves(0) : examineBoard.EvaluateMovesQ());

            if (positions.Count == 0) return examineBoard.Score;
            
            positions.Sort(ComparePositionScore);

            foreach (Position move in positions)
            {
                if (StaticExchangeEvaluation(examineBoard.Squares[move.DstPosition]) >= 0)
                {
                    continue;
                }

                //Make a copy
                Board board = examineBoard.FastBoardCopy();

                //Move Piece
                board.MovePiece(move.SrcPosition, move.DstPosition, ChessPieceType.Queen);

                //We Generate Valid Moves for Board
                board.GenerateValidMoves();

                if (board.BlackCheck)
                {
                    if (examineBoard.WhoseMove == ChessPieceColor.Black)
                    {
                        //Invalid Move
                        continue;
                    }
                }

                if (board.WhiteCheck)
                {
                    if (examineBoard.WhoseMove == ChessPieceColor.White)
                    {
                        //Invalid Move
                        continue;
                    }
                }

                int value = -Quiescence(board, - beta, -alpha, ref nodesSearched);

                if (value >= beta)
                {
                    KillerMove[2, 0].SrcPosition = move.SrcPosition;
                    KillerMove[2, 0].DstPosition = move.DstPosition;

                    return beta;
                }
                if (value > alpha)
                {
                    alpha = value;
                }
            }

            return alpha;
        }

        private List<Position> EvaluateMoves(byte depth)
        {

            //We are going to store our result boards here           
            List<Position> positions = new List<Position>();

            //bool foundPV = false;


            for (byte x = 0; x < 64; x++)
            {
                Piece piece = Squares[x].Piece;

                //Make sure there is a piece on the square
                if (piece == null)
                    continue;

                //Make sure the color is the same color as the one we are moving.
                if (piece.PieceColor != WhoseMove)
                    continue;

                //For each valid move for this piece
                foreach (byte dst in piece.ValidMoves)
                {
                    Position move = new Position();

                    move.SrcPosition = x;
                    move.DstPosition = dst;
				
                    if (move.SrcPosition == KillerMove[0, depth].SrcPosition && move.DstPosition == KillerMove[0, depth].DstPosition)
                    {
                        //move.TopSort = true;
                        move.Score += 5000;
                        positions.Add(move);
                        continue;
                    }
                    if (move.SrcPosition == KillerMove[1, depth].SrcPosition && move.DstPosition == KillerMove[1, depth].DstPosition)
                    {
                        //move.TopSort = true;
                        move.Score += 5000;
                        positions.Add(move);
                        continue;
                    }

                    Piece pieceAttacked = Squares[move.DstPosition].Piece;

                    //If the move is a capture add it's value to the score
                    if (pieceAttacked != null)
                    {
                        move.Score += pieceAttacked.PieceValue;

                        if (piece.PieceValue < pieceAttacked.PieceValue)
                        {
                            move.Score += pieceAttacked.PieceValue - piece.PieceValue;
                        }
                    }

                    if (!piece.Moved)
                    {
                        move.Score += 10;
                    }

                    move.Score += piece.PieceActionValue;

                    //Add Score for Castling
                    if (!WhiteCastled && WhoseMove == ChessPieceColor.White)
                    {

                        if (piece.PieceType == ChessPieceType.King)
                        {
                            if (move.DstPosition != 62 && move.DstPosition != 58)
                            {
                                move.Score -= 40;
                            }
                            else
                            {
                                move.Score += 40;
                            }
                        }
                        if (piece.PieceType == ChessPieceType.Rook)
                        {
                            move.Score -= 40;
                        }
                    }

                    if (!BlackCastled && WhoseMove == ChessPieceColor.Black)
                    {
                        if (piece.PieceType == ChessPieceType.King)
                        {
                            if (move.DstPosition != 6 && move.DstPosition != 2)
                            {
                                move.Score -= 40;
                            }
                            else
                            {
                                move.Score += 40;
                            }
                        }
                        if (piece.PieceType == ChessPieceType.Rook)
                        {
                            move.Score -= 40;
                        }
                    }

                    positions.Add(move);
                }
            }

            return positions;
        }

        private List<Position> EvaluateMovesQ()
        {
            //We are going to store our result boards here           
            List<Position> positions = new List<Position>();

            for (byte x = 0; x < 64; x++)
            {
                Piece piece = Squares[x].Piece;

                //Make sure there is a piece on the square
                if (piece == null)
                    continue;

                //Make sure the color is the same color as the one we are moving.
                if (piece.PieceColor != WhoseMove)
                    continue;

                //For each valid move for this piece
                foreach (byte dst in piece.ValidMoves)
                {
                    if (Squares[dst].Piece == null)
                    {
                        continue;
                    }

                    Position move = new Position();

                    move.SrcPosition = x;
                    move.DstPosition = dst;

                    if (move.SrcPosition == KillerMove[2, 0].SrcPosition && move.DstPosition == KillerMove[2, 0].DstPosition)
                    {
                        //move.TopSort = true;
                        move.Score += 5000;
                        positions.Add(move);
                        continue;
                    }

                    Piece pieceAttacked = Squares[move.DstPosition].Piece;

                    move.Score += pieceAttacked.PieceValue;

                    if (piece.PieceValue < pieceAttacked.PieceValue)
                    {
                        move.Score += pieceAttacked.PieceValue - piece.PieceValue;
                    }

                    move.Score += piece.PieceActionValue;


                    positions.Add(move);
                }
            }

            return positions;
        }

        public bool SearchForMate(ChessPieceColor movingSide)
        {
            bool foundNonCheckBlack = false;
            bool foundNonCheckWhite = false;

            for (byte x = 0; x < 64; x++)
            {
                Square sqr = Squares[x];

                //Make sure there is a piece on the square
                if (sqr.Piece == null)
                    continue;

                //Make sure the color is the same color as the one we are moving.
                if (sqr.Piece.PieceColor != movingSide)
                    continue;

                //For each valid move for this piece
                foreach (byte dst in sqr.Piece.ValidMoves)
                {

                    //We make copies of the board and move so that we can move it without effecting the parent board
                    Board board = FastBoardCopy();

                    //Make move so we can examine it
                    board.MovePiece(x, dst, ChessPieceType.Queen);

                    //We Generate Valid Moves for Board
                    board.GenerateValidMoves();

                    if (board.BlackCheck == false)
                    {
                        foundNonCheckBlack = true;
                    }
                    else if (movingSide == ChessPieceColor.Black)
                    {
                        continue;
                    }

                    if (board.WhiteCheck == false )
                    {
                        foundNonCheckWhite = true;
                    }
                    else if (movingSide == ChessPieceColor.White)
                    {
                        continue;
                    }
                }
            }

            if (foundNonCheckBlack == false)
            {
                if (BlackCheck)
                {
                    BlackMate = true;
                    return true;
                }
                if (!WhiteMate && movingSide != ChessPieceColor.White)
                {
                    StaleMate = true;
                    return true;
                }
            }

            if (foundNonCheckWhite == false)
            {
                if (WhiteCheck)
                {
                    WhiteMate = true;
                    return true;
                }
                if (!BlackMate && movingSide != ChessPieceColor.Black)
                {
                    StaleMate = true;
                    return true;
                }
            }

            return false;
        }

        private byte ModifyDepth(byte depth, int possibleMoves)
        {
            if (possibleMoves <= 20 || piecesRemaining < 14)
            {
                if (possibleMoves <= 10 || piecesRemaining < 6)
                {
                    depth += 1;
                }

                depth += 1;
            }

            return depth;
        }

        private int StaticExchangeEvaluation(Square examineSquare)
        {
            if (examineSquare.Piece == null)
            {
                return 0;
            }
            if (examineSquare.Piece.AttackedValue == 0)
            {
                return 0;
            }

            return examineSquare.Piece.PieceActionValue - examineSquare.Piece.AttackedValue + examineSquare.Piece.DefendedValue;
        }
    }
}
