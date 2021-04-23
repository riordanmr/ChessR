using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessR1
{
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
