namespace TestClient
{
    partial class ScidUserControl
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
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.checkBoxLogLastRecord = new System.Windows.Forms.CheckBox();
            this.textBoxScidPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.logControl1 = new TestClient.LogControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.checkBoxLogLastRecord);
            this.splitContainer1.Panel1.Controls.Add(this.textBoxScidPath);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.logControl1);
            this.splitContainer1.Size = new System.Drawing.Size(1003, 533);
            this.splitContainer1.SplitterDistance = 60;
            this.splitContainer1.TabIndex = 0;
            // 
            // checkBoxLogLastRecord
            // 
            this.checkBoxLogLastRecord.AutoSize = true;
            this.checkBoxLogLastRecord.Location = new System.Drawing.Point(3, 32);
            this.checkBoxLogLastRecord.Name = "checkBoxLogLastRecord";
            this.checkBoxLogLastRecord.Size = new System.Drawing.Size(110, 19);
            this.checkBoxLogLastRecord.TabIndex = 2;
            this.checkBoxLogLastRecord.Text = "Log Last Record";
            this.checkBoxLogLastRecord.UseVisualStyleBackColor = true;
            this.checkBoxLogLastRecord.CheckedChanged += new System.EventHandler(this.checkBoxLogLastRecord_CheckedChanged);
            // 
            // textBoxScidPath
            // 
            this.textBoxScidPath.Location = new System.Drawing.Point(43, 3);
            this.textBoxScidPath.Name = "textBoxScidPath";
            this.textBoxScidPath.Size = new System.Drawing.Size(289, 23);
            this.textBoxScidPath.TabIndex = 1;
            this.textBoxScidPath.Text = "D:\\SierraChart-TradeEval\\Data\\MNQM22.scid";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Path:";
            // 
            // logControl1
            // 
            this.logControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logControl1.HideTimestamps = false;
            this.logControl1.Location = new System.Drawing.Point(0, 0);
            this.logControl1.MaximumLogLengthChars = 104857600;
            this.logControl1.Name = "logControl1";
            this.logControl1.Size = new System.Drawing.Size(1003, 469);
            this.logControl1.TabIndex = 0;
            this.logControl1.Title = "Log";
            // 
            // timer1
            // 
            this.timer1.Enabled = false;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // ScidUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "ScidUserControl";
            this.Size = new System.Drawing.Size(1003, 533);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.Load += OnLoad;
        }


        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckBox checkBoxLogLastRecord;
        private System.Windows.Forms.TextBox textBoxScidPath;
        private System.Windows.Forms.Label label1;
        private LogControl logControl1;
        private System.Windows.Forms.Timer timer1;
    }
}
