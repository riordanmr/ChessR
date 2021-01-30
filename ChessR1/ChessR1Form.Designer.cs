
namespace ChessR1
{
    partial class ChessR1Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.textBoxBoard = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBoxBoard
            // 
            this.textBoxBoard.Font = new System.Drawing.Font("Lucida Console", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxBoard.Location = new System.Drawing.Point(211, 1153);
            this.textBoxBoard.Multiline = true;
            this.textBoxBoard.Name = "textBoxBoard";
            this.textBoxBoard.Size = new System.Drawing.Size(270, 191);
            this.textBoxBoard.TabIndex = 0;
            // 
            // ChessR1Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.ClientSize = new System.Drawing.Size(1119, 1351);
            this.Controls.Add(this.textBoxBoard);
            this.Name = "ChessR1Form";
            this.Text = "ChessR1";
            this.Load += new System.EventHandler(this.ChessR1Form_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ChessR1Form_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ChessR1Form_MouseDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxBoard;
    }
}

