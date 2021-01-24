// ChessR1 - Program to play a rudimentary game of chess.
// This is my first chess program.  I am going to start with a GUI
// so the program is easier to debug once I actually start implementing
// the chess engine.
// I'm starting this as a .NET WinForms app for familiarity, but that 
// will not necessarily be its final form. 
// Mark Riordan  23-JAN-2021

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
    public partial class ChessR1Form : Form
    {
        private Brush brushBlack = new System.Drawing.SolidBrush(Color.Black);
        private Brush brushWhite = new SolidBrush(Color.White);
        float offsetTop = 90.0F;
        float offsetLeft = 90.0F;
        float squareSize = 120.0F;
        float thickness;
        System.Drawing.Pen penBlack;
        Font fontPieces = new Font("Arial", 60);


        public ChessR1Form() {
            InitializeComponent();
            thickness = (float)(squareSize * 0.04);
            penBlack = new System.Drawing.Pen(System.Drawing.Color.Black, 1*thickness);
        }

        private void ChessR1Form_Load(object sender, EventArgs e) {

        }

        // Drawing a pawn by computing all the geometric shapes, and trying to 
        // have a border (by following Draw with Fill) is really hard.
        // And the results look surprisingly grainy.  I may abandon this.
        private void DrawPawn(Graphics g, int irow, int icol) {
            float x = (float) (offsetLeft + icol * squareSize + 0.42 * squareSize);
            float y = (float) (offsetTop + irow * squareSize + 0.25 * squareSize);
            float diameter = (float)(squareSize * 0.2);
            
            g.DrawEllipse(penBlack, x, y, diameter, diameter);
            g.FillEllipse(brushWhite, (float)(x + 0.5*thickness), (float)(y + 0.5*thickness),
                (float)(diameter - 1*thickness), (float)(diameter - 1*thickness));
            PointF[] pointsTriangle = new PointF[3];

            x = (float)(offsetLeft + icol * squareSize + 0.5 * squareSize);
            y = (float)(offsetTop + irow * squareSize + 0.4 * squareSize);
            pointsTriangle[0] = new PointF(x, y);
            x = (float)(offsetLeft + icol * squareSize + 0.3 * squareSize);
            y = (float)(offsetTop + irow * squareSize + 0.52 * squareSize);
            pointsTriangle[1] = new PointF(x, y);
            x = (float)(offsetLeft + icol * squareSize + 0.7 * squareSize);
            y = (float)(offsetTop + irow * squareSize + 0.52 * squareSize);
            pointsTriangle[2] = new PointF(x, y);

            g.DrawPolygon(penBlack, pointsTriangle);

        }

        private void DrawPiece(Graphics g, int piece, int irow, int icol) {
            string strPieces = "♚♛♜♝♞♟♔♕♖♗♘♙";
            string strPiece = strPieces.Substring(piece, 1);
            SizeF textSize = g.MeasureString(strPiece, fontPieces);
            PointF textSizeF = textSize.ToPointF();
            float x = (float)(offsetLeft + squareSize * icol + 0.0*textSizeF.X);
            float y = (float)(offsetTop + squareSize * irow + 0.2*textSizeF.Y);
            g.DrawString(strPiece, fontPieces, brushBlack, x, y);
        }

        private void DrawSquares(Graphics g) {
            Color colorLight = Color.FromName("cornsilk"); // moccasin papayawhip
            Brush brushLight = new System.Drawing.SolidBrush(colorLight);
            Color colorDark = Color.FromName("moccasin"); // peru tan
            Brush brushDark = new System.Drawing.SolidBrush(colorDark);

            for (int irow = 0; irow < 8; irow++) {
                for (int icol = 0; icol < 8; icol++) {
                    Brush brush = ((irow + icol) % 2) == 0 ? brushLight : brushDark;
                    g.FillRectangle(brush, offsetLeft + squareSize * icol, offsetTop + squareSize * irow, squareSize, squareSize);
                }
            }
        }

        private void ChessR1Form_Paint(object sender, PaintEventArgs e) {
            // Drawing pieces by using the Unicode characters for chess pieces isn't great.
            // The first 6 pieces are more or less hollow and the next 6 are filled-in.
            // But they don't look quite different enough from each other for me.
            // Drawing the white pieces in a white color looks bad.
            // Also, I don't especially like the design of the pieces anyway.
            // The best bet if going with the Unicode approach is probably to 
            // stick with a black color for all and hope that the hollow ones look white enough.
            // Although I don't think the hollow ones do look white enough.
            // See https://developer.mozilla.org/en-US/docs/Web/CSS/color_value for the available colors.
            //
            // Update:  They look better with this in App.config:
            //   <System.Windows.Forms.ApplicationConfigurationSection>
            //     <add key="DpiAwareness" value="PerMonitorV2" />
            //   </System.Windows.Forms.ApplicationConfigurationSection>

            //string fontColorName = "moccasin";  // "antiquewhite"
            //BackColor = Color.FromName(fontColorName);

            DrawSquares(e.Graphics);
            DrawPawn(e.Graphics, 1, 1);

            int piece = 0;
            for (int irow = 0; irow < 5; irow++) {
                for (int icol = 0; icol < 8; icol++) {
                    DrawPiece(e.Graphics, piece, irow, icol);
                    piece++;
                    if (piece > 11) piece = 0;
                }
            }

            //Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            //e.Graphics.DrawString("♔♕♖♗♘♙ ♚♛♜♝♞♟", font, brush, 120.0F, 130.0F);
            //Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            e.Graphics.DrawString("♔♕♖♗♘♙ ♚♛♜♝♞♟", fontPieces, brushBlack, 
                (float)(offsetLeft + 0.15*squareSize), (float)(offsetTop + 6.15*squareSize));
            e.Graphics.DrawString("♔♕♖♗♘♙ ♚♛♜♝♞♟", fontPieces, brushBlack,
                (float)(offsetLeft + 0.15 * squareSize), (float)(offsetTop + 6.15 * squareSize));
        }
    }
}
