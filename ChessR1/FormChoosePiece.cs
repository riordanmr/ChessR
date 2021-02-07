using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessR1
{
    public enum PlacePieceAction { ACTION_PLACE_PIECE, ACTION_EMPTY, ACTION_CANCEL };

    public partial class FormChoosePiece : Form
    {
        private Brush brushBlack = new System.Drawing.SolidBrush(Color.Black);
        Font fontPieces2 = new Font("Arial", 32);
        public PlacePieceAction PlaceAction { get; set; }
        public int PlacePiece { get; set; }
        public int PlaceColor { get; set; }

        public FormChoosePiece() {
            InitializeComponent();
        }

        private void FormAction(PlacePieceAction action, int pieceType, int color) {
            PlaceAction = action;
            PlacePiece = pieceType;
            PlaceColor = color;
            Close();
        }

        private void FormChoosePiece_Load(object sender, EventArgs e) {
            PlaceAction = PlacePieceAction.ACTION_CANCEL;
        }

        private void labelWhiteKing_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.King, PieceColor.White);
        }

        private void labelWhiteQueen_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Queen, PieceColor.White);
        }

        private void labelWhiteRook_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Rook, PieceColor.White);
        }

        private void labelWhiteBishop_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Bishop, PieceColor.White);
        }

        private void labelWhiteKnight_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Knight, PieceColor.White);
        }

        private void labelWhitePawn_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Pawn, PieceColor.White);
        }

        private void labelBlackKing_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.King, PieceColor.Black);
        }

        private void labelBlackQueen_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Queen, PieceColor.Black);
        }

        private void labelBlackRook_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Rook, PieceColor.Black);
        }

        private void labelBlackBishop_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Bishop, PieceColor.Black);
        }

        private void labelBlackKnight_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Knight, PieceColor.Black);
        }

        private void labelBlackPawn_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_PLACE_PIECE, PieceType.Pawn, PieceColor.Black);
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_CANCEL, PieceType.Empty, PieceColor.White);
        }

        private void labelEmptySquare_Click(object sender, EventArgs e) {
            FormAction(PlacePieceAction.ACTION_EMPTY, PieceType.Empty, PieceColor.White);
        }
    }
}
