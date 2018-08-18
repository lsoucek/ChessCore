using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChessEngine.Engine
{
    public partial class Board
    {
        #region Validation
        public bool IsValidMove(byte srcPos, byte dstPos)
        {
            GenerateValidMoves();

            if (Squares == null)
            {
                return false;
            }

            if (Squares[srcPos].Piece == null)
            {
                return false;
            }

            foreach (byte bs in Squares[srcPos].Piece.ValidMoves)
            {
                if (bs == dstPos)
                {
                    return true;
                }
            }

            if (dstPos == EnPassantPosition)
            {
                return true;
            }

            return false;
        }

        private void AnalyzeMovePawn(byte dstPos, Piece pcMoving)
        {
            //Because Pawns only kill diagonaly we handle the En Passant scenario specialy
            if (EnPassantPosition > 0)
            {
                if (pcMoving.PieceColor != EnPassantColor)
                {
                    if (EnPassantPosition == dstPos)
                    {
                        //We have an En Passant Possible
                        pcMoving.ValidMoves.Push(dstPos);

                        if (pcMoving.PieceColor == ChessPieceColor.White)
                        {
                            WhiteAttackBoard[dstPos] = true;
                        }
                        else
                        {
                            BlackAttackBoard[dstPos] = true;
                        }
                    }
                }
            }

            Piece pcAttacked = Squares[dstPos].Piece;

            //If there no piece there I can potentialy kill
            if (pcAttacked == null)
                return;

            //Regardless of what is there I am attacking this square
            if (pcMoving.PieceColor == ChessPieceColor.White)
            {
                WhiteAttackBoard[dstPos] = true;

                //if that piece is the same color
                if (pcAttacked.PieceColor == pcMoving.PieceColor)
                {
                    pcAttacked.DefendedValue += pcMoving.PieceActionValue;
                    return;
                }

                pcAttacked.AttackedValue += pcMoving.PieceActionValue;

                //If this is a king set it in check                   
                if (pcAttacked.PieceType == ChessPieceType.King)
                {
                    BlackCheck = true;
                }
                else
                {
                    //Add this as a valid move
                    pcMoving.ValidMoves.Push(dstPos);
                }
            }
            else
            {
                BlackAttackBoard[dstPos] = true;

                //if that piece is the same color
                if (pcAttacked.PieceColor == pcMoving.PieceColor)
                {
                    pcAttacked.DefendedValue += pcMoving.PieceActionValue;
                    return;
                }

                pcAttacked.AttackedValue += pcMoving.PieceActionValue;

                //If this is a king set it in check                   
                if (pcAttacked.PieceType == ChessPieceType.King)
                {
                    WhiteCheck = true;
                }
                else
                {
                    //Add this as a valid move
                    pcMoving.ValidMoves.Push(dstPos);
                }
            }

            return;
        }

        private bool AnalyzeMove(byte dstPos, Piece pcMoving)
        {
            //If I am not a pawn everywhere I move I can attack
            if (pcMoving.PieceColor == ChessPieceColor.White)
            {
                WhiteAttackBoard[dstPos] = true;
            }
            else
            {
                BlackAttackBoard[dstPos] = true;
            }

            //If there no piece there I can potentialy kill just add the move and exit
            if (Squares[dstPos].Piece == null)
            {
                pcMoving.ValidMoves.Push(dstPos);

                return true;
            }

            Piece pcAttacked = Squares[dstPos].Piece;

            //if that piece is a different color
            if (pcAttacked.PieceColor != pcMoving.PieceColor)
            {
                pcAttacked.AttackedValue += pcMoving.PieceActionValue;

                //If this is a king set it in check                   
                if (pcAttacked.PieceType == ChessPieceType.King)
                {
                    if (pcAttacked.PieceColor == ChessPieceColor.Black)
                    {
                        BlackCheck = true;
                    }
                    else
                    {
                        WhiteCheck = true;
                    }
                }
                else
                {
                    //Add this as a valid move
                    pcMoving.ValidMoves.Push(dstPos);
                }


                //We don't continue movement past this piece
                return false;
            }
            //Same Color I am defending
            pcAttacked.DefendedValue += pcMoving.PieceActionValue;

            //Since this piece is of my kind I can't move there
            return false;
        }

        private void CheckValidMovesPawn(List<byte> moves, Piece pcMoving, byte srcPosition, byte count)
        {
            for (byte i = 0; i < count; i++)
            {
                byte dstPos = moves[i];

                //Diagonal
                if (dstPos % 8 != srcPosition % 8)
                {
                    //If there is a piece there I can potentialy kill
                    AnalyzeMovePawn(dstPos, pcMoving);

                    if (pcMoving.PieceColor == ChessPieceColor.White)
                    {
                        WhiteAttackBoard[dstPos] = true;
                    }
                    else
                    {
                        BlackAttackBoard[dstPos] = true;
                    }
                }
                // if there is something if front pawns can't move there
                else if (Squares[dstPos].Piece != null)
                {
                    return;
                }
                //if there is nothing in front of 
                else
                {
                    pcMoving.ValidMoves.Push(dstPos);
                }
            }
        }

        private void GenerateValidMovesKing(Piece piece, byte srcPosition)
        {
            if (piece == null)
            {
                return;
            }

            for (byte i = 0; i < MoveArrays.KingTotalMoves[srcPosition]; i++)
            {
                byte dstPos = MoveArrays.KingMoves[srcPosition].Moves[i];

                if (piece.PieceColor == ChessPieceColor.White)
                {
                    //I can't move where I am being attacked
                    if (BlackAttackBoard[dstPos])
                    {
                        WhiteAttackBoard[dstPos] = true;
                        continue;
                    }
                }
                else
                {
                    if (WhiteAttackBoard[dstPos])
                    {
                        BlackAttackBoard[dstPos] = true;
                        continue;
                    }
                }

                AnalyzeMove(dstPos, piece);
            }
        }

        private void GenerateValidMovesKingCastle(Piece king)
        {
            //This code will add the castleling move to the pieces available moves
            if (king.PieceColor == ChessPieceColor.White)
            {
                if (Squares[63].Piece != null)
                {
                    //Check if the Right Rook is still in the correct position
                    if (Squares[63].Piece.PieceType == ChessPieceType.Rook)
                    {
                        if (Squares[63].Piece.PieceColor == king.PieceColor)
                        {
                            //Move one column to right see if its empty
                            if (Squares[62].Piece == null)
                            {
                                if (Squares[61].Piece == null)
                                {
                                    if (BlackAttackBoard[61] == false &&
                                        BlackAttackBoard[62] == false)
                                    {
                                        //Ok looks like move is valid lets add it
                                        king.ValidMoves.Push(62);
                                        WhiteAttackBoard[62] = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (Squares[56].Piece != null)
                {
                    //Check if the Left Rook is still in the correct position
                    if (Squares[56].Piece.PieceType == ChessPieceType.Rook)
                    {
                        if (Squares[56].Piece.PieceColor == king.PieceColor)
                        {
                            //Move one column to right see if its empty
                            if (Squares[57].Piece == null)
                            {
                                if (Squares[58].Piece == null)
                                {
                                    if (Squares[59].Piece == null)
                                    {
                                        if (BlackAttackBoard[58] == false &&
                                            BlackAttackBoard[59] == false)
                                        {
                                            //Ok looks like move is valid lets add it
                                            king.ValidMoves.Push(58);
                                            WhiteAttackBoard[58] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (king.PieceColor == ChessPieceColor.Black)
            {
                //There are two ways to castle, scenario 1:
                if (Squares[7].Piece != null)
                {
                    //Check if the Right Rook is still in the correct position
                    if (Squares[7].Piece.PieceType == ChessPieceType.Rook
                        && !Squares[7].Piece.Moved)
                    {
                        if (Squares[7].Piece.PieceColor == king.PieceColor)
                        {
                            //Move one column to right see if its empty

                            if (Squares[6].Piece == null)
                            {
                                if (Squares[5].Piece == null)
                                {
                                    if (WhiteAttackBoard[5] == false && WhiteAttackBoard[6] == false)
                                    {
                                        //Ok looks like move is valid lets add it
                                        king.ValidMoves.Push(6);
                                        BlackAttackBoard[6] = true;
                                    }
                                }
                            }
                        }
                    }
                }
                //There are two ways to castle, scenario 2:
                if (Squares[0].Piece != null)
                {
                    //Check if the Left Rook is still in the correct position
                    if (Squares[0].Piece.PieceType == ChessPieceType.Rook &&
                        !Squares[0].Piece.Moved)
                    {
                        if (Squares[0].Piece.PieceColor ==
                            king.PieceColor)
                        {
                            //Move one column to right see if its empty
                            if (Squares[1].Piece == null)
                            {
                                if (Squares[2].Piece == null)
                                {
                                    if (Squares[3].Piece == null)
                                    {
                                        if (WhiteAttackBoard[2] == false &&
                                            WhiteAttackBoard[3] == false)
                                        {
                                            //Ok looks like move is valid lets add it
                                            king.ValidMoves.Push(2);
                                            BlackAttackBoard[2] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void GenerateValidMoves()
        {
            // Reset Board
            BlackCheck = false;
            WhiteCheck = false;

            byte blackRooksMoved = 0;
            byte whiteRooksMoved = 0;

            //Calculate Remaining Material on Board to make the End Game Decision
            int remainingPieces = 0;

            //Generate Moves
#if (USETPL)
            Parallel.For(0, 64, (ii) =>
#else
            for (byte ii = 0; ii < 64; ii++)
#endif
            {
                byte x = (byte)ii;
                Square sqr = Squares[x];

                if (sqr.Piece == null)
#if (USETPL)
                    return;
#else
                    continue;
#endif


                sqr.Piece.ValidMoves = new Stack<byte>(sqr.Piece.LastValidMoveCount);

                Interlocked.Increment(ref remainingPieces);

                switch (sqr.Piece.PieceType)
                {
                    case ChessPieceType.Pawn:
                        {
                            if (sqr.Piece.PieceColor == ChessPieceColor.White)
                            {
                                CheckValidMovesPawn(MoveArrays.WhitePawnMoves[x].Moves, sqr.Piece, (byte)x, MoveArrays.WhitePawnTotalMoves[x]);
                                break;
                            }
                            if (sqr.Piece.PieceColor == ChessPieceColor.Black)
                            {
                                CheckValidMovesPawn(MoveArrays.BlackPawnMoves[x].Moves, sqr.Piece, (byte)x, MoveArrays.BlackPawnTotalMoves[x]);
                                break;
                            }

                            break;
                        }
                    case ChessPieceType.Knight:
                        {
                            for (byte i = 0; i < MoveArrays.KnightTotalMoves[x]; i++)
                            {
                                AnalyzeMove(MoveArrays.KnightMoves[x].Moves[i], sqr.Piece);
                            }

                            break;
                        }
                    case ChessPieceType.Bishop:
                        {
                            for (byte i = 0; i < MoveArrays.BishopTotalMoves1[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.BishopMoves1[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.BishopTotalMoves2[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.BishopMoves2[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.BishopTotalMoves3[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.BishopMoves3[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.BishopTotalMoves4[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.BishopMoves4[x].Moves[i], sqr.Piece) == false) break;
                            }

                            break;
                        }
                    case ChessPieceType.Rook:
                        {
                            if (sqr.Piece.Moved)
                            {
                                if (sqr.Piece.PieceColor == ChessPieceColor.Black) blackRooksMoved++; else whiteRooksMoved++;
                            }

                            for (byte i = 0; i < MoveArrays.RookTotalMoves1[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.RookMoves1[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.RookTotalMoves2[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.RookMoves2[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.RookTotalMoves3[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.RookMoves3[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.RookTotalMoves4[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.RookMoves4[x].Moves[i], sqr.Piece) == false) break;
                            }

                            break;
                        }
                    case ChessPieceType.Queen:
                        {
                            for (byte i = 0; i < MoveArrays.QueenTotalMoves1[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves1[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.QueenTotalMoves2[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves2[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.QueenTotalMoves3[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves3[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.QueenTotalMoves4[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves4[x].Moves[i], sqr.Piece) == false) break;
                            }

                            for (byte i = 0; i < MoveArrays.QueenTotalMoves5[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves5[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.QueenTotalMoves6[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves6[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.QueenTotalMoves7[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves7[x].Moves[i], sqr.Piece) == false) break;
                            }
                            for (byte i = 0; i < MoveArrays.QueenTotalMoves8[x]; i++)
                            {
                                if (AnalyzeMove(MoveArrays.QueenMoves8[x].Moves[i], sqr.Piece) == false) break;
                            }

                            break;
                        }
                    case ChessPieceType.King:
                        {
                            if (sqr.Piece.PieceColor == ChessPieceColor.White)
                            {
                                if (sqr.Piece.Moved) WhiteCanCastle = false;
                                WhiteKingPosition = (byte)x;
                            }
                            else
                            {
                                if (sqr.Piece.Moved) BlackCanCastle = false;
                                BlackKingPosition = (byte)x;
                            }

                            break;
                        }
                }
            }
#if (USETPL)
            );
#endif

            if (blackRooksMoved > 1) BlackCanCastle = false;

            if (whiteRooksMoved > 1) WhiteCanCastle = false;

            if (remainingPieces < 10) EndGamePhase = true;


            if (WhoseMove == ChessPieceColor.White)
            {
                GenerateValidMovesKing(Squares[BlackKingPosition].Piece, BlackKingPosition);
                GenerateValidMovesKing(Squares[WhiteKingPosition].Piece, WhiteKingPosition);
            }
            else
            {
                GenerateValidMovesKing(Squares[WhiteKingPosition].Piece, WhiteKingPosition);
                GenerateValidMovesKing(Squares[BlackKingPosition].Piece, BlackKingPosition);
            }

            //Now that all the pieces were examined we know if the king is in check
            if (!WhiteCastled && WhiteCanCastle && !WhiteCheck) GenerateValidMovesKingCastle(Squares[WhiteKingPosition].Piece);

            if (!BlackCastled && BlackCanCastle && !BlackCheck) GenerateValidMovesKingCastle(Squares[BlackKingPosition].Piece);
        }
        #endregion
    }
}
