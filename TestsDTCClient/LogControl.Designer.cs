namespace TestsDTCClient
{
    partial class LogControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxLog = new System.Windows.Forms.GroupBox();
            this.rtbMessages = new System.Windows.Forms.RichTextBox();
            this.groupBoxLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxLog
            // 
            this.groupBoxLog.Controls.Add(this.rtbMessages);
            this.groupBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxLog.Location = new System.Drawing.Point(0, 0);
            this.groupBoxLog.Name = "groupBoxLog";
            this.groupBoxLog.Size = new System.Drawing.Size(363, 359);
            this.groupBoxLog.TabIndex = 0;
            this.groupBoxLog.TabStop = false;
            // 
            // rtbMessages
            // 
            this.rtbMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbMessages.Location = new System.Drawing.Point(3, 16);
            this.rtbMessages.Name = "rtbMessages";
            this.rtbMessages.Size = new System.Drawing.Size(357, 340);
            this.rtbMessages.TabIndex = 1;
            this.rtbMessages.Text = "";
            // 
            // LogControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxLog);
            this.Name = "LogControl";
            this.Size = new System.Drawing.Size(363, 359);
            this.groupBoxLog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxLog;
        private System.Windows.Forms.RichTextBox rtbMessages;
    }
}
