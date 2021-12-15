using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessR1
{
    public class CHESSCONST
    {
        public const int NUMROWS = 8;
        public const int NUMCOLS = 8;
        public const int COLOR0OR1WHITE = 0;
        public const int COLOR0OR1BLACK = 1;
    };



    public class PieceType
    {
        public const int Empty = 0;
        public const int King = 1; public const int Queen = 2; public const int Rook = 3;
        public const int Bishop = 4; public const int Knight = 5; public const int Pawn = 6;
        public const int Mask = 7;
        static string[] aryNames = { "Empty", "King", "Queen", "Rook", "Bishop", "Knight", "Pawn" };
        public static string ToString(int pieceType) {
            if (pieceType >= 0 && pieceType <= Pawn) {
                return aryNames[pieceType];
            } else {
                return "Bad";
            }
        }
    };

    public class PieceColor
    {
        public const int White = 0; public const int Black = 8;
        public const int Mask = 8;
        public static string ToString(int color) {
            if (White == color) {
                return "White";
            } else if (Black == color) {
                return "Black";
            } else {
                return "Unknown";
            }
        }
    };

    struct BoardSaveState 
    {
        public byte savedStart;
        public byte savedStop;
        public byte savedRookQueen;
        public byte savedRookKing;
        public byte savedBishopKing;
        public byte savedQueen;
        public bool savedCastleKing0;
        public bool savedCastleKing1;
        public bool savedCastleQueen0;
        public bool savedCastleQueen1;
        public int savedNCapturedPieces;
    }

    public class Board
    {
        // cells is indexed by row (0-7; internal row 0 = rank 8), col (0-7; internal col 0 = file a).
        // The mapping between row/col and algebraic location on the board is fixed as above,
        // regardless of who is playing white and whether the white pieces are displayed on the bottom.
        public byte[,] cells = new byte[8, 8];
        // capturedPieces contains the pieces that have been captured so far, in order.
        // White and Black are intermingled.
        public byte[] capturedPieces = new byte[32];
        // The actual number of current captured pieces.
        public int nCapturedPieces = 0;
        //public bool BlackOnBottom = false;
        // The indices to the OKCastle arrays are 0 for white and 1 for black.
        public bool[] bOKCastleQueen = new bool[] { true, true };
        public bool[] bOKCastleKing = new bool[] { true, true };
        public uint cachedWhiteKingPosition = 0;
        public uint cachedBlackKingPosition = 0;

        // Represent the current position in FEN: Forsyth–Edwards Notation.
        // See https://en.wikipedia.org/wiki/Forsyth–Edwards_Notation.
        public string PositionAsFEN(int colorToMove)
        {
            const string FENLetters = " KQRBNP  kqrbnpz"; //" ♔♕♖♗♘♙ ♚♛♜♝♞♟";
            string fen = "";
            // Part 1:
            // Piece placement (from White's perspective). Each rank is described, starting with rank 8
            // and ending with rank 1; within each rank, the contents of each square are described from
            // file "a" through file "h". Following the Standard Algebraic Notation (SAN), each piece is
            // identified by a single letter taken from the standard English names
            // (pawn = "P", knight = "N", bishop = "B", rook = "R", queen = "Q" and king = "K").
            // White pieces are designated using upper-case letters ("PNBRQK") while black pieces use
            // lowercase ("pnbrqk"). Empty squares are noted using digits 1 through 8 (the number of
            // empty squares), and "/" separates ranks.
            for (int irow = 0; irow <= CHESSCONST.NUMROWS - 1; irow++) {
                int nConseqEmpty = 0;
                for (int icol = 0; icol < CHESSCONST.NUMCOLS; icol++) {
                    byte piece = cells[irow, icol];
                    if (0 == piece) {
                        nConseqEmpty++;
                    } else {
                        if (nConseqEmpty > 0) {
                            fen += nConseqEmpty.ToString();
                            nConseqEmpty = 0;
                        }
                        fen += FENLetters.Substring(piece, 1);
                    }
                }
                if (nConseqEmpty > 0) {
                    fen += nConseqEmpty.ToString();
                }
                if(irow < CHESSCONST.NUMROWS - 1) fen += "/";
            }
            fen += " ";

            // Part 2: Active color. "w" means White moves next, "b" means Black moves next.
            fen += (PieceColor.White == colorToMove ? "w" : "b");
            fen += " ";

            // Part 3: Castling availability. If neither side can castle, this is "-". Otherwise, this has
            // one or more letters: "K" (White can castle kingside), "Q" (White can castle queenside),
            // "k" (Black can castle kingside), and/or "q" (Black can castle queenside).
            // A move that temporarily prevents castling does not negate this notation.
            string castle = "";
            if (bOKCastleKing[CHESSCONST.COLOR0OR1WHITE]) castle += "K";
            if (bOKCastleQueen[CHESSCONST.COLOR0OR1WHITE]) castle += "Q";
            if (bOKCastleKing[CHESSCONST.COLOR0OR1BLACK]) castle += "k";
            if (bOKCastleQueen[CHESSCONST.COLOR0OR1BLACK]) castle += "q";
            if(castle.Length>0) {
                fen += castle;
            } else {
                fen += "-";
            }
            fen += " ";

            // Part 4:  En passant target square in algebraic notation. If there's no en passant target square,
            // this is "-". If a pawn has just made a two-square move, this is the position "behind" the pawn.
            // This is recorded regardless of whether there is a pawn in position to make an en passant capture.
            string enpassant = "-";
            fen += enpassant;
            fen += " ";

            // Part 5:  Halfmove clock: The number of halfmoves since the last capture or pawn advance,
            // used for the fifty-move rule.
            fen += 0.ToString();
            fen += " ";

            // Part 6:  Fullmove number: The number of the full move. It starts at 1, and is incremented
            // after Black's move.
            fen += 1.ToString();

            return fen;
        }

        public void SaveAsFEN(string filename, int colorToMove) {
            string fen = PositionAsFEN(colorToMove);
            File.WriteAllText(filename, fen);
        }
    };

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChessR1Form());
        }
    }
}
