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

    public class Board
    {
        // cells is indexed by row (0-7), col (0-7)
        public byte[,] cells = new byte[8, 8];
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
