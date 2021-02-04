﻿// ChessR1 - Program to play a rudimentary game of chess.
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
        readonly Pen penSelectedStart = new Pen(Color.MediumSpringGreen, 2.0F);
        readonly Pen penSelectedStop = new Pen(Color.Tomato, 2.0F);
        readonly Pen penLegalMoves = new Pen(Color.Plum, 2.0F);
        // I'm using Unicode characters to render the pieces.  
        // I don't think that each of the different fonts each has a unique rendering of 
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
        int selectedRowStop = NOT_SELECTED, selectedColStop = NOT_SELECTED;
        // Valid moves for the currently-selected piece.  Each byte contains a row and column
        // as encoded by EncodePositionFromRowAndCol.
        int[] m_ValidMovesForOnePiece = new int[64];
        int m_nValidMovesForOnePiece;
        // In this array, each move is encoded into an integer as follows (bit 0 = bottom bit):
        // bits 11-9: Column of "from" square
        // bits 8-6:  Row of from square
        // bits 5-3:  Colum to "to" square
        // bits 2-0:  Row of to square
        int[] m_ValidMovesForComputer = new int[256];
        int m_nValidMovesForComputer;
        bool bWhiteOnBottom = true;
        int m_ComputersColor = PieceColor.Black;
        static int [] aryKnightMoves = {-2, -1, 1, 2};
        const int POSITION_BITMASK = 63;
        Random m_random = new Random();

        public ChessR1Form() {
            InitializeComponent();
            thickness = (float)(squareSize * 0.04);
            penBlack = new System.Drawing.Pen(System.Drawing.Color.Black, 1 * thickness);
        }

        public void DebugOut(string msg) {
            System.Diagnostics.Trace.WriteLine(DateTime.Now + " " + msg);
        }

        // Combine a row and column number into a single integer.
        int EncodePositionFromRowAndCol(int irow, int icol) {
            return (NUMCOLS * icol + irow);
        }

        // Create an integer which encodes a move.
        int EncodeMoveFromRowsAndCols(int irowStart, int icolStart, int irowStop, int icolStop) {
            int encodedMove = (EncodePositionFromRowAndCol(irowStart, icolStart) << 6) | EncodePositionFromRowAndCol(irowStop, icolStop);
            //DebugOut($"EncodeMoveFromRowsAndCols: from ({irowStart},{icolStart}) to ({irowStop}, {icolStop}) encoded as {encodedMove} or {Convert.ToString(encodedMove, 8)} octal");
            return encodedMove;
        }

        void DecodeMoveFromInt(int move, out int irowStart, out int icolStart, out int irowStop, out int icolStop) {
            DecodeRowAndCol(move, out irowStop, out icolStop);
            move >>= 6;
            DecodeRowAndCol(move, out irowStart, out icolStart);
        }

        void DecodeRowAndCol(int rowcol, out int irow, out int icol) {
            irow = rowcol & (NUMCOLS - 1);
            icol = (rowcol & POSITION_BITMASK) / NUMCOLS;
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
            for (int idx = 0; idx < m_nValidMovesForOnePiece; idx++) {
                DecodeRowAndCol(m_ValidMovesForOnePiece[idx], out irow, out icol);
                float x = (float)(offsetLeft + (icol + 0.05) * squareSize);
                float y = (float)(offsetTop + (irow + 0.05) * squareSize);
                float width = (float)(squareSize * 0.9);
                float height = width;
                g.DrawRectangle(penLegalMoves, x, y, width, height);
            }

            // Highlight the square to which a piece was just moved, if any.
            if (selectedRowStop >= 0 && selectedColStop >= 0) {
                float x = (float)(offsetLeft + (selectedColStop + 0.05) * squareSize);
                float y = (float)(offsetTop + (selectedRowStop + 0.05) * squareSize);
                float width = (float)(squareSize * 0.9);
                float height = width;
                g.DrawRectangle(penSelectedStop, x, y, width, height);
            }
        }

        void DrawPieces(Graphics g, ref Board board) {
            int nDrawn = 0;
            for (int irow = 0; irow < NUMROWS; irow++) {
                for (int icol = 0; icol < NUMCOLS; icol++) {
                    if (board.cells[irow, icol] != 0) {
                        DrawPiece(g, board.cells[irow, icol], irow, icol);
                        nDrawn++;
                    } else {
                        DrawPiece(g, 0, irow, icol);
                    }
                }
            }
        }

        // Compute a textual representation of the board.
        // Capital letters are used for white pieces, lower case for black,
        // and "." for empty squares.
        string ComputeTextualBoard() {
            string strBoard = "";
            for (int irow = 0; irow < NUMROWS; irow++) {
                for (int icol = 0; icol < NUMCOLS; icol++) {
                    int piece = m_board.cells[irow, icol] & PieceType.Mask;
                    int color = m_board.cells[irow, icol] & PieceColor.Mask;
                    string strPieces = (color == PieceColor.White) ? ".KQRBNP*" : ".kqrbnp*";
                    strBoard += strPieces.Substring(piece, 1);
                }
                strBoard += "\r\n";
            }
            return strBoard;
        }

        void DrawTextualBoard() {
            textBoxBoard.Text = ComputeTextualBoard();
        }

        void DrawBoard(Graphics g, ref Board board) {
            DrawTextualBoard();
            DrawSquares(g);
            DrawCoordinates(g);
            DrawPieces(g, ref board);
            DrawHighlights(g);
        }

        bool IsLegalSquare(int irow, int icol) {
            return (irow >= 0 && irow < NUMROWS) && (icol >= 0 && icol < NUMCOLS);
        }

        int ComputeLegalMovesForPawn(int irow, int icol, ref int[] aryValidMoves, ref int nMoves) {
            int piece = m_board.cells[irow, icol];
            int myColor = piece & PieceColor.Mask;
            if (PieceColor.White == myColor && bWhiteOnBottom || PieceColor.Black == myColor && !bWhiteOnBottom) {
                // Moves are from bottom to top.
                // This check shouldn't really be necessary, since a pawn can't be on the last rank.
                if (irow > 0) {
                    if (0 == m_board.cells[irow - 1, icol]) {
                        // Square is empty.
                        aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, irow - 1, icol);
                    }
                    if (irow == NUMROWS - 2) {
                        // Pawn is in initial position, so move of 2 squares may be available.
                        if (0 == m_board.cells[irow - 1, icol] && 0 == m_board.cells[irow - 2, icol]) {
                            aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, irow - 2, icol);
                        }
                    }
                    // Now for captures. 
                    if (icol > 0) {
                        if (0 != m_board.cells[irow - 1, icol - 1] && ColorOfCell(m_board.cells[irow - 1, icol - 1]) != myColor) {
                            aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol,irow - 1, icol - 1);
                        }
                    }
                    if (icol < NUMCOLS - 1) {
                        if (0 != m_board.cells[irow - 1, icol + 1] && ColorOfCell(m_board.cells[irow - 1, icol + 1]) != myColor) {
                            aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol,irow - 1, icol + 1);
                        }
                    }
                }
            } else {
                // Moves are from top to bottom.
                // Move one ahead?
                if (irow < NUMROWS-1) {
                    if (0 == m_board.cells[irow + 1, icol]) {
                        // Square is empty.
                        aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol,irow + 1, icol);
                    }
                    if (irow == 1) {
                        // Pawn is in initial position, so move of 2 squares may be available.
                        if (0 == m_board.cells[irow + 1, icol] && 0 == m_board.cells[irow + 2, icol]) {
                            aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol,irow + 2, icol);
                        }
                    }
                    // Now for captures. 
                    if (icol > 0) {
                        if (0 != m_board.cells[irow + 1, icol - 1] && ColorOfCell(m_board.cells[irow + 1, icol - 1]) != myColor) {
                            aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol,irow + 1, icol - 1);
                        }
                    }
                    if (icol < NUMCOLS - 1) {
                        if (0 != m_board.cells[irow + 1, icol + 1] && ColorOfCell(m_board.cells[irow + 1, icol + 1]) != myColor) {
                            aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol,irow + 1, icol + 1);
                        }
                    }
                }
            }

            return nMoves;
        }

        void ComputeLegalMovesForKnight(int irow, int icol, ref int[] aryValidMoves, ref int nMoves) {
            int piece = m_board.cells[irow, icol];
            int myColor = piece & PieceColor.Mask;

            foreach (int ir in aryKnightMoves) {
                foreach (int ic in aryKnightMoves) {
                    // A knight moves 2 squares in one direction, and 1 in the other.
                    // Our loops cover too many possibilities, so we skip processing
                    // if the number of squares in the two directions are equal.
                    if (Math.Abs(ic) == Math.Abs(ir)) continue;
                    int newRow = irow + ir;
                    int newCol = icol + ic;
                    if (IsLegalSquare(newRow, newCol)) {
                        int otherPiece = m_board.cells[newRow, newCol];
                        int otherColor = otherPiece & PieceColor.Mask;
                        otherPiece &= PieceType.Mask;
                        bool bMoveOK = false;
                        if (0 == otherPiece) {
                            // Empty square
                            bMoveOK = true;
                        } else if (otherColor != myColor) {
                            // This would be a capture of an enemy piece.
                            // MRRToDo: check for discovered check, stalemate, etc.
                            bMoveOK = true;
                        }
                        //DebugOut($"Knight at ({irow},{icol}) to move to ({newRow},{newCol}): myColor={myColor} otherColor={otherColor} bMoveOK={bMoveOK}");
                        if (bMoveOK) {
                            aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol,newRow, newCol);
                        }
                    }
                }
            }
        }

        void ComputeLegalMovesForRook(int irow, int icol, ref int[] aryValidMoves, ref int nMoves) {
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
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, irow, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;

                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, irow, ic);
                    break;
                }
            }

            for (ic = icol + 1; ic < NUMCOLS; ic++) {
                otherPiece = m_board.cells[irow, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;
                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, irow, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, irow, ic);
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
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, icol);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, icol);
                    break;
                }
            }
            for (ir = irow + 1; ir < NUMROWS; ir++) {
                otherPiece = m_board.cells[ir, icol];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, icol);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, icol);
                    break;
                }
            }
        }

        void ComputeLegalMovesForBishop(int irow, int icol, ref int[] aryValidMoves, ref int nMoves) {
            int ir, ic;
            int piece = m_board.cells[irow, icol];
            int myColor = piece & PieceColor.Mask;
            int otherColor, otherPiece;
            // Starting at square the bishop is on, look diagonally away from the bishop
            // in each of the four directions.
            // First, look to the up and to the left.
            for (ic = icol - 1, ir = irow - 1; ic >= 0 && ir >= 0; ic--, ir--) {
                otherPiece = m_board.cells[ir, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                    break;
                }
            }

            // Look up and to the right.
            for (ic = icol - 1, ir = irow + 1; ic >= 0 && ir < NUMROWS; ic--, ir++) {
                otherPiece = m_board.cells[ir, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;
                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                    break;
                }
            }

            // Look down and to the left.
            for (ic = icol + 1, ir = irow - 1; ic < NUMCOLS && ir >= 0; ic++, ir--) {
                otherPiece = m_board.cells[ir, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                    break;
                }
            }

            // Look down and to the right.
            for (ic = icol + 1, ir = irow + 1; ic < NUMCOLS && ir < NUMROWS; ic++, ir++) {
                otherPiece = m_board.cells[ir, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                    break;
                }
            }
        }

        void ComputeLegalMovesForKing(int irow, int icol, ref int[] aryValidMoves, ref int nMoves) {
            int ir, ic;
            int piece = m_board.cells[irow, icol];
            int myColor = piece & PieceColor.Mask;
            int otherColor, otherPiece;
            // Consider all ways in which the king could move 0 or 1 squares horizontally and vertically,
            // except the case of not moving at all.
            for (int deltacol = -1; deltacol <= 1; deltacol++) {
                ic = icol + deltacol;
                // Don't move off the board.
                if (ic < 0 || ic >= NUMCOLS) continue;
                for (int deltarow = -1; deltarow <= 1; deltarow++) {
                    ir = irow + deltarow;
                    // Don't move off the board.
                    if (ir < 0 || ir >= NUMROWS) continue;
                    // We mustn't stay in one place to be a move.
                    if (deltacol == 0 && deltarow == 0) continue;

                    otherPiece = m_board.cells[ir, ic];
                    otherColor = otherPiece & PieceColor.Mask;
                    otherPiece &= PieceType.Mask;
                    if (0 == otherPiece || myColor != otherColor) {
                        // Empty square or capturing other piece.  
                        //mrr this really needs fleshing out because the king can't capture a defended piece.
                        aryValidMoves[nMoves++] = EncodeMoveFromRowsAndCols(irow, icol, ir, ic);
                    }
                }
            }
         }

        void ComputeLegalMovesForPiece(int irow, int icol, ref int[] aryValidMoves, ref int nMoves) {
            nMoves = 0;
            int pieceType = m_board.cells[irow, icol];
            pieceType &= PieceType.Mask;
            switch (pieceType) {
                case PieceType.Rook:
                    ComputeLegalMovesForRook(irow, icol, ref aryValidMoves, ref nMoves);
                    break;
                case PieceType.Pawn:
                    ComputeLegalMovesForPawn(irow, icol, ref aryValidMoves, ref nMoves);
                    break;
                case PieceType.Knight:
                    ComputeLegalMovesForKnight(irow, icol, ref aryValidMoves, ref nMoves);
                    break;
                case PieceType.Bishop:
                    ComputeLegalMovesForBishop(irow, icol, ref aryValidMoves, ref nMoves);
                    break;
                case PieceType.Queen:
                    ComputeLegalMovesForRook(irow, icol, ref aryValidMoves, ref nMoves);
                    ComputeLegalMovesForBishop(irow, icol, ref aryValidMoves, ref nMoves);
                    break;
                case PieceType.King:
                    ComputeLegalMovesForKing(irow, icol, ref aryValidMoves, ref nMoves);
                    break;
            }
        }

        void ComputeLegalMovesForComputer(ref Board board) {
            m_nValidMovesForComputer = 0;
            for (int irow = 0; irow < NUMROWS; irow++) {
                for (int icol = 0; icol < NUMCOLS; icol++) {
                    if ((board.cells[irow, icol] & PieceColor.Mask) == m_ComputersColor) {
                        byte piece = board.cells[irow, icol];
                        int color = piece & PieceColor.Mask;
                        int pieceType = piece & PieceType.Mask;
                        //DebugOut($"ComputeLegalMovesForComputer: looking at {PieceColor.ToString(color)} {PieceType.ToString(pieceType)} at ({irow},{icol}) ");
                        ComputeLegalMovesForPiece(irow, icol, ref m_ValidMovesForComputer, ref m_nValidMovesForComputer);
                    }
                }
            }
            // Now m_ValidMovesForComputer has all the legal moves we could make.
        }

        void ChooseMoveForComputer(ref Board board, ref int[] aryValidMoves, int nValidMoves) {
            //for (int imove = 0; imove < m_nValidMovesForComputer; imove++) {
            //}
            // For now, choose a piece at random.
            int idxMove = m_random.Next(nValidMoves);
            int move = aryValidMoves[idxMove];
            //DebugOut($"ChooseMoveForComputer: nValidMoves={nValidMoves}; I chose index {idxMove} which is {move}");
            int irowStart, icolStart, irowStop, icolStop;
            DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
            int piece = board.cells[irowStart, icolStart];
            int color = piece & PieceColor.Mask;
            int pieceType = piece & PieceType.Mask;
            DebugOut($"Computer will move {PieceColor.ToString(color)} {PieceType.ToString(pieceType)} from ({irowStart},{icolStart}) to ({irowStop},{icolStop})");
            MovePiece(ref board, irowStart, icolStart, irowStop, icolStop);
        }

        void MovePiece(ref Board board, int irowStart, int icolStart, int irowStop, int icolStop) {
            int piece = board.cells[irowStart, icolStart];
            int oldpiece = board.cells[irowStop, icolStop];
            board.cells[irowStop, icolStop] = (byte)piece;
            board.cells[irowStart, icolStart] = 0;

            // This causes the move to be displayed on the board.
            // Should I make this optional?
            selectedRowStart = irowStart;
            selectedColStart = icolStart;
            selectedRowStop = irowStop;
            selectedColStop = icolStop;
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

        private void ChessR1Form_Load(object sender, EventArgs e) {
            CreateInitialBoard(ref m_board);
            // Sample pieces on board - temporary.
            //m_board.cells[4, 3] = PieceType.Rook | PieceColor.White;
            //m_board.cells[2, 3] = PieceType.Bishop | PieceColor.White;
            //m_board.cells[5, 3] = PieceType.Knight | PieceColor.White;
            //m_board.cells[5, 5] = PieceType.Knight | PieceColor.Black;
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

            DrawBoard(e.Graphics, ref m_board);
        }

        private void ChessR1Form_MouseDown(object sender, MouseEventArgs e) {
            //DebugOut("In MouseDown");
            bool bWithinASquare = false;
            int prevCol = selectedColStart;
            int prevRow = selectedRowStart;
            int curCol = NOT_SELECTED;
            int curRow = NOT_SELECTED;
            bool bASquareWasAlreadySelected = (NOT_SELECTED != prevCol);

            // If both a start and stop square are still defined, clear all.
            if (NOT_SELECTED != selectedColStart && NOT_SELECTED != selectedColStop) {
                selectedRowStart = NOT_SELECTED;
                selectedColStart = NOT_SELECTED;
                selectedRowStop = NOT_SELECTED;
                selectedColStop = NOT_SELECTED;
                m_nValidMovesForOnePiece = 0;
            }
            // Implicit conversion from Point to PointF.
            PointF pointMouse = e.Location;
            if (pointMouse.X >= offsetLeft && pointMouse.X < offsetLeft + NUMCOLS * squareSize) {
                if (pointMouse.Y > offsetTop && pointMouse.Y < offsetTop + NUMROWS * squareSize) {
                    curCol = (int)((pointMouse.X - offsetLeft) / squareSize);
                    curRow = (int)((pointMouse.Y - offsetTop) / squareSize);
                    if (!bASquareWasAlreadySelected) {
                         ComputeLegalMovesForPiece(curRow, curCol, ref m_ValidMovesForOnePiece, ref m_nValidMovesForOnePiece);
                    }
                    bWithinASquare = true;
                }
            }

            if (bWithinASquare) {
                if (bASquareWasAlreadySelected) {
                    // We have selected a square, and there was a square already selected.
                    // Is the new square different than the old?
                    if (prevCol != curCol || prevRow != curRow) {
                        // Yes, they are different, so it means the player is trying
                        // to move there.  Is this a legal place to move that piece?
                        int desiredLoc = EncodePositionFromRowAndCol(curRow, curCol);
                        bool bDidMove = false;
                        for (int j = 0; j < m_nValidMovesForOnePiece; j++) {
                            if ((m_ValidMovesForOnePiece[j] & POSITION_BITMASK) == desiredLoc) {
                                // Yes, it's legal to move this piece here, so move it.
                                //byte pieceBeingMoved = m_board.cells[selectedRowStart, selectedColStart];
                                ////DebugOut($"We will move {pieceBeingMoved} from row {selectedRowStart} col {selectedColStart} to row {curRow} col {curCol}");
                                //m_board.cells[curRow, curCol] = pieceBeingMoved;
                                //m_board.cells[selectedRowStart, selectedColStart] = 0;
                                MovePiece(ref m_board, selectedRowStart, selectedColStart, curRow, curCol);

                                selectedRowStop = curRow;
                                selectedColStop = curCol;

                                // Now that the piece has moved, erase its previous legal moves.
                                m_nValidMovesForOnePiece = 0;
                                bDidMove = true;
                                ComputeLegalMovesForComputer(ref m_board);
                                ChooseMoveForComputer(ref m_board, ref m_ValidMovesForComputer, m_nValidMovesForComputer);
                                //DebugOut($"MouseDown: board is now\r\n{ComputeTextualBoard()}");
                                break;
                            } else {
                                //DebugOut($"Can't move: desired={desiredLoc} m_ValidMoves[j]={m_ValidMoves[j]}");
                            }
                        }
                        if (!bDidMove) {
                            // A square had already been selected, and the user chose a different one.
                            // But it was not a legal destination.
                            // This means that the user changed their mind and selected a different piece.
                            selectedRowStart = curRow;
                            selectedColStart = curCol;
                            selectedRowStop = NOT_SELECTED;
                            selectedColStop = NOT_SELECTED;
                            ComputeLegalMovesForPiece(curRow, curCol, ref m_ValidMovesForOnePiece, ref m_nValidMovesForOnePiece);
                        }
                    } else {
                        // It's the same square.
                        selectedRowStop = NOT_SELECTED;
                        selectedColStop = NOT_SELECTED;
                    }
                } else {
                    // No square had already been selected.
                    selectedRowStart = curRow;
                    selectedColStart = curCol;
                    selectedRowStop = NOT_SELECTED;
                    selectedColStop = NOT_SELECTED;
                    ComputeLegalMovesForPiece(curRow, curCol, ref m_ValidMovesForOnePiece, ref m_nValidMovesForOnePiece);
                }
            } else {
                selectedRowStart = NOT_SELECTED;
                selectedColStart = NOT_SELECTED;
                selectedRowStop = NOT_SELECTED;
                selectedColStop = NOT_SELECTED;
                m_nValidMovesForOnePiece = 0;
            }

            //if (NOT_SELECTED == selectedColStart) {
            //    DebugOut("Click detected off the board");
            //}
            Invalidate();
            //DebugOut($"MouseDown end: board is now\r\n{ComputeTextualBoard()}");
        }
    }
}
