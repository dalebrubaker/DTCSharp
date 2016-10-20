namespace TestClient
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.txtPortListening = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPortHistorical = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageConnect = new System.Windows.Forms.TabPage();
            this.tabPageSymbols = new System.Windows.Forms.TabPage();
            this.btnSecurityDefinition = new System.Windows.Forms.Button();
            this.btnExchanges = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtSymbolDef = new System.Windows.Forms.TextBox();
            this.tabPageLevel1 = new System.Windows.Forms.TabPage();
            this.btnUnsubscribe = new System.Windows.Forms.Button();
            this.btnSubscribe = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtSymbolLevel1 = new System.Windows.Forms.TextBox();
            this.logControl1 = new TestClient.LogControl();
            this.logControl2 = new TestClient.LogControl();
            this.logControl3 = new TestClient.LogControl();
            this.cbShowBidAsk = new System.Windows.Forms.CheckBox();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageConnect.SuspendLayout();
            this.tabPageSymbols.SuspendLayout();
            this.tabPageLevel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server:";
            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(99, 11);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(100, 20);
            this.txtServer.TabIndex = 1;
            this.txtServer.Text = "localhost";
            // 
            // txtPortListening
            // 
            this.txtPortListening.Location = new System.Drawing.Point(99, 39);
            this.txtPortListening.Name = "txtPortListening";
            this.txtPortListening.Size = new System.Drawing.Size(100, 20);
            this.txtPortListening.TabIndex = 3;
            this.txtPortListening.Text = "50000";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Listening Port:";
            // 
            // txtPortHistorical
            // 
            this.txtPortHistorical.Location = new System.Drawing.Point(99, 65);
            this.txtPortHistorical.Name = "txtPortHistorical";
            this.txtPortHistorical.Size = new System.Drawing.Size(100, 20);
            this.txtPortHistorical.TabIndex = 5;
            this.txtPortHistorical.Text = "11098";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Historical Port:";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(15, 99);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 6;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(99, 99);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnDisconnect.TabIndex = 7;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(3, 429);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(420, 22);
            this.statusStrip1.TabIndex = 8;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageConnect);
            this.tabControl1.Controls.Add(this.tabPageSymbols);
            this.tabControl1.Controls.Add(this.tabPageLevel1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(777, 480);
            this.tabControl1.TabIndex = 10;
            // 
            // tabPageConnect
            // 
            this.tabPageConnect.Controls.Add(this.statusStrip1);
            this.tabPageConnect.Controls.Add(this.label1);
            this.tabPageConnect.Controls.Add(this.txtServer);
            this.tabPageConnect.Controls.Add(this.label2);
            this.tabPageConnect.Controls.Add(this.txtPortListening);
            this.tabPageConnect.Controls.Add(this.txtPortHistorical);
            this.tabPageConnect.Controls.Add(this.label3);
            this.tabPageConnect.Controls.Add(this.btnConnect);
            this.tabPageConnect.Controls.Add(this.btnDisconnect);
            this.tabPageConnect.Controls.Add(this.logControl1);
            this.tabPageConnect.Location = new System.Drawing.Point(4, 22);
            this.tabPageConnect.Name = "tabPageConnect";
            this.tabPageConnect.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageConnect.Size = new System.Drawing.Size(426, 454);
            this.tabPageConnect.TabIndex = 0;
            this.tabPageConnect.Text = "Connect";
            this.tabPageConnect.UseVisualStyleBackColor = true;
            // 
            // tabPageSymbols
            // 
            this.tabPageSymbols.Controls.Add(this.btnSecurityDefinition);
            this.tabPageSymbols.Controls.Add(this.btnExchanges);
            this.tabPageSymbols.Controls.Add(this.label4);
            this.tabPageSymbols.Controls.Add(this.txtSymbolDef);
            this.tabPageSymbols.Controls.Add(this.logControl2);
            this.tabPageSymbols.Location = new System.Drawing.Point(4, 22);
            this.tabPageSymbols.Name = "tabPageSymbols";
            this.tabPageSymbols.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSymbols.Size = new System.Drawing.Size(426, 454);
            this.tabPageSymbols.TabIndex = 1;
            this.tabPageSymbols.Text = "Symbols";
            this.tabPageSymbols.UseVisualStyleBackColor = true;
            // 
            // btnSecurityDefinition
            // 
            this.btnSecurityDefinition.Location = new System.Drawing.Point(135, 43);
            this.btnSecurityDefinition.Name = "btnSecurityDefinition";
            this.btnSecurityDefinition.Size = new System.Drawing.Size(91, 23);
            this.btnSecurityDefinition.TabIndex = 6;
            this.btnSecurityDefinition.Text = "Get Definition";
            this.btnSecurityDefinition.UseVisualStyleBackColor = true;
            this.btnSecurityDefinition.Click += new System.EventHandler(this.btnSymbolDefinition_Click);
            // 
            // btnExchanges
            // 
            this.btnExchanges.Location = new System.Drawing.Point(11, 16);
            this.btnExchanges.Name = "btnExchanges";
            this.btnExchanges.Size = new System.Drawing.Size(75, 23);
            this.btnExchanges.TabIndex = 5;
            this.btnExchanges.Text = "Exchanges";
            this.btnExchanges.UseVisualStyleBackColor = true;
            this.btnExchanges.Click += new System.EventHandler(this.btnExchanges_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(44, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Symbol:";
            // 
            // txtSymbolDef
            // 
            this.txtSymbolDef.Location = new System.Drawing.Point(62, 45);
            this.txtSymbolDef.Name = "txtSymbolDef";
            this.txtSymbolDef.Size = new System.Drawing.Size(67, 20);
            this.txtSymbolDef.TabIndex = 3;
            this.txtSymbolDef.Text = "ESZ16";
            // 
            // tabPageLevel1
            // 
            this.tabPageLevel1.Controls.Add(this.cbShowBidAsk);
            this.tabPageLevel1.Controls.Add(this.btnUnsubscribe);
            this.tabPageLevel1.Controls.Add(this.logControl3);
            this.tabPageLevel1.Controls.Add(this.btnSubscribe);
            this.tabPageLevel1.Controls.Add(this.label5);
            this.tabPageLevel1.Controls.Add(this.txtSymbolLevel1);
            this.tabPageLevel1.Location = new System.Drawing.Point(4, 22);
            this.tabPageLevel1.Name = "tabPageLevel1";
            this.tabPageLevel1.Size = new System.Drawing.Size(769, 454);
            this.tabPageLevel1.TabIndex = 2;
            this.tabPageLevel1.Text = "Level 1";
            this.tabPageLevel1.UseVisualStyleBackColor = true;
            // 
            // btnUnsubscribe
            // 
            this.btnUnsubscribe.Location = new System.Drawing.Point(316, 9);
            this.btnUnsubscribe.Name = "btnUnsubscribe";
            this.btnUnsubscribe.Size = new System.Drawing.Size(91, 23);
            this.btnUnsubscribe.TabIndex = 11;
            this.btnUnsubscribe.Text = "Unsubscribe";
            this.btnUnsubscribe.UseVisualStyleBackColor = true;
            this.btnUnsubscribe.Click += new System.EventHandler(this.btnUnsubscribe_Click);
            // 
            // btnSubscribe
            // 
            this.btnSubscribe.Location = new System.Drawing.Point(219, 9);
            this.btnSubscribe.Name = "btnSubscribe";
            this.btnSubscribe.Size = new System.Drawing.Size(91, 23);
            this.btnSubscribe.TabIndex = 9;
            this.btnSubscribe.Text = "Subscribe";
            this.btnSubscribe.UseVisualStyleBackColor = true;
            this.btnSubscribe.Click += new System.EventHandler(this.btnSubscribe_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(44, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Symbol:";
            // 
            // txtSymbolLevel1
            // 
            this.txtSymbolLevel1.Location = new System.Drawing.Point(58, 9);
            this.txtSymbolLevel1.Name = "txtSymbolLevel1";
            this.txtSymbolLevel1.Size = new System.Drawing.Size(67, 20);
            this.txtSymbolLevel1.TabIndex = 8;
            this.txtSymbolLevel1.Text = "ESZ6";
            // 
            // logControl1
            // 
            this.logControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControl1.Location = new System.Drawing.Point(12, 128);
            this.logControl1.Name = "logControl1";
            this.logControl1.Size = new System.Drawing.Size(395, 298);
            this.logControl1.TabIndex = 9;
            this.logControl1.Title = "Log";
            // 
            // logControl2
            // 
            this.logControl2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControl2.Location = new System.Drawing.Point(3, 72);
            this.logControl2.Name = "logControl2";
            this.logControl2.Size = new System.Drawing.Size(415, 379);
            this.logControl2.TabIndex = 4;
            this.logControl2.Title = "Log";
            // 
            // logControl3
            // 
            this.logControl3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControl3.Location = new System.Drawing.Point(3, 49);
            this.logControl3.Name = "logControl3";
            this.logControl3.Size = new System.Drawing.Size(758, 402);
            this.logControl3.TabIndex = 10;
            this.logControl3.Title = "Log";
            // 
            // cbShowBidAsk
            // 
            this.cbShowBidAsk.AutoSize = true;
            this.cbShowBidAsk.Location = new System.Drawing.Point(133, 11);
            this.cbShowBidAsk.Name = "cbShowBidAsk";
            this.cbShowBidAsk.Size = new System.Drawing.Size(94, 17);
            this.cbShowBidAsk.TabIndex = 12;
            this.cbShowBidAsk.Text = "Show Bid/Ask";
            this.cbShowBidAsk.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(777, 480);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.Text = "TestClient";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageConnect.ResumeLayout(false);
            this.tabPageConnect.PerformLayout();
            this.tabPageSymbols.ResumeLayout(false);
            this.tabPageSymbols.PerformLayout();
            this.tabPageLevel1.ResumeLayout(false);
            this.tabPageLevel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.TextBox txtPortListening;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPortHistorical;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private LogControl logControl1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageConnect;
        private System.Windows.Forms.TabPage tabPageSymbols;
        private System.Windows.Forms.Button btnExchanges;
        private LogControl logControl2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtSymbolDef;
        private System.Windows.Forms.Button btnSecurityDefinition;
        private System.Windows.Forms.TabPage tabPageLevel1;
        private LogControl logControl3;
        private System.Windows.Forms.Button btnSubscribe;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtSymbolLevel1;
        private System.Windows.Forms.Button btnUnsubscribe;
        private System.Windows.Forms.CheckBox cbShowBidAsk;
    }
}

