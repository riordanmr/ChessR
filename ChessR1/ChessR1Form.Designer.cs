
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
            this.labelMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelMessage
            // 
            this.labelMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMessage.ForeColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.labelMessage.Location = new System.Drawing.Point(226, 9);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(565, 44);
            this.labelMessage.TabIndex = 0;
            // 
            // ChessR1Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.ClientSize = new System.Drawing.Size(1100, 1116);
            this.Controls.Add(this.labelMessage);
            this.Name = "ChessR1Form";
            this.Text = "ChessR1";
            this.Load += new System.EventHandler(this.ChessR1Form_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ChessR1Form_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ChessR1Form_MouseDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelMessage;
    }
}

