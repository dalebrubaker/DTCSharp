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
            this.components = new System.ComponentModel.Container();
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
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabPageSymbols = new System.Windows.Forms.TabPage();
            this.btnSecurityDefinition = new System.Windows.Forms.Button();
            this.btnExchanges = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtSymbolDef = new System.Windows.Forms.TextBox();
            this.tabPageLevel1 = new System.Windows.Forms.TabPage();
            this.btnSubscribeCallbacks2 = new System.Windows.Forms.Button();
            this.cbShowBidAsk2 = new System.Windows.Forms.CheckBox();
            this.btnUnsubscribe2 = new System.Windows.Forms.Button();
            this.btnSubscribeEvents2 = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.txtSymbolLevel1_2 = new System.Windows.Forms.TextBox();
            this.btnSubscribeCallbacks1 = new System.Windows.Forms.Button();
            this.cbShowBidAsk1 = new System.Windows.Forms.CheckBox();
            this.btnUnsubscribe1 = new System.Windows.Forms.Button();
            this.btnSubscribeEvents1 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtSymbolLevel1_1 = new System.Windows.Forms.TextBox();
            this.tabPageHistorical = new System.Windows.Forms.TabPage();
            this.cbZip = new System.Windows.Forms.CheckBox();
            this.btnGetHistoricalMinutes = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.btnGetHistoricalTicks = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.txtSymbolHistorical = new System.Windows.Forms.TextBox();
            this.timerLevel1Update = new System.Windows.Forms.Timer(this.components);
            this.logControlConnect = new TestClient.LogControl();
            this.logControlSymbols = new TestClient.LogControl();
            this.logControlLevel1 = new TestClient.LogControl();
            this.logControlHistorical = new TestClient.LogControl();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageConnect.SuspendLayout();
            this.tabPageSymbols.SuspendLayout();
            this.tabPageLevel1.SuspendLayout();
            this.tabPageHistorical.SuspendLayout();
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
            this.txtPortHistorical.Text = "50001";
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
            this.statusStrip1.Location = new System.Drawing.Point(3, 586);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(806, 22);
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
            this.tabControl1.Controls.Add(this.tabPageHistorical);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(820, 637);
            this.tabControl1.TabIndex = 10;
            // 
            // tabPageConnect
            // 
            this.tabPageConnect.Controls.Add(this.txtPassword);
            this.tabPageConnect.Controls.Add(this.label10);
            this.tabPageConnect.Controls.Add(this.txtUsername);
            this.tabPageConnect.Controls.Add(this.label6);
            this.tabPageConnect.Controls.Add(this.statusStrip1);
            this.tabPageConnect.Controls.Add(this.label1);
            this.tabPageConnect.Controls.Add(this.txtServer);
            this.tabPageConnect.Controls.Add(this.label2);
            this.tabPageConnect.Controls.Add(this.txtPortListening);
            this.tabPageConnect.Controls.Add(this.txtPortHistorical);
            this.tabPageConnect.Controls.Add(this.label3);
            this.tabPageConnect.Controls.Add(this.btnConnect);
            this.tabPageConnect.Controls.Add(this.btnDisconnect);
            this.tabPageConnect.Controls.Add(this.logControlConnect);
            this.tabPageConnect.Location = new System.Drawing.Point(4, 22);
            this.tabPageConnect.Name = "tabPageConnect";
            this.tabPageConnect.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageConnect.Size = new System.Drawing.Size(812, 611);
            this.tabPageConnect.TabIndex = 0;
            this.tabPageConnect.Text = "Connect";
            this.tabPageConnect.UseVisualStyleBackColor = true;
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(269, 14);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(86, 20);
            this.txtUsername.TabIndex = 11;
            this.txtUsername.Text = "testUsername";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(205, 18);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(58, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Username:";
            // 
            // tabPageSymbols
            // 
            this.tabPageSymbols.Controls.Add(this.btnSecurityDefinition);
            this.tabPageSymbols.Controls.Add(this.btnExchanges);
            this.tabPageSymbols.Controls.Add(this.label4);
            this.tabPageSymbols.Controls.Add(this.txtSymbolDef);
            this.tabPageSymbols.Controls.Add(this.logControlSymbols);
            this.tabPageSymbols.Location = new System.Drawing.Point(4, 22);
            this.tabPageSymbols.Name = "tabPageSymbols";
            this.tabPageSymbols.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSymbols.Size = new System.Drawing.Size(812, 611);
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
            this.tabPageLevel1.Controls.Add(this.btnSubscribeCallbacks2);
            this.tabPageLevel1.Controls.Add(this.cbShowBidAsk2);
            this.tabPageLevel1.Controls.Add(this.btnUnsubscribe2);
            this.tabPageLevel1.Controls.Add(this.btnSubscribeEvents2);
            this.tabPageLevel1.Controls.Add(this.label9);
            this.tabPageLevel1.Controls.Add(this.txtSymbolLevel1_2);
            this.tabPageLevel1.Controls.Add(this.btnSubscribeCallbacks1);
            this.tabPageLevel1.Controls.Add(this.cbShowBidAsk1);
            this.tabPageLevel1.Controls.Add(this.btnUnsubscribe1);
            this.tabPageLevel1.Controls.Add(this.btnSubscribeEvents1);
            this.tabPageLevel1.Controls.Add(this.label5);
            this.tabPageLevel1.Controls.Add(this.txtSymbolLevel1_1);
            this.tabPageLevel1.Controls.Add(this.logControlLevel1);
            this.tabPageLevel1.Location = new System.Drawing.Point(4, 22);
            this.tabPageLevel1.Name = "tabPageLevel1";
            this.tabPageLevel1.Size = new System.Drawing.Size(812, 611);
            this.tabPageLevel1.TabIndex = 2;
            this.tabPageLevel1.Text = "Level 1";
            this.tabPageLevel1.UseVisualStyleBackColor = true;
            // 
            // btnSubscribeCallbacks2
            // 
            this.btnSubscribeCallbacks2.Location = new System.Drawing.Point(359, 33);
            this.btnSubscribeCallbacks2.Name = "btnSubscribeCallbacks2";
            this.btnSubscribeCallbacks2.Size = new System.Drawing.Size(120, 23);
            this.btnSubscribeCallbacks2.TabIndex = 20;
            this.btnSubscribeCallbacks2.Text = "Subscribe Callbacks 2";
            this.btnSubscribeCallbacks2.UseVisualStyleBackColor = true;
            this.btnSubscribeCallbacks2.Click += new System.EventHandler(this.btnSubscribeCallbacks2_Click);
            // 
            // cbShowBidAsk2
            // 
            this.cbShowBidAsk2.AutoSize = true;
            this.cbShowBidAsk2.Location = new System.Drawing.Point(134, 36);
            this.cbShowBidAsk2.Name = "cbShowBidAsk2";
            this.cbShowBidAsk2.Size = new System.Drawing.Size(103, 17);
            this.cbShowBidAsk2.TabIndex = 19;
            this.cbShowBidAsk2.Text = "Show Bid/Ask 2";
            this.cbShowBidAsk2.UseVisualStyleBackColor = true;
            // 
            // btnUnsubscribe2
            // 
            this.btnUnsubscribe2.Location = new System.Drawing.Point(485, 33);
            this.btnUnsubscribe2.Name = "btnUnsubscribe2";
            this.btnUnsubscribe2.Size = new System.Drawing.Size(91, 23);
            this.btnUnsubscribe2.TabIndex = 18;
            this.btnUnsubscribe2.Text = "Unsubscribe 2";
            this.btnUnsubscribe2.UseVisualStyleBackColor = true;
            this.btnUnsubscribe2.Click += new System.EventHandler(this.btnUnsubscribe2_Click);
            // 
            // btnSubscribeEvents2
            // 
            this.btnSubscribeEvents2.Location = new System.Drawing.Point(243, 33);
            this.btnSubscribeEvents2.Name = "btnSubscribeEvents2";
            this.btnSubscribeEvents2.Size = new System.Drawing.Size(110, 23);
            this.btnSubscribeEvents2.TabIndex = 17;
            this.btnSubscribeEvents2.Text = "Subscribe Events 2";
            this.btnSubscribeEvents2.UseVisualStyleBackColor = true;
            this.btnSubscribeEvents2.Click += new System.EventHandler(this.btnSubscribeEvents2_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(8, 38);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(50, 13);
            this.label9.TabIndex = 15;
            this.label9.Text = "Symbol2:";
            // 
            // txtSymbolLevel1_2
            // 
            this.txtSymbolLevel1_2.Location = new System.Drawing.Point(58, 34);
            this.txtSymbolLevel1_2.Name = "txtSymbolLevel1_2";
            this.txtSymbolLevel1_2.Size = new System.Drawing.Size(67, 20);
            this.txtSymbolLevel1_2.TabIndex = 16;
            this.txtSymbolLevel1_2.Text = "NQZ6";
            // 
            // btnSubscribeCallbacks1
            // 
            this.btnSubscribeCallbacks1.Location = new System.Drawing.Point(359, 7);
            this.btnSubscribeCallbacks1.Name = "btnSubscribeCallbacks1";
            this.btnSubscribeCallbacks1.Size = new System.Drawing.Size(120, 23);
            this.btnSubscribeCallbacks1.TabIndex = 13;
            this.btnSubscribeCallbacks1.Text = "Subscribe Callbacks 1";
            this.btnSubscribeCallbacks1.UseVisualStyleBackColor = true;
            this.btnSubscribeCallbacks1.Click += new System.EventHandler(this.btnSubscribeCallbacks1_Click);
            // 
            // cbShowBidAsk1
            // 
            this.cbShowBidAsk1.AutoSize = true;
            this.cbShowBidAsk1.Location = new System.Drawing.Point(133, 11);
            this.cbShowBidAsk1.Name = "cbShowBidAsk1";
            this.cbShowBidAsk1.Size = new System.Drawing.Size(103, 17);
            this.cbShowBidAsk1.TabIndex = 12;
            this.cbShowBidAsk1.Text = "Show Bid/Ask 1";
            this.cbShowBidAsk1.UseVisualStyleBackColor = true;
            // 
            // btnUnsubscribe1
            // 
            this.btnUnsubscribe1.Location = new System.Drawing.Point(485, 8);
            this.btnUnsubscribe1.Name = "btnUnsubscribe1";
            this.btnUnsubscribe1.Size = new System.Drawing.Size(91, 23);
            this.btnUnsubscribe1.TabIndex = 11;
            this.btnUnsubscribe1.Text = "Unsubscribe 1";
            this.btnUnsubscribe1.UseVisualStyleBackColor = true;
            this.btnUnsubscribe1.Click += new System.EventHandler(this.btnUnsubscribe1_Click);
            // 
            // btnSubscribeEvents1
            // 
            this.btnSubscribeEvents1.Location = new System.Drawing.Point(242, 7);
            this.btnSubscribeEvents1.Name = "btnSubscribeEvents1";
            this.btnSubscribeEvents1.Size = new System.Drawing.Size(111, 23);
            this.btnSubscribeEvents1.TabIndex = 9;
            this.btnSubscribeEvents1.Text = "Subscribe Events 1";
            this.btnSubscribeEvents1.UseVisualStyleBackColor = true;
            this.btnSubscribeEvents1.Click += new System.EventHandler(this.btnSubscribeEvents1_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(50, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Symbol1:";
            // 
            // txtSymbolLevel1_1
            // 
            this.txtSymbolLevel1_1.Location = new System.Drawing.Point(58, 9);
            this.txtSymbolLevel1_1.Name = "txtSymbolLevel1_1";
            this.txtSymbolLevel1_1.Size = new System.Drawing.Size(67, 20);
            this.txtSymbolLevel1_1.TabIndex = 8;
            this.txtSymbolLevel1_1.Text = "ESZ6";
            // 
            // tabPageHistorical
            // 
            this.tabPageHistorical.Controls.Add(this.cbZip);
            this.tabPageHistorical.Controls.Add(this.btnGetHistoricalMinutes);
            this.tabPageHistorical.Controls.Add(this.label8);
            this.tabPageHistorical.Controls.Add(this.dtpStart);
            this.tabPageHistorical.Controls.Add(this.btnGetHistoricalTicks);
            this.tabPageHistorical.Controls.Add(this.label7);
            this.tabPageHistorical.Controls.Add(this.txtSymbolHistorical);
            this.tabPageHistorical.Controls.Add(this.logControlHistorical);
            this.tabPageHistorical.Location = new System.Drawing.Point(4, 22);
            this.tabPageHistorical.Name = "tabPageHistorical";
            this.tabPageHistorical.Size = new System.Drawing.Size(812, 611);
            this.tabPageHistorical.TabIndex = 3;
            this.tabPageHistorical.Text = "Historical";
            this.tabPageHistorical.UseVisualStyleBackColor = true;
            // 
            // cbZip
            // 
            this.cbZip.AutoSize = true;
            this.cbZip.Location = new System.Drawing.Point(355, 18);
            this.cbZip.Name = "cbZip";
            this.cbZip.Size = new System.Drawing.Size(41, 17);
            this.cbZip.TabIndex = 15;
            this.cbZip.Text = "Zip";
            this.cbZip.UseVisualStyleBackColor = true;
            // 
            // btnGetHistoricalMinutes
            // 
            this.btnGetHistoricalMinutes.Location = new System.Drawing.Point(402, 15);
            this.btnGetHistoricalMinutes.Name = "btnGetHistoricalMinutes";
            this.btnGetHistoricalMinutes.Size = new System.Drawing.Size(122, 23);
            this.btnGetHistoricalMinutes.TabIndex = 14;
            this.btnGetHistoricalMinutes.Text = "Get Historical Minutes";
            this.btnGetHistoricalMinutes.UseVisualStyleBackColor = true;
            this.btnGetHistoricalMinutes.Click += new System.EventHandler(this.btnGetHistoricalMinutes_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(131, 20);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "Starting (local):";
            // 
            // dtpStart
            // 
            this.dtpStart.CustomFormat = "MMM d, yyyy HH:mm";
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpStart.Location = new System.Drawing.Point(214, 16);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(132, 20);
            this.dtpStart.TabIndex = 11;
            this.dtpStart.Value = new System.DateTime(2016, 10, 21, 9, 30, 0, 0);
            // 
            // btnGetHistoricalTicks
            // 
            this.btnGetHistoricalTicks.Location = new System.Drawing.Point(530, 15);
            this.btnGetHistoricalTicks.Name = "btnGetHistoricalTicks";
            this.btnGetHistoricalTicks.Size = new System.Drawing.Size(113, 23);
            this.btnGetHistoricalTicks.TabIndex = 9;
            this.btnGetHistoricalTicks.Text = "Get Historical Ticks";
            this.btnGetHistoricalTicks.UseVisualStyleBackColor = true;
            this.btnGetHistoricalTicks.Click += new System.EventHandler(this.btnGetHistoricalTicks_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 20);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(44, 13);
            this.label7.TabIndex = 7;
            this.label7.Text = "Symbol:";
            // 
            // txtSymbolHistorical
            // 
            this.txtSymbolHistorical.Location = new System.Drawing.Point(58, 16);
            this.txtSymbolHistorical.Name = "txtSymbolHistorical";
            this.txtSymbolHistorical.Size = new System.Drawing.Size(67, 20);
            this.txtSymbolHistorical.TabIndex = 8;
            this.txtSymbolHistorical.Text = "ESZ6";
            // 
            // timerLevel1Update
            // 
            this.timerLevel1Update.Enabled = true;
            this.timerLevel1Update.Interval = 200;
            this.timerLevel1Update.Tick += new System.EventHandler(this.timerLevel1Update_Tick);
            // 
            // logControlConnect
            // 
            this.logControlConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControlConnect.Location = new System.Drawing.Point(12, 128);
            this.logControlConnect.Name = "logControlConnect";
            this.logControlConnect.Size = new System.Drawing.Size(778, 455);
            this.logControlConnect.TabIndex = 9;
            this.logControlConnect.Title = "Log";
            // 
            // logControlSymbols
            // 
            this.logControlSymbols.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControlSymbols.Location = new System.Drawing.Point(3, 72);
            this.logControlSymbols.Name = "logControlSymbols";
            this.logControlSymbols.Size = new System.Drawing.Size(803, 531);
            this.logControlSymbols.TabIndex = 4;
            this.logControlSymbols.Title = "Log";
            // 
            // logControlLevel1
            // 
            this.logControlLevel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControlLevel1.Location = new System.Drawing.Point(3, 66);
            this.logControlLevel1.Name = "logControlLevel1";
            this.logControlLevel1.Size = new System.Drawing.Size(801, 537);
            this.logControlLevel1.TabIndex = 10;
            this.logControlLevel1.Title = "Log";
            // 
            // logControlHistorical
            // 
            this.logControlHistorical.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControlHistorical.Location = new System.Drawing.Point(3, 44);
            this.logControlHistorical.Name = "logControlHistorical";
            this.logControlHistorical.Size = new System.Drawing.Size(801, 559);
            this.logControlHistorical.TabIndex = 10;
            this.logControlHistorical.Title = "Log";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(271, 38);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(86, 20);
            this.txtPassword.TabIndex = 136;
            this.txtPassword.Text = "password";
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(207, 42);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(56, 13);
            this.label10.TabIndex = 135;
            this.label10.Text = "Password:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(820, 637);
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
            this.tabPageHistorical.ResumeLayout(false);
            this.tabPageHistorical.PerformLayout();
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
        private LogControl logControlConnect;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageConnect;
        private System.Windows.Forms.TabPage tabPageSymbols;
        private System.Windows.Forms.Button btnExchanges;
        private LogControl logControlSymbols;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtSymbolDef;
        private System.Windows.Forms.Button btnSecurityDefinition;
        private System.Windows.Forms.TabPage tabPageLevel1;
        private LogControl logControlLevel1;
        private System.Windows.Forms.Button btnSubscribeEvents1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtSymbolLevel1_1;
        private System.Windows.Forms.Button btnUnsubscribe1;
        private System.Windows.Forms.CheckBox cbShowBidAsk1;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TabPage tabPageHistorical;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private LogControl logControlHistorical;
        private System.Windows.Forms.Button btnGetHistoricalTicks;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtSymbolHistorical;
        private System.Windows.Forms.Button btnGetHistoricalMinutes;
        private System.Windows.Forms.CheckBox cbZip;
        private System.Windows.Forms.Button btnSubscribeCallbacks1;
        private System.Windows.Forms.Button btnSubscribeCallbacks2;
        private System.Windows.Forms.CheckBox cbShowBidAsk2;
        private System.Windows.Forms.Button btnUnsubscribe2;
        private System.Windows.Forms.Button btnSubscribeEvents2;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtSymbolLevel1_2;
        private System.Windows.Forms.Timer timerLevel1Update;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label10;
    }
}

