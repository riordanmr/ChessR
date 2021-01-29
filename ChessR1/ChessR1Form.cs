// ChessR1 - Program to play a rudimentary game of chess.
// This is my first chess program.  I am going to start with a GUI
// so the program is easier to debug once I actually start implementing
// the chess engine.
// I'm starting this as a .NET WinForms app for familiarity, but that 
// will not necessarily be its final form. 
// Mark Riordan  23-JAN-2021

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChessR1
{
    public partial class ChessR1Form : Form
    {
        private Brush brushBlack = new System.Drawing.SolidBrush(Color.Black);
        private Brush brushWhite = new SolidBrush(Color.White);
        Brush brushBoardLight = new SolidBrush(Color.FromName("cornsilk"));
        Brush brushBoardDark = new SolidBrush(Color.FromName("moccasin"));
        float offsetTop = 90.0F;
        float offsetLeft = 90.0F;
        float squareSize = 120.0F;
        float thickness;
        System.Drawing.Pen penBlack;
        Pen penSelectedStart = new Pen(Color.MediumSpringGreen, 2.0F);
        Pen penLegalMoves = new Pen(Color.Plum, 2.0F);
        // I'm using Unicode characters to render the pieces.  
        // I don't think that all the different fonts each has a unique rendering of 
        // the pieces, but I can see that at least Arial and Segoe UI Symbol do.
        // Unfortunately, the center points of the characters differ between those
        // fonts, so you can't just swap in one font for another and have it look good. 
        Font fontPieces = new Font("Arial", 60);
        Font fontCoords = new Font("Lucida Console", 24);
        Board m_board = new Board();
        const int NUMROWS = 8;
        const int NUMCOLS = 8;
        const int NOT_SELECTED = -1;
        int selectedRowStart = NOT_SELECTED, selectedColStart = NOT_SELECTED;
        byte[] m_ValidMoves = new byte[64];
        int m_nValidMoves;
        bool bWhiteOnBottom = true;


        public ChessR1Form() {
            InitializeComponent();
            thickness = (float)(squareSize * 0.04);
            penBlack = new System.Drawing.Pen(System.Drawing.Color.Black, 1 * thickness);
        }

        public void DebugOut(string msg) {
            System.Diagnostics.Trace.WriteLine(msg);
        }

        private void ChessR1Form_Load(object sender, EventArgs e) {

        }

        // Combine a row and column number into a single integer.
        byte CalcByteFromRowAndCol(int irow, int icol) {
            return (byte)(NUMCOLS * icol + irow);
        }

        void CalcRowAndColFromByte(byte rowcol, out int irow, out int icol) {
            irow = rowcol & (NUMCOLS - 1);
            icol = rowcol / NUMCOLS;
        }

        int ColorOfCell(byte cell) {
            return (cell & (int)PieceColor.Mask);
        }

        // Drawing a pawn by computing all the geometric shapes, and trying to 
        // have a border (by following Draw with Fill) is really hard.
        // And the results look surprisingly grainy.  
        // I have abandoned this approach for now.
        private void DrawPawnBad(Graphics g, int irow, int icol) {
            float x = (float)(offsetLeft + icol * squareSize + 0.42 * squareSize);
            float y = (float)(offsetTop + irow * squareSize + 0.25 * squareSize);
            float diameter = (float)(squareSize * 0.2);

            g.DrawEllipse(penBlack, x, y, diameter, diameter);
            g.FillEllipse(brushWhite, (float)(x + 0.5 * thickness), (float)(y + 0.5 * thickness),
                (float)(diameter - 1 * thickness), (float)(diameter - 1 * thickness));
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

        /// <summary>
        /// Draw a single piece on the board.
        /// </summary>
        /// <param name="g">A Graphics object</param>
        /// <param name="pieceWithColor">A combination of PieceType and PieceColor</param>
        /// <param name="irow">The row, 0-7</param>
        /// <param name="icol">The column 0-7</param>
        private void DrawPiece(Graphics g, int pieceWithColor, int irow, int icol) {
            string strPieces = " ♔♕♖♗♘♙  ♚♛♜♝♞♟  ";
            string strPiece = strPieces.Substring(pieceWithColor, 1);
            SizeF textSize = g.MeasureString(strPiece, fontPieces);
            PointF textSizeF = textSize.ToPointF();
            float x = (float)(offsetLeft + squareSize * icol + 0.0 * textSizeF.X);
            float y = (float)(offsetTop + squareSize * irow + 0.2 * textSizeF.Y);
            g.DrawString(strPiece, fontPieces, brushBlack, x, y);
        }

        // Draw the squares of the board. 
        // This should be done before drawing the pieces and highlights, as
        // those are drawn on top of the squares.
        private void DrawSquares(Graphics g) {
            for (int irow = 0; irow < 8; irow++) {
                for (int icol = 0; icol < 8; icol++) {
                    Brush brush = ((irow + icol) % 2) == 0 ? brushBoardLight : brushBoardDark;
                    g.FillRectangle(brush, offsetLeft + squareSize * icol, offsetTop + squareSize * irow, squareSize, squareSize);
                }
            }
        }

        // Draw the algebraic notation coordinates on the sides of the board.
        void DrawCoordinates(Graphics g) {
            SizeF textSize = g.MeasureString("a", fontCoords);
            string strLetters = "abcdefgh";
            for (int icol = 0; icol < 8; icol++) {
                float x = (float)(offsetLeft + squareSize * (icol + 0.5) - textSize.Width * 0.5);
                float y = (float)(offsetTop + squareSize * 8.1);
                g.DrawString(strLetters.Substring(icol, 1), fontCoords, brushWhite, x, y);
            }

            string strNumbers = "12345678";
            for (int irow = 0; irow < 8; irow++) {
                float x = (float)(offsetLeft - squareSize * 0.4);
                float y = (float)(offsetTop + squareSize * (irow + 0.5) - textSize.Height * 0.5);
                g.DrawString(strNumbers.Substring(7 - irow, 1), fontCoords, brushWhite, x, y);
            }
        }

        // Draw colored rectangles in the squares we want to highlight.
        // The square currently selected by the user (if any) gets a green rectangle.
        void DrawHighlights(Graphics g) {
            // Highlight the currently selected square, if any.
            if (selectedRowStart >= 0 && selectedColStart >= 0) {
                float x = (float)(offsetLeft + (selectedColStart + 0.05) * squareSize);
                float y = (float)(offsetTop + (selectedRowStart + 0.05) * squareSize);
                float width = (float)(squareSize * 0.9);
                float height = width;
                g.DrawRectangle(penSelectedStart, x, y, width, height);
            }

            // Highlight the squares to which the currently selected piece can move.
            int irow, icol;
            for (int idx = 0; idx < m_nValidMoves; idx++) {
                CalcRowAndColFromByte(m_ValidMoves[idx], out irow, out icol);
                float x = (float)(offsetLeft + (icol + 0.05) * squareSize);
                float y = (float)(offsetTop + (irow + 0.05) * squareSize);
                float width = (float)(squareSize * 0.9);
                float height = width;
                g.DrawRectangle(penLegalMoves, x, y, width, height);
            }
        }

        int ComputeLegalMovesForPawn(int irow, int icol, ref byte[] aryValidMoves) {
            int nMoves = 0;

            int piece = m_board.cells[irow, icol];
            int myColor = piece & PieceColor.Mask;
            if (PieceColor.White == myColor && bWhiteOnBottom || PieceColor.Black == myColor && !bWhiteOnBottom) {
                // Moves are from bottom to top.
                // This check shouldn't really be necessary, since a pawn can't be on the last rank.
                if (irow > 0) {
                    if (0 == m_board.cells[irow - 1, icol]) {
                        // Square is empty.
                        aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow - 1, icol);
                    }
                    if (irow == NUMROWS - 2) {
                        // Pawn is in initial position, so move of 2 squares may be available.
                        if (0 == m_board.cells[irow - 1, icol] && 0 == m_board.cells[irow - 2, icol]) {
                            aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow - 2, icol);
                        }
                    }
                    // Now for captures. 
                    if (icol > 0) {
                        if (0 != m_board.cells[irow - 1, icol - 1] && ColorOfCell(m_board.cells[irow - 1, icol - 1]) != myColor) {
                            aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow - 1, icol - 1);
                        }
                    }
                    if (icol < NUMCOLS - 1) {
                        if (0 != m_board.cells[irow - 1, icol + 1] && ColorOfCell(m_board.cells[irow - 1, icol + 1]) != myColor) {
                            aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow - 1, icol + 1);
                        }
                    }
                }
            } else {
                // Moves are from top to bottom.
                // Move one ahead?
                if (irow < NUMROWS-1) {
                    if (0 == m_board.cells[irow + 1, icol]) {
                        // Square is empty.
                        aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow + 1, icol);
                    }
                    if (irow == 1) {
                        // Pawn is in initial position, so move of 2 squares may be available.
                        if (0 == m_board.cells[irow + 1, icol] && 0 == m_board.cells[irow + 2, icol]) {
                            aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow + 2, icol);
                        }
                    }
                    // Now for captures. 
                    if (icol > 0) {
                        if (0 != m_board.cells[irow + 1, icol - 1] && ColorOfCell(m_board.cells[irow + 1, icol - 1]) != myColor) {
                            aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow + 1, icol - 1);
                        }
                    }
                    if (icol < NUMCOLS - 1) {
                        if (0 != m_board.cells[irow + 1, icol + 1] && ColorOfCell(m_board.cells[irow + 1, icol + 1]) != myColor) {
                            aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow + 1, icol + 1);
                        }
                    }
                }
            }

            return nMoves;
        }

        int ComputeLegalMovesForRook(int irow, int icol, ref byte[] aryValidMoves) {
            int nMoves = 0;
            int ir, ic;
            int piece = m_board.cells[irow, icol];
            int myColor = piece & PieceColor.Mask;
            int otherColor, otherPiece;
            // Look in same row.
            // First, look to the left.
            for (ic = icol - 1; ic >= 0; ic--) {
                otherPiece = m_board.cells[irow, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;

                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow, ic);
                    break;
                }
            }

            for (ic = icol + 1; ic < NUMCOLS; ic++) {
                otherPiece = m_board.cells[irow, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;
                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(irow, ic);
                    break;
                }
            }

            // Now look in same column.
            for (ir = irow - 1; ir >= 0; ir--) {
                otherPiece = m_board.cells[ir, icol];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(ir, icol);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(ir, icol);
                    break;
                }
            }
            for (ir = irow + 1; ir < NUMROWS; ir++) {
                otherPiece = m_board.cells[ir, icol];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(ir, icol);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = CalcByteFromRowAndCol(ir, icol);
                    break;
                }
            }

            return nMoves;
        }


        int ComputeLegalMovesForPiece(int irow, int icol) {
            int pieceType = m_board.cells[irow, icol];
            pieceType &= PieceType.Mask;
            int nMoves = 0;
            switch (pieceType) {
                case PieceType.Rook:
                    nMoves = ComputeLegalMovesForRook(irow, icol, ref m_ValidMoves);
                    break;
                case PieceType.Pawn:
                    nMoves = ComputeLegalMovesForPawn(irow, icol, ref m_ValidMoves);
                    break;
            }
            return nMoves;
        }

        void DrawBoard(Graphics g, ref Board board) {
            DrawSquares(g);
            DrawCoordinates(g);
            for (int irow = 0; irow < 8; irow++) {
                for (int icol = 0; icol < 8; icol++) {
                    if (board.cells[irow, icol] != 0) {
                        DrawPiece(g, board.cells[irow, icol], irow, icol);
                    }
                }
            }
            DrawHighlights(g);
        }

        void CreateInitialBoard(ref Board board) {
            board.cells[0, 0] = PieceColor.Black | PieceType.Rook;
            board.cells[0, 1] = PieceColor.Black | PieceType.Knight;
            board.cells[0, 2] = PieceColor.Black | PieceType.Bishop;
            board.cells[0, 3] = PieceColor.Black | PieceType.Queen;
            board.cells[0, 4] = PieceColor.Black | PieceType.King;
            board.cells[0, 5] = PieceColor.Black | PieceType.Bishop;
            board.cells[0, 6] = PieceColor.Black | PieceType.Knight;
            board.cells[0, 7] = PieceColor.Black | PieceType.Rook;
            for (int icol = 0; icol < 8; icol++) {
                board.cells[1, icol] = PieceColor.Black | PieceType.Pawn;
                board.cells[6, icol] = PieceColor.White | PieceType.Pawn;
            }

            board.cells[7, 0] = PieceColor.White | PieceType.Rook;
            board.cells[7, 1] = PieceColor.White | PieceType.Knight;
            board.cells[7, 2] = PieceColor.White | PieceType.Bishop;
            board.cells[7, 3] = PieceColor.White | PieceType.Queen;
            board.cells[7, 4] = PieceColor.White | PieceType.King;
            board.cells[7, 5] = PieceColor.White | PieceType.Bishop;
            board.cells[7, 6] = PieceColor.White | PieceType.Knight;
            board.cells[7, 7] = PieceColor.White | PieceType.Rook;
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

            //DrawSquares(e.Graphics);
            //DrawPawn(e.Graphics, 1, 1);

            //int piece = 0;
            //for (int irow = 0; irow < 5; irow++) {
            //    for (int icol = 0; icol < 8; icol++) {
            //        DrawPiece(e.Graphics, piece, irow, icol);
            //        piece++;
            //        if (piece > 15) piece = 0;
            //    }
            //}

            //Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            //e.Graphics.DrawString("♔♕♖♗♘♙ ♚♛♜♝♞♟", font, brush, 120.0F, 130.0F);
            //Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

            //e.Graphics.DrawString("♔♕♖♗♘♙ ♚♛♜♝♞♟", fontPieces, brushBlack, 
            //    (float)(offsetLeft + 0.15*squareSize), (float)(offsetTop + 6.15*squareSize));
            //e.Graphics.DrawString("♔♕♖♗♘♙ ♚♛♜♝♞♟", fontPieces, brushBlack,
            //    (float)(offsetLeft + 0.15 * squareSize), (float)(offsetTop + 6.15 * squareSize));

            CreateInitialBoard(ref m_board);
            // Sample pieces on board - temporary.
            m_board.cells[4, 3] = PieceType.Rook | PieceColor.White;
            m_board.cells[2, 3] = PieceType.Bishop | PieceColor.White;
            m_board.cells[5, 3] = PieceType.Knight | PieceColor.White;
            m_board.cells[5, 5] = PieceType.Knight | PieceColor.Black;
            DrawBoard(e.Graphics, ref m_board);
        }

        private void ChessR1Form_MouseDown(object sender, MouseEventArgs e) {
            selectedColStart = NOT_SELECTED;
            // Implicit conversion from Point to PointF.
            PointF pointMouse = e.Location;
            if (pointMouse.X >= offsetLeft && pointMouse.X < offsetLeft + NUMCOLS * squareSize) {
                if (pointMouse.Y > offsetTop && pointMouse.Y < offsetTop + NUMROWS * squareSize) {
                    selectedColStart = (int)((pointMouse.X - offsetLeft) / squareSize);
                    selectedRowStart = (int)((pointMouse.Y - offsetTop) / squareSize);
                    m_nValidMoves = ComputeLegalMovesForPiece(selectedRowStart, selectedColStart);
                    //DebugOut($"Selected row {selectedRowStart} col {selectedColStart}");
                }
            }
            //if (NOT_SELECTED == selectedColStart) {
            //    DebugOut("Click detected off the board");
            //}

            Invalidate();
        }
    }
}
