namespace TestServer
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label2 = new System.Windows.Forms.Label();
            this.txtPortListening = new System.Windows.Forms.TextBox();
            this.txtPortHistorical = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lblServerName = new System.Windows.Forms.Label();
            this.lblServerIPAddress = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.logControl1 = new TestServer.LogControl();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Listening Port:";
            // 
            // txtPortListening
            // 
            this.txtPortListening.Location = new System.Drawing.Point(99, 57);
            this.txtPortListening.Name = "txtPortListening";
            this.txtPortListening.Size = new System.Drawing.Size(100, 20);
            this.txtPortListening.TabIndex = 7;
            this.txtPortListening.Text = "49999";
            // 
            // txtPortHistorical
            // 
            this.txtPortHistorical.Location = new System.Drawing.Point(99, 87);
            this.txtPortHistorical.Name = "txtPortHistorical";
            this.txtPortHistorical.Size = new System.Drawing.Size(100, 20);
            this.txtPortHistorical.TabIndex = 9;
            this.txtPortHistorical.Text = "49998";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 90);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Historical Port:";
            // 
            // lblServerName
            // 
            this.lblServerName.AutoSize = true;
            this.lblServerName.Location = new System.Drawing.Point(12, 9);
            this.lblServerName.Name = "lblServerName";
            this.lblServerName.Size = new System.Drawing.Size(72, 13);
            this.lblServerName.TabIndex = 10;
            this.lblServerName.Text = "Server Name:";
            // 
            // lblServerIPAddress
            // 
            this.lblServerIPAddress.AutoSize = true;
            this.lblServerIPAddress.Location = new System.Drawing.Point(12, 35);
            this.lblServerIPAddress.Name = "lblServerIPAddress";
            this.lblServerIPAddress.Size = new System.Drawing.Size(95, 13);
            this.lblServerIPAddress.TabIndex = 11;
            this.lblServerIPAddress.Text = "Server IP Address:";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(15, 124);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 12;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(99, 124);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 13;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // logControl1
            // 
            this.logControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControl1.Location = new System.Drawing.Point(15, 153);
            this.logControl1.Name = "logControl1";
            this.logControl1.Size = new System.Drawing.Size(480, 236);
            this.logControl1.TabIndex = 14;
            this.logControl1.Title = "Log";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 401);
            this.Controls.Add(this.logControl1);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.lblServerIPAddress);
            this.Controls.Add(this.lblServerName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtPortListening);
            this.Controls.Add(this.txtPortHistorical);
            this.Controls.Add(this.label3);
            this.Name = "Form1";
            this.Text = "Testserver";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPortListening;
        private System.Windows.Forms.TextBox txtPortHistorical;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblServerName;
        private System.Windows.Forms.Label lblServerIPAddress;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private LogControl logControl1;
    }
}

