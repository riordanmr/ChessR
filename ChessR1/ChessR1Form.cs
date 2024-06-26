﻿// ChessR1 - Program to play a rudimentary game of chess.
// This is my first chess program.  I am going to start with a GUI
// so the program is easier to debug once I actually start implementing
// the chess engine.
// I'm starting this as a .NET WinForms app for familiarity, but that 
// will not necessarily be its final form. 
// I may eventually translate this to another, simpler, language, so I 
// am not using all the fancy features of C#. 
// Mark Riordan  23-JAN-2021

//#define USING_OUTPUTDEBUGSTRING

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ChessR1
{
    public partial class ChessR1Form : Form
    {
        private Brush brushBlack = new System.Drawing.SolidBrush(Color.Black);
        private Brush brushWhite = new SolidBrush(Color.White);
        Brush brushBoardLight = new SolidBrush(Color.FromName("cornsilk"));
        Brush brushBoardDark = new SolidBrush(Color.FromName("moccasin"));
        Brush brushCapturedPiecesWhite = new SolidBrush(Color.LemonChiffon);
        Brush brushCapturedPiecesBlack = new SolidBrush(Color.SandyBrown);

        float offsetCapturedPiecesTop = 80.0F;
        float offsetTop = 150.0F;
        float offsetLeft = 90.0F;
        float squareSize = 120.0F;
        float thickness;
        System.Drawing.Pen penBlack;
        readonly Pen penSelectedStart = new Pen(Color.MediumSpringGreen, 2.0F);
        readonly Pen penSelectedStop = new Pen(Color.Salmon, 2.0F); // Color.Tomato
        readonly Pen penLegalMoves = new Pen(Color.Plum, 2.0F);
        //readonly Pen penAttackedFrom = new Pen(Color.OrangeRed, 2.0F);

        // I'm using Unicode characters to render the pieces.  
        // I don't think that each of the different fonts each has a unique rendering of 
        // the pieces, but I can see that at least Arial and Segoe UI Symbol do.
        // Unfortunately, the center points of the characters differ between those
        // fonts, so you can't just swap in one font for another and have it look good. 
        Font fontPieces = new Font("Arial", 60);
        Font fontCapturedPieces = new Font("Arial", 40);
        Font fontCoords = new Font("Lucida Console", 24);
        string strAllPieces = " ♔♕♖♗♘♙  ♚♛♜♝♞♟  ";
        Board m_board = new Board();
        const int NUMROWS = 8;
        const int NUMCOLS = 8;
        const int NOT_SELECTED = -1;
        int selectedRowStart = NOT_SELECTED, selectedColStart = NOT_SELECTED;
        int selectedRowStop = NOT_SELECTED, selectedColStop = NOT_SELECTED;
        // Valid moves for the currently-selected piece.  Each byte contains a row and column
        // as encoded by EncodePositionFromRowAndCol.
        ulong[] m_ValidMovesForOnePiece = new ulong[64];
        int m_nValidMovesForOnePiece;
        // In this array, each move is encoded into an integer as follows (bit 0 = bottom bit):
        // bits 11-9: Column of "from" square
        // bits 8-6:  Row of from square
        // bits 5-3:  Colum to "to" square
        // bits 2-0:  Row of to square
        ulong[] m_ValidMovesForComputer = new ulong[256];
        int m_nValidMovesForComputer;
        int[] m_ScoresForValidMovesForComputer = new int[256];
        ulong[] m_ValidMoves = new ulong[1024];
        bool m_bWhiteOnBottom;  // true if white is on the bottom of the board (meaning human plays white).
        int m_ComputersColor;  // Color being played by the computer.
        //int m_ComputersDirection;  // -1 for computer moves up the board; 1 for computer moves down
        static int[] aryKnightMoves = { -2, -1, 1, 2 };
        const int POSITION_BITMASK = 63;
        Random m_random = new Random();
        bool m_bGameOver = false;
        int[] m_aryPieceBaseValue = new int[16];
        const long RESULT_NO_VALID_MOVES = 0xffff;
        int m_lookaheadPlies = 4;
        const ulong DBG_MISC = 2;
        const ulong DBG_MOVES = 1;
        ulong m_DebugBits = DBG_MOVES | DBG_MISC;
        StreamWriter m_swDebug = new StreamWriter("ChessR1.log");
        Stopwatch m_stopwatch = new Stopwatch();
        public CmdLineSettings m_cmdLineSettings;

        public ChessR1Form() {
            InitializeComponent();
            thickness = (float)(squareSize * 0.04);
            penBlack = new System.Drawing.Pen(System.Drawing.Color.Black, 1 * thickness);
            SetComputersColor(PieceColor.Black);
            InitializePieceBaseValues();
            InitializeGame();
        }

        private void InitializePieceBaseValues() {
            // Values for pieces come from _Chess Skill in Man and Machine_, edition 1, p. 94.
            m_aryPieceBaseValue[PieceColor.White | PieceType.Pawn] = 100;
            m_aryPieceBaseValue[PieceColor.White | PieceType.Knight] = 325;
            m_aryPieceBaseValue[PieceColor.White | PieceType.Bishop] = 350;
            m_aryPieceBaseValue[PieceColor.White | PieceType.Rook] = 500;
            m_aryPieceBaseValue[PieceColor.White | PieceType.Queen] = 900;
            m_aryPieceBaseValue[PieceColor.White | PieceType.King] = 19999;

            m_aryPieceBaseValue[PieceColor.Black | PieceType.Pawn] = -100;
            m_aryPieceBaseValue[PieceColor.Black | PieceType.Knight] = -325;
            m_aryPieceBaseValue[PieceColor.Black | PieceType.Bishop] = -350;
            m_aryPieceBaseValue[PieceColor.Black | PieceType.Rook] = -500;
            m_aryPieceBaseValue[PieceColor.Black | PieceType.Queen] = -900;
            m_aryPieceBaseValue[PieceColor.Black | PieceType.King] = -19999;
        }

        public void DebugOut(string msg) {
#if USING_OUTPUTDEBUGSTRING
            System.Diagnostics.Trace.WriteLine(DateTime.Now + " " + msg);
#else
            string now = DateTime.Now.ToString("yyyy-MM-dd HHH:mm:ss.fff");
            m_swDebug.WriteLine(now + " " + msg);
#endif
        }

        public void SetMessage(string msg) {
            labelMessage.Visible = true;
            labelMessage.Text = msg;
        }

        public void ClearMessage() {
            labelMessage.Visible = false;
            labelMessage.Text = "";
        }

        // Set the color being played by the computer.
        // Input:   color is 8 for black or 0 for white.
        public void SetComputersColor(int color) {
            m_ComputersColor = color;
            //m_ComputersDirection = (PieceColor.Black == m_ComputersColor) ? -1 : 1;
            m_bWhiteOnBottom = (PieceColor.Black == m_ComputersColor);
        }

        // Combine a row and column number into a single integer.
        // Exit:    Returns the position in the bottom 6 bits of an integer:
        //          Bits 5-3: column; bits 2-0: row.
        ulong EncodePositionFromRowAndCol(int irow, int icol) {
            return (ulong)(NUMCOLS * icol + irow);
        }

        // Create an integer which encodes a move.
        // Exit:    Returns the move in the bottom 12 bits of an integer:
        //          Bits 11-9: column from; bits 8-6: column from; bits 5-3: column to; bits 2-0: row to.
        ulong EncodeMoveFromRowsAndCols(int irowStart, int icolStart, int irowStop, int icolStop) {
            ulong encodedMove = (EncodePositionFromRowAndCol(irowStart, icolStart) << 6) | EncodePositionFromRowAndCol(irowStop, icolStop);
            //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"EncodeMoveFromRowsAndCols: from ({irowStart},{icolStart}) to ({irowStop}, {icolStop}) encoded as {encodedMove} or {Convert.ToString(encodedMove, 8)} octal");
            return encodedMove;
        }

        void DecodeMoveFromInt(ulong move, out int irowStart, out int icolStart, out int irowStop, out int icolStop) {
            DecodeRowAndCol(move, out irowStop, out icolStop);
            move >>= 6;
            DecodeRowAndCol(move, out irowStart, out icolStart);
        }

        void DecodeRowAndCol(ulong rowcol, out int irow, out int icol) {
            irow = (int)(rowcol & (NUMCOLS - 1));
            icol = (int)((rowcol & POSITION_BITMASK) / NUMCOLS);
        }

        string RowColToAlgebraic(int irow, int icol) {
            return "abcdefgh".Substring(icol, 1) + "12345678".Substring(7 - irow, 1);
        }

        string DescribePiece(int piece) {
            return PieceColor.ToString(piece & PieceColor.Mask) + " " + PieceType.ToString(piece & PieceType.Mask);
        }

        string DescribeMove(ulong move) {
            int irowStart, icolStart, irowStop, icolStop;
            DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
            return RowColToAlgebraic(irowStart, icolStart) + RowColToAlgebraic(irowStop, icolStop);
        }

        // Given a ulong containing a sequence of moves, return a string representation
        // of those moves.
        // Entry:   bestLine    contains a sequence of 12-bit moves, with the least significant
        //                      12-bit packet being the last.  12 bits of 0's indicate no move.
        string DescribeBestLine(ulong bestLine) {
            //DebugOut($"Entered DescribeBestLine with bestLine {String.Format("{0:X}", bestLine)}");
            string strBestLine = "";
            for(int ibits=0; ibits < 64; ibits += 12) {
                ulong move = (bestLine >> ibits) & 0xfff;
                if (0==move) {
                    break;
                } else {
                    strBestLine = DescribeMove(move) + " " + strBestLine;
                }
            }
            return strBestLine;
        }

        int ColorOfCell(byte cell) {
            return (cell & (int)PieceColor.Mask);
        }

        int ComputeEffectiveRowForDisplay(int irow) {
            return m_bWhiteOnBottom ? irow : NUMROWS - irow - 1;
        }

        int ComputeEffectiveColForDisplay(int icol) {
            return m_bWhiteOnBottom ? icol : NUMCOLS - icol - 1;
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
            //string strPieces = " ♔♕♖♗♘♙  ♚♛♜♝♞♟  ";
            string strPiece = strAllPieces.Substring(pieceWithColor, 1);
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
                int idxLetter = ComputeEffectiveColForDisplay(icol);
                g.DrawString(strLetters.Substring(idxLetter, 1), fontCoords, brushWhite, x, y);
            }

            string strNumbers = "12345678";
            for (int irow = 0; irow < 8; irow++) {
                float x = (float)(offsetLeft - squareSize * 0.4);
                float y = (float)(offsetTop + squareSize * (irow + 0.5) - textSize.Height * 0.5);
                int idxDigit = m_bWhiteOnBottom ? NUMROWS - irow - 1 : irow; // ComputeEffectiveRowForDisplay(irow);
                g.DrawString(strNumbers.Substring(idxDigit, 1), fontCoords, brushWhite, x, y);
            }
        }

        // Draw colored rectangles in the squares we want to highlight.
        // The square currently selected by the user (if any) gets a green rectangle.
        void DrawHighlights(Graphics g) {
            int irow, icol;
            // Highlight the currently selected square, if any.
            if (selectedRowStart >= 0 && selectedColStart >= 0) {
                irow = ComputeEffectiveRowForDisplay(selectedRowStart);
                icol = ComputeEffectiveColForDisplay(selectedColStart);
                float x = (float)(offsetLeft + (icol + 0.05) * squareSize);
                float y = (float)(offsetTop + (irow + 0.05) * squareSize);
                float width = (float)(squareSize * 0.9);
                float height = width;
                g.DrawRectangle(penSelectedStart, x, y, width, height);
            }

            // Highlight the squares to which the currently selected piece can move.
            for (int idx = 0; idx < m_nValidMovesForOnePiece; idx++) {
                DecodeRowAndCol(m_ValidMovesForOnePiece[idx], out irow, out icol);
                irow = ComputeEffectiveRowForDisplay(irow);
                icol = ComputeEffectiveColForDisplay(icol);
                float x = (float)(offsetLeft + (icol + 0.05) * squareSize);
                float y = (float)(offsetTop + (irow + 0.05) * squareSize);
                float width = (float)(squareSize * 0.9);
                float height = width;
                g.DrawRectangle(penLegalMoves, x, y, width, height);
            }

            // Highlight the square to which a piece was just moved, if any.
            if (selectedRowStop >= 0 && selectedColStop >= 0) {
                irow = ComputeEffectiveRowForDisplay(selectedRowStop);
                icol = ComputeEffectiveColForDisplay(selectedColStop);
                float x = (float)(offsetLeft + (icol + 0.05) * squareSize);
                float y = (float)(offsetTop + (irow + 0.05) * squareSize);
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
                        //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"DrawPieces: drawing {DescribePiece(board.cells[irow, icol])} at {RowColToAlgebraic(irow,icol)}");
                        DrawPiece(g, board.cells[irow, icol],
                            ComputeEffectiveRowForDisplay(irow), ComputeEffectiveColForDisplay(icol));
                        nDrawn++;
                    } else {
                        DrawPiece(g, 0,
                            ComputeEffectiveRowForDisplay(irow), ComputeEffectiveColForDisplay(icol));
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

        int ColorTo0Or1(int color) {
            if (color == PieceColor.White) {
                return 0;
            } else {
                return 1;
            }
        }

        int OtherPieceColor(int color) {
            return PieceColor.Mask - color;
        }

        void DrawTextualBoard() {
            //textBoxBoard.Text = ComputeTextualBoard();
        }

        void DrawCapturedPieces(Graphics g) {
            // Draw pieces at top.
            //int desiredColor = m_bWhiteOnBottom ? PieceColor.Black : PieceColor.White;
            //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"m_board.nCapturedPieces= {m_board.nCapturedPieces}");
            string strPiecesWhite = "", strPiecesBlack = "";
            for (int icap = 0; icap < m_board.nCapturedPieces; icap++) {
                int thisPiece = m_board.capturedPieces[icap];
                if ((thisPiece & PieceColor.Mask) == PieceColor.White) {
                    strPiecesWhite += strAllPieces[thisPiece];
                } else {
                    strPiecesBlack += strAllPieces[thisPiece];
                }
            }

            // Draw the captured black pieces on White's side of the board, and vice versa.
            // Draw the pieces on top.  
            float x = (float)(offsetLeft);
            float y = (float)(offsetCapturedPiecesTop);
            //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"Drawing {strPiecesWhite} at {x},{y}");
            g.DrawString(m_bWhiteOnBottom ? strPiecesWhite : strPiecesBlack, fontCapturedPieces,
                m_bWhiteOnBottom ? brushCapturedPiecesWhite : brushCapturedPiecesBlack, x, y);
            // Draw the pieces on the bottom.
            y = (float)(offsetTop + (squareSize * NUMROWS) + 0.5 * squareSize);
            if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Drawing {strPiecesBlack} at {x},{y}");
            g.DrawString(m_bWhiteOnBottom ? strPiecesBlack : strPiecesWhite, fontCapturedPieces,
                m_bWhiteOnBottom ? brushCapturedPiecesBlack : brushCapturedPiecesWhite, x, y);
        }

        void DrawBoard(Graphics g, ref Board board) {
            //DrawTextualBoard();
            DrawSquares(g);
            DrawCoordinates(g);
            DrawPieces(g, ref board);
            DrawHighlights(g);
            DrawCapturedPieces(g);
        }

        bool IsLegalSquare(int irow, int icol) {
            return (irow >= 0 && irow < NUMROWS) && (icol >= 0 && icol < NUMCOLS);
        }

        /// <summary>
        /// Move a piece on the given board.
        /// </summary>
        /// <param name="board">The copy of the board on which to do the move.</param>
        /// <param name="irowStart">The row of the starting square.</param>
        /// <param name="icolStart">The column of the starting square.</param>
        /// <param name="irowStop">The row of the ending square.</param>
        /// <param name="icolStop">The column of the ending square.</param>
        /// <param name="bDisplay">true if we should display this move on the screen.</param>
        void MovePiece(ref Board board, int irowStart, int icolStart, int irowStop, int icolStop, bool bDisplay) {
            int piece = board.cells[irowStart, icolStart];
            int pieceType = piece & PieceType.Mask;
            int oldpiece = board.cells[irowStop, icolStop];
            board.cells[irowStop, icolStop] = (byte)piece;
            board.cells[irowStart, icolStart] = 0;

            // Special-case castling.
            int idxColor = ColorTo0Or1(piece & PieceColor.Mask);
            if ((PieceType.Mask & piece) == PieceType.King && (Math.Abs(icolStart - icolStop) > 1)) {
                // This is castling.  Also move the rook.  Determine which rook, and where.
                if (6 == icolStop) {
                    // Kingside castling.  Move rook.
                    board.cells[irowStop, 5] = board.cells[irowStop, 7];
                    board.cells[irowStop, 7] = 0;
                    board.bOKCastleKing[idxColor] = false;
                } else if (2 == icolStop) {
                    // Queenside castling.  Move rook. 
                    board.cells[irowStop, 3] = board.cells[irowStop, 0];
                    board.cells[irowStop, 0] = 0;
                    board.bOKCastleQueen[idxColor] = false;
                }
            }
            // Clear "OK to castle" flag(s) whenever rook or king moves.
            if (pieceType == PieceType.King) {
                board.bOKCastleKing[idxColor] = false;
                board.bOKCastleQueen[idxColor] = false;
            }
            if ((pieceType == PieceType.Rook)) {
                if (icolStart == 0) {
                    board.bOKCastleQueen[idxColor] = false;
                } else if (icolStart == 7) {
                    board.bOKCastleKing[idxColor] = false;
                }
            }

            // If a pawn reaches the last rank, promote it.
            // For now, always promote to Queen.
            if (pieceType == PieceType.Pawn && (irowStop == 0 || irowStop == NUMROWS - 1)) {
                int newPieceWithColor = PieceType.Queen | (piece & PieceColor.Mask);
                if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Promoting pawn at {RowColToAlgebraic(irowStop, icolStop)} to queen");
                board.cells[irowStop, icolStop] = (byte)newPieceWithColor;
            }

            if (0 != oldpiece) {
                board.capturedPieces[board.nCapturedPieces++] = (byte)oldpiece;
            }

            if (bDisplay) {
                // This causes the move to be displayed on the board.
                selectedRowStart = irowStart;
                selectedColStart = icolStart;
                selectedRowStop = irowStop;
                selectedColStop = icolStop;
                string captureMsg = oldpiece != 0 ? $"capturing {DescribePiece(oldpiece)}" : "";
                if (0 != (m_DebugBits & DBG_MOVES)) DebugOut($"Moved {DescribePiece(piece)} from {RowColToAlgebraic(irowStart, icolStart)} to {RowColToAlgebraic(irowStop, icolStop)} {captureMsg}");
            }
        }

        /// <summary>
        /// Determine whether a piece on a given square is under attack.
        /// </summary>
        /// <param name="board">The board in question</param>
        /// <param name="irow">The row of the piece (0-7)</param>
        /// <param name="icol">The column of the piece (0-7)</param>
        /// <param name="myColor">My color; the attacker must be of the other player</param>
        /// <returns>true if that square is being attacked by the other player</returns>
        bool IsSquareAttacked(ref Board board, int irow, int icol, int myColor) {
            bool bIsAttacked = false;
            int piece = board.cells[irow, icol];
            int pieceType = piece & PieceType.Mask;

            // First check for knights.
            foreach (int ir2 in aryKnightMoves) {
                foreach (int ic2 in aryKnightMoves) {
                    // A knight moves 2 squares in one direction, and 1 in the other.
                    // Our loops cover too many possibilities, so we skip processing
                    // if the number of squares in the two directions are equal.
                    if (Math.Abs(ic2) == Math.Abs(ir2)) continue;
                    int newRow = irow + ir2;
                    int newCol = icol + ic2;
                    if (IsLegalSquare(newRow, newCol)) {
                        int otherPiece = m_board.cells[newRow, newCol];
                        int otherColor = otherPiece & PieceColor.Mask;
                        otherPiece &= PieceType.Mask;
                        if (otherPiece == PieceType.Knight && otherColor != myColor) {
                            // An enemy knight is attacking our square.
                            bIsAttacked = true;
                            break;
                        }
                    }
                }
            }

            // Now look for various pieces in straight lines.
            // Start with vertical, up.
            int ir, ic;
            bool bFirstSquare = true;
            for (ir = irow - 1, ic = icol; ir >= 0; ir--, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Rook == otherPiece
                            || (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Now for vertical, down.
            for (ir = irow + 1, ic = icol, bFirstSquare = true; ir < NUMROWS; ir++, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Rook == otherPiece ||
                            (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Now for horizonal, right.
            for (ir = irow, ic = icol + 1, bFirstSquare = true; ic < NUMCOLS; ic++, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Rook == otherPiece ||
                            (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Now for horizonal, left.
            for (ir = irow, ic = icol - 1, bFirstSquare = true; ic >= 0; ic--, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Rook == otherPiece ||
                            (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Check diagonal up left.
            for (ir = irow - 1, ic = icol - 1, bFirstSquare = true; ir >= 0 && ic >= 0; ir--, ic--, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Bishop == otherPiece ||
                            (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Check diagonal down right.
            for (ir = irow + 1, ic = icol + 1, bFirstSquare = true; ir < NUMROWS && ic < NUMCOLS; ir++, ic++, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Bishop == otherPiece ||
                            (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Check diagonal down left.
            for (ir = irow + 1, ic = icol - 1, bFirstSquare = true; ir < NUMROWS && ic >= 0; ir++, ic--, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Bishop == otherPiece ||
                            (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Check diagonal up right.
            for (ir = irow - 1, ic = icol + 1, bFirstSquare = true; ir >= 0 && ic < NUMCOLS; ir--, ic++, bFirstSquare = false) {
                int otherPiece = board.cells[ir, ic];
                if (0 != otherPiece) {
                    int otherColor = otherPiece & PieceColor.Mask;
                    if (otherColor == myColor) {
                        // We run against our own piece, so no threat from this direction.
                        break;
                    } else {
                        // It's an opponent's piece.  Is it a threat?
                        otherPiece &= PieceType.Mask;
                        if (PieceType.Queen == otherPiece || PieceType.Bishop == otherPiece ||
                            (PieceType.King == otherPiece && bFirstSquare)) {
                            bIsAttacked = true;
                        } else {
                            // This opponent's piece is blocking his other pieces from attacking us.
                        }
                        break;
                    }
                }
            }

            // Check whether a pawn is attacking this square.
            int directionFromPieceToPawn = -1;
            if (myColor == PieceColor.Black) {
                directionFromPieceToPawn = 1;
            }
            // The row on which we are checking a pawn is one away from the current row;
            // the direction has been computed above.
            int testRow = irow + directionFromPieceToPawn;
            if (testRow >= 0 && testRow < NUMROWS) {
                // Now check two columns: to the left and right of the piece.
                // But make sure we don't go off the board.
                if (icol > 0) {
                    int otherPiece = board.cells[testRow, icol - 1];
                    int otherPieceType = otherPiece & PieceType.Mask;
                    int otherPieceColor = otherPiece & PieceColor.Mask;
                    if (otherPieceType == PieceType.Pawn && otherPieceColor != myColor) {
                        bIsAttacked = true;
                    }
                }
                if (icol < NUMCOLS - 1) {
                    int otherPiece = board.cells[testRow, icol + 1];
                    int otherPieceType = otherPiece & PieceType.Mask;
                    int otherPieceColor = otherPiece & PieceColor.Mask;
                    if (otherPieceType == PieceType.Pawn && otherPieceColor != myColor) {
                        bIsAttacked = true;
                    }
                }
            }

            return bIsAttacked;
        }

        /// <summary>
        /// Determine whether a king on a given board is under attack.
        /// </summary>
        /// <param name="board">The board in question</param>
        /// <param name="color">The color of the king we are checking</param>
        /// <returns>true if the king of that color is under attack</returns>
        bool KingIsUnderAttack(ref Board board, int color) {
            int irow, icol = 0, irowKing = -1, icolKing = -1;
            bool bFound = false;
            // Locate the king.
            // Start by looking in the cached location.
            int pieceLookingFor = PieceType.King | color;
            if (PieceColor.White == color) {
                irowKing = (int)board.cachedWhiteKingPosition & 0x7;
                icolKing = (int)(board.cachedWhiteKingPosition >> 3);
            } else {
                irowKing = (int)board.cachedBlackKingPosition & 0x7;
                icolKing = (int)(board.cachedBlackKingPosition >> 3);
            }
            if (pieceLookingFor == (board.cells[irowKing, icolKing] & (PieceType.Mask | PieceColor.Mask))) {
                bFound = true;
                //if (0 != (m_DebugBits & 0xff)) DebugOut($"Found king at cached location");
            } else {
                //if (0 != (m_DebugBits & 0xff)) DebugOut($"Did not find king at cached location");
                for (irow = 0; irow < NUMROWS && !bFound; irow++) {
                    for (icol = 0; icol < NUMCOLS; icol++) {
                        if (pieceLookingFor == (board.cells[irow, icol] & (PieceType.Mask | PieceColor.Mask))) {
                            bFound = true;
                            irowKing = irow;
                            icolKing = icol;
                            break;
                        }
                    }
                }
                if (!bFound) {
                    if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"** Error: cannot find {PieceColor.ToString(color)} {PieceType.ToString(PieceType.King)}\n{ComputeTextualBoard()}");
                } else {
                    if (PieceColor.White == color) {
                        board.cachedWhiteKingPosition = (uint)(NUMCOLS * icolKing + irowKing);
                    } else {
                        board.cachedBlackKingPosition = (uint)(NUMCOLS * icolKing + irowKing);
                    }
                }
            }

            bool bIsAttacked = IsSquareAttacked(ref board, irowKing, icolKing, color);
            return bIsAttacked;
        }

        void SaveBoardState(int irowStart, int icolStart, int irowStop, int icolStop,
            ref Board board, ref BoardSaveState state) {
            state.savedStart = board.cells[irowStart, icolStart];
            state.savedStop = board.cells[irowStop, icolStop];

            // In case we are castling, save the squares that might be affected by castling.
            // I'm trying to avoid too much logic here, as it's probably faster to store a
            // few extra bytes than to figure out whether the storing could be avoided.
            int irowCastle = irowStart == 0 ? 0 : NUMROWS - 1;
            state.savedBishopKing = board.cells[irowCastle, 5];
            state.savedQueen = board.cells[irowCastle, 3];
            state.savedRookKing = board.cells[irowCastle, NUMCOLS - 1];
            state.savedRookQueen = board.cells[irowCastle, 0];

            state.savedCastleKing0 = board.bOKCastleKing[0];
            state.savedCastleKing1 = board.bOKCastleKing[1];
            state.savedCastleQueen0 = board.bOKCastleQueen[0];
            state.savedCastleQueen1 = board.bOKCastleQueen[1];
            state.savedNCapturedPieces = board.nCapturedPieces;
        }

        void RestoreBoardState(int irowStart, int icolStart, int irowStop, int icolStop,
            ref Board board, ref BoardSaveState state) {
            board.cells[irowStart, icolStart] = state.savedStart;
            board.cells[irowStop, icolStop] = state.savedStop;

            int irowCastle = irowStart == 0 ? 0 : NUMROWS - 1;
            board.cells[irowCastle, 5] = state.savedBishopKing;
            board.cells[irowCastle, 3] = state.savedQueen;
            board.cells[irowCastle, NUMCOLS - 1] = state.savedRookKing;
            board.cells[irowCastle, 0] = state.savedRookQueen;

            board.bOKCastleKing[0] = state.savedCastleKing0;
            board.bOKCastleKing[1] = state.savedCastleKing1;
            board.bOKCastleQueen[0] = state.savedCastleQueen0;
            board.bOKCastleQueen[1] = state.savedCastleQueen1;

            board.nCapturedPieces = state.savedNCapturedPieces;
        }

        /// <summary>
        /// Add a move to a list of possible moves if the move is legal
        /// </summary>
        /// <param name="board">The copy of the board on which to do the move</param>
        /// <param name="myColor">The color of the piece being moved</param>
        /// <param name="irowStart">The starting row of the piece to move</param>
        /// <param name="icolStart">The starting colum of the piece to move</param>
        /// <param name="irowStop">The ending row of the piece to move</param>
        /// <param name="icolStop">The ending column of the piece to move</param>
        /// <param name="aryValidMoves">The array of possible moves to which we will add</param>
        /// <param name="nMoves">The number of active entries in aryValidMoves.  It may 
        /// have been incremented by the time we return.</param>
        void AddMoveIfKingNotUnderAttack(ref Board board, int myColor,
            int irowStart, int icolStart, int irowStop, int icolStop,
            ref ulong[] aryValidMoves, int offsetValidMoves, ref int nMoves) {
            BoardSaveState boardSaveState = new BoardSaveState();
            // Save the from and to squares.
            SaveBoardState(irowStart, icolStart, irowStop, icolStop, ref board, ref boardSaveState);

            // Tentatively move the piece.
            MovePiece(ref board, irowStart, icolStart, irowStop, icolStop, false);
            if (!KingIsUnderAttack(ref board, myColor)) {
                aryValidMoves[offsetValidMoves + nMoves++] = EncodeMoveFromRowsAndCols(irowStart, icolStart, irowStop, icolStop);
                //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"Valid move from {RowColToAlgebraic(irowStart, icolStart)} to {RowColToAlgebraic(irowStop, icolStop)}");
            } else {
                //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"Rejected move from {RowColToAlgebraic(irowStart, icolStart)} to {RowColToAlgebraic(irowStop, icolStop)} due to King in check");
            }
            // Undo the tentative move - we don't really want to change the board.
            RestoreBoardState(irowStart, icolStart, irowStop, icolStop, ref board, ref boardSaveState);
        }

        int ComputeLegalMovesForPawn(int irow, int icol, ref ulong[] aryValidMoves, int offsetValidMoves, ref int nMoves) {
            int piece = m_board.cells[irow, icol];
            int myColor = piece & PieceColor.Mask;
            if (PieceColor.White == myColor /*&& m_bWhiteOnBottom || PieceColor.Black == myColor && !m_bWhiteOnBottom*/) {
                // Moves are from bottom to top.
                // This check shouldn't really be necessary, since a pawn can't be on the last rank.
                if (irow > 0) {
                    if (0 == m_board.cells[irow - 1, icol]) {
                        // Square is empty.
                        AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow - 1, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    }
                    if (irow == NUMROWS - 2) {
                        // Pawn is in initial position, so move of 2 squares may be available.
                        if (0 == m_board.cells[irow - 1, icol] && 0 == m_board.cells[irow - 2, icol]) {
                            AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow - 2, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                        }
                    }
                    // Now for captures. 
                    if (icol > 0) {
                        if (0 != m_board.cells[irow - 1, icol - 1] && ColorOfCell(m_board.cells[irow - 1, icol - 1]) != myColor) {
                            AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow - 1, icol - 1, ref aryValidMoves, offsetValidMoves, ref nMoves);
                        }
                    }
                    if (icol < NUMCOLS - 1) {
                        if (0 != m_board.cells[irow - 1, icol + 1] && ColorOfCell(m_board.cells[irow - 1, icol + 1]) != myColor) {
                            AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow - 1, icol + 1, ref aryValidMoves, offsetValidMoves, ref nMoves);
                        }
                    }
                }
            } else {
                // Moves are from top to bottom.
                // Move one ahead?
                if (irow < NUMROWS - 1) {
                    if (0 == m_board.cells[irow + 1, icol]) {
                        // Square is empty.
                        AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow + 1, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    }
                    if (irow == 1) {
                        // Pawn is in initial position, so move of 2 squares may be available.
                        if (0 == m_board.cells[irow + 1, icol] && 0 == m_board.cells[irow + 2, icol]) {
                            AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow + 2, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                        }
                    }
                    // Now for captures. 
                    if (icol > 0) {
                        if (0 != m_board.cells[irow + 1, icol - 1] && ColorOfCell(m_board.cells[irow + 1, icol - 1]) != myColor) {
                            AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow + 1, icol - 1, ref aryValidMoves, offsetValidMoves, ref nMoves);
                        }
                    }
                    if (icol < NUMCOLS - 1) {
                        if (0 != m_board.cells[irow + 1, icol + 1] && ColorOfCell(m_board.cells[irow + 1, icol + 1]) != myColor) {
                            AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow + 1, icol + 1, ref aryValidMoves, offsetValidMoves, ref nMoves);
                        }
                    }
                }
            }

            return nMoves;
        }

        void ComputeLegalMovesForKnight(int irow, int icol, ref ulong[] aryValidMoves, int offsetValidMoves, ref int nMoves) {
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
                        //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"Knight at ({irow},{icol}) to move to ({newRow},{newCol}): myColor={myColor} otherColor={otherColor} bMoveOK={bMoveOK}");
                        if (bMoveOK) {
                            AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, newRow, newCol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                        }
                    }
                }
            }
        }

        void ComputeLegalMovesForRook(int irow, int icol, ref ulong[] aryValidMoves, int offsetValidMoves, ref int nMoves) {
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
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;

                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                }
            }

            for (ic = icol + 1; ic < NUMCOLS; ic++) {
                otherPiece = m_board.cells[irow, ic];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;
                if (0 == otherPiece) {
                    // Empty square; OK
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, irow, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
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
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                }
            }
            for (ir = irow + 1; ir < NUMROWS; ir++) {
                otherPiece = m_board.cells[ir, icol];
                otherColor = otherPiece & PieceColor.Mask;
                otherPiece &= PieceType.Mask;

                if (0 == otherPiece) {
                    // Empty square; OK
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  Let's say it's OK, though we need to flesh this out more.
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                }
            }
        }

        void ComputeLegalMovesForBishop(int irow, int icol, ref ulong[] aryValidMoves, int offsetValidMoves, ref int nMoves) {
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
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
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
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
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
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
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
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                } else if (myColor == otherColor) {
                    // We have gotten to one of our own pieces.  We can't go further in this direction.
                    break;
                } else {
                    // Opponent's square.  
                    AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                }
            }
        }

        void ComputeLegalMovesForKing(int irow, int icol, ref ulong[] aryValidMoves, int offsetValidMoves, ref int nMoves) {
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
                    // We mustn't stay in one place.
                    if (deltacol == 0 && deltarow == 0) continue;

                    otherPiece = m_board.cells[ir, ic];
                    otherColor = otherPiece & PieceColor.Mask;
                    otherPiece &= PieceType.Mask;
                    if (0 == otherPiece || myColor != otherColor) {
                        // Empty square or capturing other piece.  
                        AddMoveIfKingNotUnderAttack(ref m_board, myColor, irow, icol, ir, ic, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    }
                }
            }
            // Check for castling.
            int idxCastleSide = ColorTo0Or1(myColor);
            // OK to castle king side?
            if (m_board.bOKCastleKing[idxCastleSide]) {
                if (myColor == PieceColor.White) {
                    // Assume white at bottom of board.  Check for empty squares:
                    // RNBQK__R
                    if (0 == m_board.cells[NUMROWS - 1, 5] && 0 == m_board.cells[NUMROWS - 1, 6]) {
                        // Squares between are empty.  Are they attacked?
                        if (!IsSquareAttacked(ref m_board, NUMROWS - 1, 5, myColor) &&
                            !IsSquareAttacked(ref m_board, NUMROWS - 1, 6, myColor)) {
                            aryValidMoves[offsetValidMoves + nMoves++] = EncodeMoveFromRowsAndCols(NUMROWS - 1, 4, NUMROWS - 1, 6);
                            if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Adding kingside castling as legal move for white");
                        }
                    }
                } else {
                    // Assume black at top of board.  Check for empty squares:
                    // RNBQK__R
                    if (0 == m_board.cells[0, 5] && 0 == m_board.cells[0, 6]) {
                        // Squares between are empty.  Are they attacked?
                        if (!IsSquareAttacked(ref m_board, 0, 5, myColor) &&
                            !IsSquareAttacked(ref m_board, 0, 6, myColor)) {
                            aryValidMoves[offsetValidMoves + nMoves++] = EncodeMoveFromRowsAndCols(0, 4, 0, 6);
                            if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Adding kingside castling as legal move for black");
                        }
                    }
                }
            }

            // OK to castle queen side?
            if (m_board.bOKCastleQueen[idxCastleSide]) {
                if (myColor == PieceColor.White) {
                    // Assume white at bottom of board.  Check for empty squares:
                    // R___KBNR
                    if (0 == m_board.cells[NUMROWS - 1, 1] && 0 == m_board.cells[NUMROWS - 1, 2] &&
                        0 == m_board.cells[NUMROWS - 1, 3]) {
                        // Squares between are empty.  Are they attacked?
                        if (!IsSquareAttacked(ref m_board, NUMROWS - 1, 1, myColor) &&
                            !IsSquareAttacked(ref m_board, NUMROWS - 1, 2, myColor) &&
                            !IsSquareAttacked(ref m_board, NUMROWS - 1, 3, myColor)) {
                            aryValidMoves[offsetValidMoves + nMoves++] = EncodeMoveFromRowsAndCols(NUMROWS - 1, 4, NUMROWS - 1, 2);
                            if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Adding queenside castling as legal move for white");
                        }
                    }
                } else {
                    // Assume black at top of board.  Check for empty squares:
                    // R___KBNR
                    if (0 == m_board.cells[0, 1] && 0 == m_board.cells[0, 2] &&
                        0 == m_board.cells[0, 2]) {
                        // Squares between are empty.  Are they attacked?
                        if (!IsSquareAttacked(ref m_board, 0, 1, myColor) &&
                            !IsSquareAttacked(ref m_board, 0, 2, myColor) &&
                            !IsSquareAttacked(ref m_board, 0, 3, myColor)) {
                            aryValidMoves[offsetValidMoves + nMoves++] = EncodeMoveFromRowsAndCols(0, 4, 0, 2);
                            if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Adding queenside castling as legal move for black");
                        }
                    }
                }
            }
        }

        void ComputeLegalMovesForPiece(int irow, int icol, ref ulong[] aryValidMoves, int offsetValidMoves, ref int nMoves) {
            //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"ComputeLegalMovesForPiece called for {DescribePiece(m_board.cells[irow,icol])} at {RowColToAlgebraic(irow,icol)}");
            int pieceType = m_board.cells[irow, icol];
            pieceType &= PieceType.Mask;
            switch (pieceType) {
                case PieceType.Rook:
                    ComputeLegalMovesForRook(irow, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                case PieceType.Pawn:
                    ComputeLegalMovesForPawn(irow, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                case PieceType.Knight:
                    ComputeLegalMovesForKnight(irow, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                case PieceType.Bishop:
                    ComputeLegalMovesForBishop(irow, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                case PieceType.Queen:
                    ComputeLegalMovesForRook(irow, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    ComputeLegalMovesForBishop(irow, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
                case PieceType.King:
                    ComputeLegalMovesForKing(irow, icol, ref aryValidMoves, offsetValidMoves, ref nMoves);
                    break;
            }
        }

        void ComputeLegalMovesForSide(ref Board board, int pieceColor,
            ref ulong[] validMovesForComputer, int offsetValidMoves, ref int nValidMovesForComputer) {
            nValidMovesForComputer = 0;
            for (int irow = 0; irow < NUMROWS; irow++) {
                for (int icol = 0; icol < NUMCOLS; icol++) {
                    if ((board.cells[irow, icol] & PieceColor.Mask) == pieceColor) {
                        byte piece = board.cells[irow, icol];
                        int color = piece & PieceColor.Mask;
                        int pieceType = piece & PieceType.Mask;
                        //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"ComputeLegalMovesForComputer: looking at {PieceColor.ToString(color)} {PieceType.ToString(pieceType)} at ({irow},{icol}) ");
                        ComputeLegalMovesForPiece(irow, icol, ref validMovesForComputer, offsetValidMoves, ref nValidMovesForComputer);
                    }
                }
            }
            // Now validMovesForComputer has all the legal moves we could make.
        }

        void ComputeLegalMovesForComputer(ref Board board) {
            ComputeLegalMovesForSide(ref board, m_ComputersColor, ref m_ValidMovesForComputer, 0, ref m_nValidMovesForComputer);
        }

        void DumpValidMoves(string msg, ref ulong[] aryValidMoves, int nValidMoves) {
            string strValidMoves = "";
            for (int j = 0; j < nValidMoves; j++) {
                var move = aryValidMoves[j];
                int irowStart, icolStart, irowStop, icolStop;
                DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
                strValidMoves += " " + RowColToAlgebraic(irowStart, icolStart);
                strValidMoves += RowColToAlgebraic(irowStop, icolStop);
            }
            if (0 != (m_DebugBits & DBG_MISC)) DebugOut(msg + ":" + strValidMoves);
        }

        // Evaluate a board position, returning a score that is positive if things look
        // good for White, or negative if Black is doing better.
        int EvaluateBoard(ref Board board) {
            int score = 0;
            for (int irow = 0; irow < NUMROWS; irow++) {
                for (int icol = 0; icol < NUMROWS; icol++) {
                    score += m_aryPieceBaseValue[board.cells[irow, icol]];
                    //int piece = board.cells[irow, icol];
                    //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"EvaluateBoard: {DescribePiece(piece)} at {RowColToAlgebraic(irow,icol)} has score {m_aryPieceBaseValue[board.cells[irow, icol]]}");
                }
            }
            //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"EvaluateBoard returning {score}");
            return score;
        }

        void ChooseAndMakeMoveForComputerSimple(ref Board board, ref ulong[] aryValidMoves, int nValidMoves) {
            if (nValidMoves > 0) {
                DumpValidMoves("Valid moves for computer", ref aryValidMoves, nValidMoves);
                ulong move = 0;
                int irowStart = -1, icolStart = -1, irowStop = -1, icolStop = -1;
                if (0 == m_lookaheadPlies) {
                    // For now, choose a piece at random.
                    int idxMove = m_random.Next(nValidMoves);
                    move = aryValidMoves[idxMove];
                    //DebugOut($"For 0 ply lookahead chose move at random: {DescribeMove(move)}");
                } else {
                    // Loop through all legal moves, evaluating the board position 
                    // that would result if we made each of those moves.
                    // Store the results in m_ScoresForValidMovesForComputer.
                    int score;
                    BoardSaveState boardSaveState = new BoardSaveState();
                    for (int idxMove = 0; idxMove < nValidMoves; idxMove++) {
                        move = aryValidMoves[idxMove];
                        DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
                        //int piece = board.cells[irowStart, icolStart];
                        SaveBoardState(irowStart, icolStart, irowStop, icolStop, ref board, ref boardSaveState);
                        MovePiece(ref board, irowStart, icolStart, irowStop, icolStop, false);
                        score = EvaluateBoard(ref board);
                        // We will search for the move with the highest score. 
                        // So if we (the computer) are playing black piece, negate the score,
                        // so that moves that are favorable for Black will result in high scores.
                        if (m_ComputersColor == PieceColor.Black) score = -score;
                        m_ScoresForValidMovesForComputer[idxMove] = score;
                        RestoreBoardState(irowStart, icolStart, irowStop, icolStop, ref board, ref boardSaveState);
                    }

                    // Find the move with the highest score.  (There may be more than one with that score.)
                    int highScore = -99999, idxOfHigh = 0;
                    for (int idxMove = 0; idxMove < nValidMoves; idxMove++) {
                        if (m_ScoresForValidMovesForComputer[idxMove] > highScore) {
                            highScore = m_ScoresForValidMovesForComputer[idxMove];
                            idxOfHigh = idxMove;
                        }
                    }
                    // Count the number of moves with that score.
                    int nWithHighScore = 0;
                    for (int idxMove = 0; idxMove < nValidMoves; idxMove++) {
                        if (m_ScoresForValidMovesForComputer[idxMove] == highScore) {
                            nWithHighScore++;
                        }
                    }

                    // Choose a move at random from those tied with the highest score.
                    int nthRandom = m_random.Next(nWithHighScore);
                    if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"I found {nWithHighScore} moves with best score {highScore}; will choose # {nthRandom}");
                    for (int idxMove = 0; idxMove < nValidMoves; idxMove++) {
                        if (m_ScoresForValidMovesForComputer[idxMove] == highScore) {
                            if (nthRandom-- <= 0) {
                                move = aryValidMoves[idxMove];
                                break;
                            }
                        }
                    }
                }
                DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
                int piece = board.cells[irowStart, icolStart];
                //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"ChooseMoveForComputer: nValidMoves={nValidMoves}; I chose index {idxMove} which is {move}");
                if (0!=(m_DebugBits & DBG_MISC)) DebugOut($"For {m_lookaheadPlies}-ply lookahead, computer will move {DescribePiece(piece)} from {RowColToAlgebraic(irowStart, icolStart)} to {RowColToAlgebraic(irowStop, icolStop)}");
                MovePiece(ref board, irowStart, icolStart, irowStop, icolStop, true);
            } else {
                if (KingIsUnderAttack(ref m_board, m_ComputersColor)) {
                    SetMessage("Human wins via checkmate!");
                } else {
                    SetMessage("Stalemate!");
                }
                m_bGameOver = true;
            }
        }

        void ChooseAndMakeMoveForComputer(ref Board board) {
            UseWaitCursor = true;
            if (m_lookaheadPlies <= 1) {
                ComputeLegalMovesForComputer(ref m_board);
                ChooseAndMakeMoveForComputerSimple(ref m_board, ref m_ValidMovesForComputer, m_nValidMovesForComputer);
            } else {
                m_stopwatch.Reset();
                m_stopwatch.Start();
                ulong moveSequence = 0, bestLine = 0;
                long result = ChooseMove(ref m_board, m_ComputersColor, 1, 0, moveSequence, out bestLine);
                m_stopwatch.Stop();
                var elapsed = m_stopwatch.Elapsed;
                var strElapsed = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
                if (RESULT_NO_VALID_MOVES == result) {
                    if (KingIsUnderAttack(ref m_board, m_ComputersColor)) {
                        SetMessage("Human wins!");
                    } else {
                        SetMessage("Stalemate!");
                    }
                    m_bGameOver = true;
                } else {
                    int score = (int)(result >> 32);
                    ulong move = (0xffffffff & (ulong)result);
                    int irowStart, icolStart, irowStop, icolStop;
                    DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
                    int piece = m_board.cells[irowStart, icolStart];
                    //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"ChooseMoveForComputer: nValidMoves={nValidMoves}; I chose index {idxMove} which is {move}");
                    if (0 != (m_DebugBits & DBG_MOVES)) DebugOut($"Computer will move {DescribePiece(piece)} from {RowColToAlgebraic(irowStart, icolStart)} to {RowColToAlgebraic(irowStop, icolStop)} with score {score}; took {strElapsed} bestLine {DescribeBestLine(bestLine)}");
                    MovePiece(ref m_board, irowStart, icolStart, irowStop, icolStop, true);
                }
            }
            int nValidMovesForHuman = 0;
            ComputeLegalMovesForSide(ref m_board, OtherPieceColor(m_ComputersColor), ref m_ValidMoves, 0, ref nValidMovesForHuman);
            if (0 == nValidMovesForHuman) {
                m_bGameOver = true;
                if (KingIsUnderAttack(ref m_board, PieceColor.Mask - m_ComputersColor)) {
                    SetMessage("Computer wins!");
                } else {
                    SetMessage("Stalemate!");
                }
            }
            UseWaitCursor = false;

        }

        // Choose the next move to make.
        // Entry:   board   is the current board position.
        //          colorMoving is which side is moving (as a PieceColor value).
        //          iply    is the number of plies from the original position; we use
        //                  this is know when to stop looking ahead.
        //          offsetValidMoves    is the index into m_validMoves for the next
        //                  unused position in the array.  This will initially be 0, but
        //                  will be > 0 for recursive calls.
        //          moveSequence    has an encoding of the best moves computed so far;
        //                  This will be 0 for the initial call, but != 0 for recursive calls.
        // Exit:  Returns both the score of the best move so far, and the move, encoded as:
        //        score << 32 | move
        long ChooseMove(ref Board board, int colorMoving, int iply, int offsetValidMoves, ulong bestLineSoFar, out ulong bestLine) {
            if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"ChooseMove called for color {colorMoving} ply {iply} offsetValidMoves {offsetValidMoves}");
            int nValidMoves = 0;
            bestLine = 0;
            ComputeLegalMovesForSide(ref board, colorMoving, ref m_ValidMoves, offsetValidMoves, ref nValidMoves);
            if (1 == iply && 0 == nValidMoves) {
                return RESULT_NO_VALID_MOVES;
            }
            bool maximize;
            long bestScore;
            if (((1 == (iply & 1)) && m_ComputersColor == PieceColor.Black) ||
               ((0 == (iply & 1)) && m_ComputersColor == PieceColor.White)) {
                // We should find the most negative score.
                maximize = false;
                bestScore = 99999;
            } else {
                maximize = true;
                bestScore = -99999;
            }
            long score;
            ulong bestMove = 0, bestLineOut = 0;
            BoardSaveState boardSaveState = new BoardSaveState();
            int irowStart = -1, icolStart = -1, irowStop = -1, icolStop = -1;
            string msg = "";
            string strBestMove = "";
            bool bRecurse = (iply < m_lookaheadPlies);
            for (int imove = 0; imove < nValidMoves; imove++) {
                var move = m_ValidMoves[imove + offsetValidMoves];
                DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
                string desc = DescribeMove(move);
#if DEBUG_UNEXPECTEDMOVE
                bool bPuzzlingMove = false;
                if ("c3e4" == desc && iply == 1) bPuzzlingMove = true;
                if (bPuzzlingMove) {
                    if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"ZZ Start of considering puzzling move.");
                }
#endif
                if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Considering move {RowColToAlgebraic(irowStart, icolStart)}{RowColToAlgebraic(irowStop, icolStop)} ply {iply}");
                SaveBoardState(irowStart, icolStart, irowStop, icolStop, ref board, ref boardSaveState);
                MovePiece(ref board, irowStart, icolStart, irowStop, icolStop, false);
                if (bRecurse) {
                    score = ChooseMove(ref board, PieceColor.Black ^ colorMoving, iply + 1, 
                        offsetValidMoves + nValidMoves, (bestLineSoFar << 12) | move, out bestLineOut);
                    score = (score >> 32);
                } else {
                    // Evaluate board only at leaf nodes.
                    score = EvaluateBoard(ref board);
                }
                //if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"Score for move {imove}: {DescribeMove(move)} ply {iply} was {score}; best so far={bestScore} maximize={maximize}");
#if DEBUG_UNEXPECTEDMOVE
                if (bPuzzlingMove) if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"ZZ Line for puzzling move: {DescribeBestLine(bestLineOut)} score {score}");
                if(DescribeBestLine(bestLineSoFar).StartsWith("c3e4 d7d5 ")) {
                    if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"WW For ply {iply} bestLineSoFar {DescribeBestLine(bestLineSoFar)} score {score} for move {desc}");
                }
#endif
                RestoreBoardState(irowStart, icolStart, irowStop, icolStop, ref board, ref boardSaveState);
                if (maximize) {
                    if (score >= bestScore) {
                        bestScore = score;
                        bestMove = move;
                        bestLine = bestLineOut;
                        strBestMove = DescribeMove(move);
                        if (0 != (m_DebugBits & DBG_MISC)) msg = $"Best score at ply {iply} was for {DescribeMove(move)} and is {score}; maximize={maximize}";
                    }
                } else {
                    if (score <= bestScore) {
                        bestScore = score;
                        bestMove = move;
                        bestLine = bestLineOut;
                        strBestMove = DescribeMove(move);
                        if (0 != (m_DebugBits & DBG_MISC)) msg = $"Best score at ply {iply} was for {DescribeMove(move)} and is {score}; maximize={maximize}";
                    }
                }
#if DEBUG_UNEXPECTEDMOVE
                if(bPuzzlingMove) {
                    if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"ZZ Done considering puzzling move.  strBestMove was {strBestMove}; bestScore {bestScore} ply {iply} bestLine {DescribeBestLine(bestLine)}");
                }
#endif
            }
            if (0 != (m_DebugBits & DBG_MISC)) DebugOut(msg);
            long result = (bestScore << 32) | (long)bestMove;
            if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"ChooseMove for color {colorMoving} ply {iply} returning best score {bestScore} bestMove {DescribeMove(bestMove)} of {nValidMoves} moves");
            if(!bRecurse) {
                bestLine = (bestLineSoFar << 12) | bestMove;
            }
            return result;
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

                for (int irow = 2; irow < 6; irow++) {
                    board.cells[irow, icol] = 0;
                }
            }

            board.cells[7, 0] = PieceColor.White | PieceType.Rook;
            board.cells[7, 1] = PieceColor.White | PieceType.Knight;
            board.cells[7, 2] = PieceColor.White | PieceType.Bishop;
            board.cells[7, 3] = PieceColor.White | PieceType.Queen;
            board.cells[7, 4] = PieceColor.White | PieceType.King;
            board.cells[7, 5] = PieceColor.White | PieceType.Bishop;
            board.cells[7, 6] = PieceColor.White | PieceType.Knight;
            board.cells[7, 7] = PieceColor.White | PieceType.Rook;

            board.bOKCastleKing[0] = true;
            board.bOKCastleKing[1] = true;
            board.bOKCastleQueen[0] = true;
            board.bOKCastleQueen[1] = true;

            board.nCapturedPieces = 0;

            // Temporary extra pieces
            //board.cells[2, 1] = PieceColor.Black | PieceType.Rook;
            //board.cells[3, 2] = PieceColor.White | PieceType.King;
            //board.cells[3, 4] = PieceColor.Black | PieceType.King;
            //board.cells[5, 2] = PieceColor.Black | PieceType.King;
            //board.cells[4, 5] = PieceColor.White | PieceType.Queen;
            //board.cells[5, 5] = PieceColor.White | PieceType.Queen;

            // For testing display of captured pieces.
            //board.capturedPieces[board.nCapturedPieces++] = PieceColor.White | PieceType.Pawn;
            //board.capturedPieces[board.nCapturedPieces++] = PieceColor.White | PieceType.Rook;
            //board.capturedPieces[board.nCapturedPieces++] = PieceColor.Black | PieceType.Bishop;
            //board.capturedPieces[board.nCapturedPieces++] = PieceColor.Black | PieceType.Knight;
            //board.capturedPieces[board.nCapturedPieces++] = PieceColor.White | PieceType.Pawn;
        }

        void ClearHighlights() {
            selectedRowStart = NOT_SELECTED;
            selectedColStart = NOT_SELECTED;
            selectedRowStop = NOT_SELECTED;
            selectedColStop = NOT_SELECTED;
        }

        void InitializeGame() {
            CreateInitialBoard(ref m_board);
            ClearHighlights();
            m_nValidMovesForOnePiece = 0;
            m_bGameOver = false;
            ClearMessage();
        }

        private void ChessR1Form_Load(object sender, EventArgs e) {
            InitializeGame();
            if (m_cmdLineSettings.bStartWithComputerWhite) {
                computerPlaysWhiteToolStripMenuItem_Click(null, null);
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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void computerPlaysBlackToolStripMenuItem_Click(object sender, EventArgs e) {
            SetComputersColor(PieceColor.Black);
            InitializeGame();
            Invalidate();
        }

        private void computerPlaysWhiteToolStripMenuItem_Click(object sender, EventArgs e) {
            SetComputersColor(PieceColor.White);
            InitializeGame();
            //ComputeLegalMovesForComputer(ref m_board);
            //ChooseAndMakeMoveForComputer(ref m_board, ref m_ValidMovesForComputer, m_nValidMovesForComputer);
#if false
            long result = ChooseMove(ref m_board, PieceColor.White, 1, 0);
            int irowStart, icolStart, irowStop, icolStop;
            ulong move = (0xffffffff & (ulong)result);
            DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
            int piece = m_board.cells[irowStart, icolStart];
            MovePiece(ref m_board, irowStart, icolStart, irowStop, icolStop, true);
#else
            ChooseAndMakeMoveForComputer(ref m_board);
#endif

            Invalidate();
        }

        private void ChessR1Form_FormClosing(object sender, FormClosingEventArgs e) {
            m_swDebug.Flush();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            FormSettings formSettings = new FormSettings();
            formSettings.Controls["textBoxDebugMask"].Text = m_DebugBits.ToString();
            formSettings.Controls["numericUpDownLookahead"].Text = m_lookaheadPlies.ToString();
            DialogResult result = formSettings.ShowDialog();
            if (result == DialogResult.OK) {
                ulong.TryParse(formSettings.Controls["textBoxDebugMask"].Text, out m_DebugBits);
                int.TryParse(formSettings.Controls["numericUpDownLookahead"].Text, out m_lookaheadPlies);
            }
        }

        // Process File | Open menu command, to open FEN file and replace the current game state
        // with the contents of that file.
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Forsyth–Edwards Notation (*.fen)|*.fen|All files (*.*)|*.*";
            openFileDialog1.Title = "Replace current game with board postion from FEN file";
            openFileDialog1.ShowDialog();
            if(openFileDialog1.FileName != "") {
                int colorToMove;
                m_board.OpenFromFEN(openFileDialog1.FileName, out colorToMove);
                m_ComputersColor = PieceColor.Black - colorToMove;
                m_bWhiteOnBottom = (m_ComputersColor == PieceColor.Black);
                Invalidate();
            }
        }

        // Process File | Save menu command, to save the current state as a FEN file.
        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Forsyth–Edwards Notation|*.fen";
            saveFileDialog1.Title = "Save board position in FEN format";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "") { 
                // We save only when it's the human's turn to move.  So the color to move
                // is always the opposite of the computer's color.
                int colorToMove = PieceColor.Black - m_ComputersColor;
                m_board.SaveAsFEN(saveFileDialog1.FileName, colorToMove);
            }
        }

        private void MouseRightClickEvent(object sender, MouseEventArgs e, int curRow, int curCol) {
            FormChoosePiece form = new FormChoosePiece();
            form.ShowDialog();
            // When we get here, the user has closed the dialog to choose a piece.
            if (form.PlaceAction == PlacePieceAction.ACTION_PLACE_PIECE) {
                m_board.cells[curRow, curCol] = (byte)(form.PlacePiece | form.PlaceColor);
            } else if (form.PlaceAction == PlacePieceAction.ACTION_EMPTY) {
                m_board.cells[curRow, curCol] = 0;
            }
            if (NOT_SELECTED == selectedColStop && NOT_SELECTED != selectedColStart) {
                // A piece had already been selected, so recalculate its legal moves.
                m_nValidMovesForOnePiece = 0;
                ComputeLegalMovesForPiece(selectedRowStart, selectedColStart, ref m_ValidMovesForOnePiece, 0, ref m_nValidMovesForOnePiece);
                if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"After modifying board, recalculated {m_nValidMovesForOnePiece} moves for piece at ({selectedRowStart},{selectedColStart})");

            }
            Invalidate();
        }

        private void ChessR1Form_MouseDown(object sender, MouseEventArgs e) {
            if (m_bGameOver) return;
            m_swDebug.Flush();
            bool bWithinASquare = false;
            int prevCol = selectedColStart;
            int prevRow = selectedRowStart;
            int curCol = NOT_SELECTED;
            int curRow = NOT_SELECTED;
            bool bASquareWasAlreadySelected = (NOT_SELECTED != prevCol);

            // Implicit conversion from Point to PointF.
            PointF pointMouse = e.Location;
            if (pointMouse.X >= offsetLeft && pointMouse.X < offsetLeft + NUMCOLS * squareSize) {
                if (pointMouse.Y > offsetTop && pointMouse.Y < offsetTop + NUMROWS * squareSize) {
                    int curColRaw = (int)((pointMouse.X - offsetLeft) / squareSize);
                    curCol = this.ComputeEffectiveColForDisplay(curColRaw);
                    int curRowRaw = (int)((pointMouse.Y - offsetTop) / squareSize);
                    curRow = ComputeEffectiveRowForDisplay(curRowRaw);
                    bWithinASquare = true;
                }
            }

            if (e.Button == MouseButtons.Right) {
                if (bWithinASquare) {
                    MouseRightClickEvent(sender, e, curRow, curCol);
                }
                return;
            }

            if (bWithinASquare) {
                if (!bASquareWasAlreadySelected) {
                    if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"MouseDown: at algebraic {RowColToAlgebraic(curRow, curCol)}");
                    ComputeLegalMovesForPiece(curRow, curCol, ref m_ValidMovesForOnePiece, 0, ref m_nValidMovesForOnePiece);
                }
            }

            // If both a start and stop square are still defined, clear all.
            if (NOT_SELECTED != selectedColStart && NOT_SELECTED != selectedColStop) {
                selectedRowStart = NOT_SELECTED;
                selectedColStart = NOT_SELECTED;
                selectedRowStop = NOT_SELECTED;
                selectedColStop = NOT_SELECTED;
                m_nValidMovesForOnePiece = 0;
            }

            if (bWithinASquare) {
                if (bASquareWasAlreadySelected) {
                    // We have selected a square, and there was a square already selected.
                    // Is the new square different than the old?
                    if (prevCol != curCol || prevRow != curRow) {
                        // Yes, they are different, so it means the player is trying
                        // to move there.  Is this a legal place to move that piece?
                        ulong desiredLoc = EncodePositionFromRowAndCol(curRow, curCol);
                        bool bDidMove = false;
                        for (int j = 0; j < m_nValidMovesForOnePiece; j++) {
                            if ((m_ValidMovesForOnePiece[j] & POSITION_BITMASK) == desiredLoc) {
                                // Yes, it's legal to move this piece here, so move it.
                                //byte pieceBeingMoved = m_board.cells[selectedRowStart, selectedColStart];
                                ////if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"We will move {pieceBeingMoved} from row {selectedRowStart} col {selectedColStart} to row {curRow} col {curCol}");
                                //m_board.cells[curRow, curCol] = pieceBeingMoved;
                                //m_board.cells[selectedRowStart, selectedColStart] = 0;
                                MovePiece(ref m_board, selectedRowStart, selectedColStart, curRow, curCol, true);
                                if (0 != (m_DebugBits & DBG_MISC)) DebugOut($"After human move, board is now\r\n{ComputeTextualBoard()}");
                                m_swDebug.Flush();

                                selectedRowStop = curRow;
                                selectedColStop = curCol;

                                // Now that the piece has moved, erase its previous legal moves.
                                m_nValidMovesForOnePiece = 0;
                                bDidMove = true;

#if false
                                UseWaitCursor = true;
#if false
                                ComputeLegalMovesForComputer(ref m_board);
                                ChooseAndMakeMoveForComputer(ref m_board, ref m_ValidMovesForComputer, m_nValidMovesForComputer);
#else
                                m_stopwatch.Reset();
                                m_stopwatch.Start();
                                long result = ChooseMove(ref m_board, m_ComputersColor, 1, 0);
                                m_stopwatch.Stop();
                                var elapsed = m_stopwatch.Elapsed;
                                var strElapsed = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
                                if (RESULT_NO_VALID_MOVES == result) {
                                    if(KingIsUnderAttack(ref m_board, m_ComputersColor)) {
                                        SetMessage("Human wins!");
                                    } else {
                                        SetMessage("Stalemate!");
                                    }
                                    m_bGameOver = true;
                                } else {
                                    int score = (int)(result >> 32);
                                    ulong move = (0xffffffff & (ulong)result);
                                    int irowStart, icolStart, irowStop, icolStop;
                                    DecodeMoveFromInt(move, out irowStart, out icolStart, out irowStop, out icolStop);
                                    int piece = m_board.cells[irowStart, icolStart];
                                    //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"ChooseMoveForComputer: nValidMoves={nValidMoves}; I chose index {idxMove} which is {move}");
                                    if (0 != (m_DebugBits & DBG_MOVES)) DebugOut($"Computer will move {DescribePiece(piece)} from {RowColToAlgebraic(irowStart, icolStart)} to {RowColToAlgebraic(irowStop, icolStop)} with score {score}; took {strElapsed}");
                                    MovePiece(ref m_board, irowStart, icolStart, irowStop, icolStop, true);
                                }
#endif
                                UseWaitCursor = false;
#else
                                ChooseAndMakeMoveForComputer(ref m_board);
#endif
                                //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"MouseDown: board is now\r\n{ComputeTextualBoard()}");
                                break;
                            } else {
                                //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"Can't move: desired={desiredLoc} m_ValidMoves[j]={m_ValidMoves[j]}");
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
                            m_nValidMovesForOnePiece = 0;
                            ComputeLegalMovesForPiece(curRow, curCol, ref m_ValidMovesForOnePiece, 0, ref m_nValidMovesForOnePiece);
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
                    ComputeLegalMovesForPiece(curRow, curCol, ref m_ValidMovesForOnePiece, 0, ref m_nValidMovesForOnePiece);
                }
            } else {
                selectedRowStart = NOT_SELECTED;
                selectedColStart = NOT_SELECTED;
                selectedRowStop = NOT_SELECTED;
                selectedColStop = NOT_SELECTED;
                m_nValidMovesForOnePiece = 0;
            }

            //if (NOT_SELECTED == selectedColStart) {
            //    if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut("Click detected off the board");
            //}
            Invalidate();
            //if (0!=(m_DebugBits & DBG_NORMAL)) DebugOut($"MouseDown end: board is now\r\n{ComputeTextualBoard()}");
            m_swDebug.Flush();
        }
    }
}
