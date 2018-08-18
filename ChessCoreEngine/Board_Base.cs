using ChessCoreEngine.Utils;
using System;
using System.Collections.Generic;

namespace ChessEngine.Engine
{
    public partial class Board
    {
        static Logger logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal Square[] Squares;
              
        internal bool InsufficientMaterial;

        internal int Score;

        internal ulong ZobristHash;       
       
        //Game Over Flags
        internal bool BlackCheck;
        internal bool BlackMate;
        internal bool WhiteCheck;
        internal bool WhiteMate;
        internal bool StaleMate;

        internal byte FiftyMove;
        internal byte RepeatedMove;

        internal bool BlackCastled;
        internal bool WhiteCastled;

        internal bool BlackCanCastle;
        internal bool WhiteCanCastle;

        internal bool EndGamePhase;

        internal MoveContent LastMove;

        internal byte WhiteKingPosition;
        internal byte BlackKingPosition;

        internal bool[] BlackAttackBoard;
        internal bool[] WhiteAttackBoard;

        //Who initated En Passant
        internal ChessPieceColor EnPassantColor;
        //Positions liable to En Passant
        internal byte EnPassantPosition;

        internal ChessPieceColor WhoseMove;
        
        internal int MoveCount;

        #region Constructors

        //Default Constructor

