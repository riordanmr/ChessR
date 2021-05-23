
namespace ChessR1
{
    partial class FormSettings
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
            this.labelDebugMask = new System.Windows.Forms.Label();
            this.labelLookahead = new System.Windows.Forms.Label();
            this.labelLookahead2 = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.numericUpDownLookahead = new System.Windows.Forms.NumericUpDown();
            this.textBoxDebugMask = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLookahead)).BeginInit();
            this.SuspendLayout();
            // 
            // labelDebugMask
            // 
            this.labelDebugMask.AutoSize = true;
            this.labelDebugMask.Location = new System.Drawing.Point(68, 151);
            this.labelDebugMask.Name = "labelDebugMask";
            this.labelDebugMask.Size = new System.Drawing.Size(133, 25);
            this.labelDebugMask.TabIndex = 3;
            this.labelDebugMask.Text = "Debug Mask";
            // 
            // labelLookahead
            // 
            this.labelLookahead.AutoSize = true;
            this.labelLookahead.Location = new System.Drawing.Point(68, 52);
            this.labelLookahead.Name = "labelLookahead";
            this.labelLookahead.Size = new System.Drawing.Size(125, 25);
            this.labelLookahead.TabIndex = 0;
            this.labelLookahead.Text = "Lookahead ";
            // 
            // labelLookahead2
            // 
            this.labelLookahead2.AutoSize = true;
            this.labelLookahead2.Location = new System.Drawing.Point(490, 52);
            this.labelLookahead2.Name = "labelLookahead2";
            this.labelLookahead2.Size = new System.Drawing.Size(140, 25);
            this.labelLookahead2.TabIndex = 2;
            this.labelLookahead2.Text = "in half-moves";
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(109, 376);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(180, 78);
            this.buttonOK.TabIndex = 5;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(434, 376);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(177, 78);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // numericUpDownLookahead
            // 
            this.numericUpDownLookahead.Location = new System.Drawing.Point(319, 52);
            this.numericUpDownLookahead.Name = "numericUpDownLookahead";
            this.numericUpDownLookahead.Size = new System.Drawing.Size(120, 31);
            this.numericUpDownLookahead.TabIndex = 1;
            // 
            // textBoxDebugMask
            // 
            this.textBoxDebugMask.Location = new System.Drawing.Point(319, 145);
            this.textBoxDebugMask.Name = "textBoxDebugMask";
            this.textBoxDebugMask.Size = new System.Drawing.Size(100, 31);
            this.textBoxDebugMask.TabIndex = 4;
            // 
            // FormSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(794, 522);
            this.Controls.Add(this.textBoxDebugMask);
            this.Controls.Add(this.numericUpDownLookahead);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelLookahead2);
            this.Controls.Add(this.labelLookahead);
            this.Controls.Add(this.labelDebugMask);
            this.Name = "FormSettings";
            this.Text = "ChessR1 Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLookahead)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelDebugMask;
        private System.Windows.Forms.Label labelLookahead;
        private System.Windows.Forms.Label labelLookahead2;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.NumericUpDown numericUpDownLookahead;
        private System.Windows.Forms.TextBox textBoxDebugMask;
    }
}