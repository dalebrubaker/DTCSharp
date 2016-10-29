namespace TestServer
{
    partial class ServerForm
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
            this.btnStartPrimary = new System.Windows.Forms.Button();
            this.btnStopPrimary = new System.Windows.Forms.Button();
            this.btnStartHistorical = new System.Windows.Forms.Button();
            this.btnStopHistorical = new System.Windows.Forms.Button();
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
            // btnStartPrimary
            // 
            this.btnStartPrimary.Location = new System.Drawing.Point(220, 56);
            this.btnStartPrimary.Name = "btnStartPrimary";
            this.btnStartPrimary.Size = new System.Drawing.Size(84, 23);
            this.btnStartPrimary.TabIndex = 12;
            this.btnStartPrimary.Text = "Start Primary";
            this.btnStartPrimary.UseVisualStyleBackColor = true;
            this.btnStartPrimary.Click += new System.EventHandler(this.btnStartPrimary_Click);
            // 
            // btnStopPrimary
            // 
            this.btnStopPrimary.Enabled = false;
            this.btnStopPrimary.Location = new System.Drawing.Point(310, 56);
            this.btnStopPrimary.Name = "btnStopPrimary";
            this.btnStopPrimary.Size = new System.Drawing.Size(84, 23);
            this.btnStopPrimary.TabIndex = 13;
            this.btnStopPrimary.Text = "Stop Primary";
            this.btnStopPrimary.UseVisualStyleBackColor = true;
            this.btnStopPrimary.Click += new System.EventHandler(this.btnStopPrimary_Click);
            // 
            // btnStartHistorical
            // 
            this.btnStartHistorical.Location = new System.Drawing.Point(220, 85);
            this.btnStartHistorical.Name = "btnStartHistorical";
            this.btnStartHistorical.Size = new System.Drawing.Size(84, 23);
            this.btnStartHistorical.TabIndex = 15;
            this.btnStartHistorical.Text = "Start Historical";
            this.btnStartHistorical.UseVisualStyleBackColor = true;
            this.btnStartHistorical.Click += new System.EventHandler(this.btnStartHistorical_Click);
            // 
            // btnStopHistorical
            // 
            this.btnStopHistorical.Enabled = false;
            this.btnStopHistorical.Location = new System.Drawing.Point(310, 85);
            this.btnStopHistorical.Name = "btnStopHistorical";
            this.btnStopHistorical.Size = new System.Drawing.Size(84, 23);
            this.btnStopHistorical.TabIndex = 16;
            this.btnStopHistorical.Text = "Stop Historical";
            this.btnStopHistorical.UseVisualStyleBackColor = true;
            this.btnStopHistorical.Click += new System.EventHandler(this.btnStopHistorical_Click);
            // 
            // logControl1
            // 
            this.logControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControl1.Location = new System.Drawing.Point(15, 114);
            this.logControl1.Name = "logControl1";
            this.logControl1.Size = new System.Drawing.Size(480, 275);
            this.logControl1.TabIndex = 14;
            this.logControl1.Title = "Log";
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 401);
            this.Controls.Add(this.btnStartHistorical);
            this.Controls.Add(this.btnStopHistorical);
            this.Controls.Add(this.logControl1);
            this.Controls.Add(this.btnStartPrimary);
            this.Controls.Add(this.btnStopPrimary);
            this.Controls.Add(this.lblServerIPAddress);
            this.Controls.Add(this.lblServerName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtPortListening);
            this.Controls.Add(this.txtPortHistorical);
            this.Controls.Add(this.label3);
            this.Name = "ServerForm";
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
        private System.Windows.Forms.Button btnStartPrimary;
        private System.Windows.Forms.Button btnStopPrimary;
        private LogControl logControl1;
        private System.Windows.Forms.Button btnStartHistorical;
        private System.Windows.Forms.Button btnStopHistorical;
    }
}