        internal Board(string fen) : this()
        {
            byte index = 0;
            byte spc = 0;

            WhiteCastled = true;
            BlackCastled = true;

            byte spacers = 0;

            WhoseMove = ChessPieceColor.White;

            if (fen.Contains("a3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 40;
            }
            else if (fen.Contains("b3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 41;
            }
            else if (fen.Contains("c3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 42;
            }
            else if (fen.Contains("d3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 43;
            }
            else if (fen.Contains("e3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 44;
            }
            else if (fen.Contains("f3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 45;
            }
            else if (fen.Contains("g3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 46;
            }
            else if (fen.Contains("h3"))
            {
                EnPassantColor = ChessPieceColor.White;
                EnPassantPosition = 47;
            }


            if (fen.Contains("a6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition = 16;
            }
            else if (fen.Contains("b6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition = 17;
            }
            else if (fen.Contains("c6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition =18;
            }
            else if (fen.Contains("d6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition = 19;
            }
            else if (fen.Contains("e6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition = 20;
            }
            else if (fen.Contains("f6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition = 21;
            }
            else if (fen.Contains("g6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition = 22;
            }
            else if (fen.Contains("h6"))
            {
                EnPassantColor = ChessPieceColor.Black;
                EnPassantPosition = 23;
            }

            if (fen.Contains(" w "))
            {
                WhoseMove = ChessPieceColor.White;
            }
            if (fen.Contains(" b "))
            {
                WhoseMove = ChessPieceColor.Black;
            }

            foreach (char c in fen)
            {
 
                if (index < 64 && spc == 0)
                {
                    if (c == '1' && index < 63)
                    {
                        index++;
                    }
                    else if (c == '2' && index < 62)
                    {
                        index += 2;
                    }
                    else if (c == '3' && index < 61)
                    {
                        index += 3;
                    }
                    else if (c == '4' && index < 60)
                    {
                        index += 4;
                    }
                    else if (c == '5' && index < 59)
                    {
                        index += 5;
                    }
                    else if (c == '6' && index < 58)
                    {
                        index += 6;
                    }
                    else if (c == '7' && index < 57)
                    {
                        index += 7;
                    }
                    else if (c == '8' && index < 56)
                    {
                        index += 8;
                    }
                    else if (c == 'P')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Pawn, ChessPieceColor.White);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'N')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Knight, ChessPieceColor.White);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'B')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Bishop, ChessPieceColor.White);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'R')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Rook, ChessPieceColor.White);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'Q')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Queen, ChessPieceColor.White);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'K')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.King, ChessPieceColor.White);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'p')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Pawn, ChessPieceColor.Black);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'n')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Knight, ChessPieceColor.Black);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'b')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Bishop, ChessPieceColor.Black);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'r')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Rook, ChessPieceColor.Black);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'q')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.Queen, ChessPieceColor.Black);
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == 'k')
                    {
                        Squares[index].Piece = new Piece(ChessPieceType.King, ChessPieceColor.Black);      
                        Squares[index].Piece.Moved = true;
                        index++;
                    }
                    else if (c == '/')
                    {
                        continue;
                    }
                    else if (c == ' ')
                    {
                        spc++;
                    }
                }
                else
                {
                    
                    if (c == 'K')
                    {
                        if (Squares[60].Piece != null)
                        {
                            if (Squares[60].Piece.PieceType == ChessPieceType.King)
                            {
                                Squares[60].Piece.Moved = false;
                            }
                        }

                        if (Squares[63].Piece != null)
                        {
                            if (Squares[63].Piece.PieceType == ChessPieceType.Rook)
                            {
                                Squares[63].Piece.Moved = false;
                            }
                        }

                        WhiteCastled = false;
                        
                    }
                    else if (c == 'Q')
                    {
                        if (Squares[60].Piece != null)
                        {
                            if (Squares[60].Piece.PieceType == ChessPieceType.King)
                            {
                                Squares[60].Piece.Moved = false;
                            }
                        }

                        if (Squares[56].Piece != null)
                        {
                            if (Squares[56].Piece.PieceType == ChessPieceType.Rook)
                            {
                                Squares[56].Piece.Moved = false;
                            }
                        }

                        WhiteCastled = false;
                    }
                    else if (c == 'k')
                    {
                        if (Squares[4].Piece != null)
                        {
                            if (Squares[4].Piece.PieceType == ChessPieceType.King)
                            {
                                Squares[4].Piece.Moved = false;
                            }
                        }

                        if (Squares[7].Piece != null)
                        {
                            if (Squares[7].Piece.PieceType == ChessPieceType.Rook)
                            {
                                Squares[7].Piece.Moved = false;
                            }
                        }

                        BlackCastled = false;
                    }
                    else if (c == 'q')
                    {
                        if (Squares[4].Piece != null)
                        {
                            if (Squares[4].Piece.PieceType == ChessPieceType.King)
                            {
                                Squares[4].Piece.Moved = false;
                            }
                        }

                        if (Squares[0].Piece != null)
                        {
                            if (Squares[0].Piece.PieceType == ChessPieceType.Rook)
                            {
                                Squares[0].Piece.Moved = false;
                            }
                        }

                        BlackCastled = false;
                    }
                    else if (c == ' ')
                    {
                        spacers++;
                    }
                    else if (c == '1' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 1);
                    }
                    else if (c == '2' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 2);
                    }
                    else if (c == '3' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 3);
                    }
                    else if (c == '4' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 4);
                    }
                    else if (c == '5' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 5);
                    }
                    else if (c == '6' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 6);
                    }
                    else if (c == '7' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 7);
                    }
                    else if (c == '8' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 8);
                    }
                    else if (c == '9' && spacers == 4)
                    {
                        FiftyMove = (byte)((FiftyMove * 10) + 9);
                    }
                    else if (c == '0' && spacers == 4)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 0);
                    }
                    else if (c == '1' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 1);
                    }
                    else if (c == '2' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 2);
                    }
                    else if (c == '3' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 3);
                    }
                    else if (c == '4' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 4);
                    }
                    else if (c == '5' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 5);
                    }
                    else if (c == '6' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 6);
                    }
                    else if (c == '7' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 7);
                    }
                    else if (c == '8' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 8);
                    }
                    else if (c == '9' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 9);
                    }
                    else if (c == '0' && spacers == 5)
                    {
                        MoveCount = (byte)((MoveCount * 10) + 0);
                    }

                    

                }
            }
     
        }

        internal Board()
        {
            Squares = new Square[64];

            for (byte i = 0; i < 64; i++)
            {
                Squares[i] = new Square();
            }

            LastMove = new MoveContent();

            BlackCanCastle = true;
            WhiteCanCastle = true;
            
            WhiteAttackBoard = new bool[64];
            BlackAttackBoard = new bool[64];

        }

        private Board(Square[] squares)
        {
            Squares = new Square[64];

            for (byte x = 0; x < 64; x++)
            {
                if (squares[x].Piece != null)
                {
                    Squares[x].Piece = new Piece(squares[x].Piece);
                }
            }

            

            WhiteAttackBoard = new bool[64];
            BlackAttackBoard = new bool[64];

        }

        //Constructor
        internal Board(int score) : this()
        {
            Score = score;

            WhiteAttackBoard = new bool[64];
            BlackAttackBoard = new bool[64];

        }

        //Copy Constructor
        internal Board(Board board)
        {
            Squares = new Square[64];

            for (byte x = 0; x < 64; x++)
            {
                if (board.Squares[x].Piece != null)
                {
                    Squares[x] = new Square(board.Squares[x].Piece);
                }
            }

            WhiteAttackBoard = new bool[64];
            BlackAttackBoard = new bool[64];

            for (byte x = 0; x < 64; x++)
            {
                WhiteAttackBoard[x] = board.WhiteAttackBoard[x];
                BlackAttackBoard[x] = board.BlackAttackBoard[x];
            }

            EndGamePhase = board.EndGamePhase;

            FiftyMove = board.FiftyMove;
            RepeatedMove = board.RepeatedMove;
           
            WhiteCastled = board.WhiteCastled;
            BlackCastled = board.BlackCastled;

            WhiteCanCastle = board.WhiteCanCastle;
            BlackCanCastle = board.BlackCanCastle;

            WhiteKingPosition = board.WhiteKingPosition;
            BlackKingPosition = board.BlackKingPosition;

            BlackCheck = board.BlackCheck;
            WhiteCheck = board.WhiteCheck;
            StaleMate = board.StaleMate;
            WhiteMate = board.WhiteMate;
            BlackMate = board.BlackMate;
            WhoseMove = board.WhoseMove;
            EnPassantPosition = board.EnPassantPosition;
            EnPassantColor = board.EnPassantColor;

            ZobristHash = board.ZobristHash;

            Score = board.Score;

            LastMove = new MoveContent(board.LastMove);

            MoveCount = board.MoveCount;
        }

        #endregion

        #region PrivateMethods

        private bool PromotePawns(Piece piece, byte dstPosition, ChessPieceType promoteToPiece)
        {
            if (piece.PieceType == ChessPieceType.Pawn)
            {
                if (dstPosition < 8)
                {
                    Squares[dstPosition].Piece.PieceType = promoteToPiece;
                    Squares[dstPosition].Piece.PieceValue = Piece.CalculatePieceValue(promoteToPiece);
                    Squares[dstPosition].Piece.PieceActionValue = Piece.CalculatePieceActionValue(promoteToPiece);
                    return true;
                }
                if (dstPosition > 55)
                {
                    Squares[dstPosition].Piece.PieceType = promoteToPiece;
                    Squares[dstPosition].Piece.PieceValue = Piece.CalculatePieceValue(promoteToPiece);
                    Squares[dstPosition].Piece.PieceActionValue = Piece.CalculatePieceActionValue(promoteToPiece);
                    return true;
                }
            }

            return false;
        }

        private void RecordEnPassant(ChessPieceColor pcColor, ChessPieceType pcType, byte srcPosition, byte dstPosition)
        {
            //Record En Passant if Pawn Moving
            if (pcType == ChessPieceType.Pawn)
            {
                //Reset FiftyMoveCount if pawn moved
                FiftyMove = 0;

                int difference = srcPosition - dstPosition; 

                if (difference == 16 || difference == -16)
                {
                    EnPassantPosition = (byte)(dstPosition + (difference / 2));
                    EnPassantColor = pcColor;
                }
            }
        }

        private bool SetEnpassantMove(byte srcPosition, byte dstPosition, ChessPieceColor pcColor)
        {
            if (EnPassantPosition != dstPosition) return false;

            if (pcColor == EnPassantColor) return false;
            
            if (Squares[srcPosition].Piece.PieceType != ChessPieceType.Pawn) return false;

            int pieceLocationOffset = 8;

            if (EnPassantColor == ChessPieceColor.White) pieceLocationOffset = -8;

            dstPosition = (byte)(dstPosition + pieceLocationOffset);

            Square sqr = Squares[dstPosition];

            LastMove.TakenPiece = new PieceTaken(sqr.Piece.PieceColor, sqr.Piece.PieceType, sqr.Piece.Moved, dstPosition);

            Squares[dstPosition].Piece = null;
                    
            //Reset FiftyMoveCount if capture
            FiftyMove = 0;

            return true;

        }

        private void KingCastle(Piece piece, byte srcPosition, byte dstPosition)
        {
            if (piece.PieceType != ChessPieceType.King) return;

            //Lets see if this is a casteling move.
            if (piece.PieceColor == ChessPieceColor.White && srcPosition == 60)
            {
                //Castle Right
                if (dstPosition == 62)
                {
                    //Ok we are casteling we need to move the Rook
                    if (Squares[63].Piece != null)
                    {
                        Squares[61].Piece = Squares[63].Piece;
                        Squares[63].Piece = null;
                        WhiteCastled = true;
                        LastMove.MovingPieceSecondary = new PieceMoving(Squares[61].Piece.PieceColor, Squares[61].Piece.PieceType, Squares[61].Piece.Moved, 63, 61);
                        Squares[61].Piece.Moved = true;
                        return;
                    }
                }
                //Castle Left
                else if (dstPosition == 58)
                {   
                    //Ok we are casteling we need to move the Rook
                    if (Squares[56].Piece != null)
                    {
                        Squares[59].Piece = Squares[56].Piece;
                        Squares[56].Piece = null;
                        WhiteCastled = true;
                        LastMove.MovingPieceSecondary = new PieceMoving(Squares[59].Piece.PieceColor, Squares[59].Piece.PieceType, Squares[59].Piece.Moved, 56, 59);
                        Squares[59].Piece.Moved = true;
                        return;
                    }
                }
            }
            else if (piece.PieceColor == ChessPieceColor.Black && srcPosition == 4)
            {
                if (dstPosition == 6)
                {
                    //Ok we are casteling we need to move the Rook
                    if (Squares[7].Piece != null)
                    {
                        Squares[5].Piece = Squares[7].Piece;
                        Squares[7].Piece = null;
                        BlackCastled = true;
                        LastMove.MovingPieceSecondary = new PieceMoving(Squares[5].Piece.PieceColor, Squares[5].Piece.PieceType, Squares[5].Piece.Moved, 7, 5);
                        Squares[5].Piece.Moved = true;
                        return;
                    }
                }
                    //Castle Left
                else if (dstPosition == 2)
                {
                    //Ok we are casteling we need to move the Rook
                    if (Squares[0].Piece != null)
                    {
                        Squares[3].Piece = Squares[0].Piece;
                        Squares[0].Piece = null;
                        BlackCastled = true;
                        LastMove.MovingPieceSecondary = new PieceMoving(Squares[3].Piece.PieceColor, Squares[3].Piece.PieceType, Squares[3].Piece.Moved, 0, 3);
                        Squares[3].Piece.Moved = true;
                        return;
                    }
                }
            }

            return;
        }

        #endregion

        #region InternalMethods

        //Fast Copy
        internal Board FastBoardCopy()
        {
            Board clonedBoard = new Board(Squares);

            clonedBoard.EndGamePhase = EndGamePhase;
            clonedBoard.WhoseMove = WhoseMove;
            clonedBoard.MoveCount = MoveCount;
            clonedBoard.FiftyMove = FiftyMove;
            clonedBoard.ZobristHash = ZobristHash;
            clonedBoard.BlackCastled = BlackCastled;
            clonedBoard.WhiteCastled = WhiteCastled;

            clonedBoard.WhiteCanCastle = WhiteCanCastle;
            clonedBoard.BlackCanCastle = BlackCanCastle;

            WhiteAttackBoard = new bool[64];
            BlackAttackBoard = new bool[64];

            return clonedBoard;
        }

        internal MoveContent MovePiece(byte srcPosition, byte dstPosition, ChessPieceType promoteToPiece)
        {
            Piece piece = Squares[srcPosition].Piece;

            //Record my last move
            LastMove = new MoveContent();

            

            if (piece.PieceColor == ChessPieceColor.Black)
            {
                MoveCount++;
                //Add One to FiftyMoveCount to check for tie.
                FiftyMove++;
            }

            //En Passant
            if (EnPassantPosition > 0)
            {
                LastMove.EnPassantOccured = SetEnpassantMove(srcPosition, dstPosition, piece.PieceColor);
            }

            if (!LastMove.EnPassantOccured)
            {
                Square sqr = Squares[dstPosition];

                if (sqr.Piece != null)
                {
                    LastMove.TakenPiece = new PieceTaken(sqr.Piece.PieceColor, sqr.Piece.PieceType, sqr.Piece.Moved, dstPosition);
                    FiftyMove = 0;
                }
                else
                {
                    LastMove.TakenPiece = new PieceTaken(ChessPieceColor.White, ChessPieceType.None, false, dstPosition);
                    
                }
            }

            LastMove.MovingPiecePrimary = new PieceMoving(piece.PieceColor, piece.PieceType, piece.Moved, srcPosition, dstPosition);

            //Delete the piece in its source position
            Squares[srcPosition].Piece = null;
      
            //Add the piece to its new position
            piece.Moved = true;
            piece.Selected = false;
            Squares[dstPosition].Piece = piece;

            //Reset EnPassantPosition
            EnPassantPosition = 0;
          
            //Record En Passant if Pawn Moving
            if (piece.PieceType == ChessPieceType.Pawn)
            {
               FiftyMove = 0;
               RecordEnPassant(piece.PieceColor, piece.PieceType, srcPosition, dstPosition);
            }

            WhoseMove = WhoseMove == ChessPieceColor.White ? ChessPieceColor.Black : ChessPieceColor.White;

            KingCastle(piece, srcPosition, dstPosition);

            //Promote Pawns 
            if (PromotePawns(piece, dstPosition, promoteToPiece))
            {
                LastMove.PawnPromotedTo = promoteToPiece;
            }
            else
            {
                LastMove.PawnPromotedTo = ChessPieceType.None;
            }

            if ( FiftyMove >= 50)
            {
                StaleMate = true;
            }

            return LastMove;
        }

        private static string GetColumnFromByte(byte column)
        {
            switch (column)
            {
                case 0:
                    return "a";
                case 1:
                    return "b";
                case 2:
                    return "c";
                case 3:
                    return "d";
                case 4:
                    return "e";
                case 5:
                    return "f";
                case 6:
                    return "g";
                case 7:
                    return "h";
                default:
                    return "a";
            }
        }

        public new string ToString() => Fen(false);

        public string Fen(bool boardOnly)
        {
            string output = String.Empty;
            byte blankSquares = 0;

            for (byte x = 0; x < 64; x++)
            {
                byte index = x;

                if (Squares[index].Piece != null)
                {
                    if (blankSquares > 0)
                    {
                        output += blankSquares.ToString();
                        blankSquares = 0;
                    }

                    if (Squares[index].Piece.PieceColor == ChessPieceColor.Black)
                    {
                        output += Piece.GetPieceTypeShort(Squares[index].Piece.PieceType).ToLower();
                    }
                    else
                    {
                        output += Piece.GetPieceTypeShort(Squares[index].Piece.PieceType);
                    }
                }
                else
                {
                    blankSquares++;
                }

                if (x % 8 == 7)
                {
                    if (blankSquares > 0)
                    {
                        output += blankSquares.ToString();
                        output += "/";
                        blankSquares = 0;
                    }
                    else
                    {
                        if (x > 0 && x != 63)
                        {
                            output += "/";
                        }
                    }
                }
            }

            if (WhoseMove == ChessPieceColor.White)
            {
                output += " w ";
            }
            else
            {
                output += " b ";
            }

            string spacer = "";

            if (WhiteCastled == false)
            {
                if (Squares[60].Piece != null)
                {
                    if (Squares[60].Piece.Moved == false)
                    {
                        if (Squares[63].Piece != null)
                        {
                            if (Squares[63].Piece.Moved == false)
                            {
                                output += "K";
                                spacer = " ";
                            }
                        }
                        if (Squares[56].Piece != null)
                        {
                            if (Squares[56].Piece.Moved == false)
                            {
                                output += "Q";
                                spacer = " ";
                            }
                        }
                    }
                }
            }

            if (BlackCastled == false)
            {
                if (Squares[4].Piece != null)
                {
                    if (Squares[4].Piece.Moved == false)
                    {
                        if (Squares[7].Piece != null)
                        {
                            if (Squares[7].Piece.Moved == false)
                            {
                                output += "k";
                                spacer = " ";
                            }
                        }
                        if (Squares[0].Piece != null)
                        {
                            if (Squares[0].Piece.Moved == false)
                            {
                                output += "q";
                                spacer = " ";
                            }
                        }
                    }
                }

                
            }

            if (output.EndsWith("/")) output.TrimEnd('/');


            if (EnPassantPosition != 0)
            {
                output += spacer + GetColumnFromByte((byte)(EnPassantPosition % 8)) + "" + (byte)(8 - (byte)(EnPassantPosition / 8)) + " ";
            }
            else
            {
                output += spacer + "- ";
            }

            if (!boardOnly)
            {
                output += FiftyMove + " ";
                output += MoveCount + 1;
            }
            return output.Trim();
        }

        #endregion
    }
}