namespace TestClient
{
    partial class ClientForm
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
            this.btnConnectListener = new System.Windows.Forms.Button();
            this.btnDisconnectListener = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageConnect = new System.Windows.Forms.TabPage();
            this.cbxShowHeartbeats = new System.Windows.Forms.CheckBox();
            this.gbxListenerClientConnector = new System.Windows.Forms.GroupBox();
            this.btnStopListenerClientConnector = new System.Windows.Forms.Button();
            this.btnStartListenerClientConnector = new System.Windows.Forms.Button();
            this.btnConnectHistorical = new System.Windows.Forms.Button();
            this.btnDisconnectHistorical = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.cbxEncoding = new System.Windows.Forms.ComboBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabPageSymbols = new System.Windows.Forms.TabPage();
            this.btnSymbolsForExchange = new System.Windows.Forms.Button();
            this.cbxInstrumentTypes = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txtExchangeSymbols = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
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
            this.btnGetHistoricalDays = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.txtExchangeHistorical = new System.Windows.Forms.TextBox();
            this.cbZip = new System.Windows.Forms.CheckBox();
            this.btnGetHistoricalMinutes = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.btnGetHistoricalTicks = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.txtSymbolHistorical = new System.Windows.Forms.TextBox();
            this.tabPageTrade = new System.Windows.Forms.TabPage();
            this.label27 = new System.Windows.Forms.Label();
            this.txtQtyOCO = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.cmbxOrderTypeOCO = new System.Windows.Forms.ComboBox();
            this.label26 = new System.Windows.Forms.Label();
            this.txtPrice2OCO = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.txtPrice1OCO = new System.Windows.Forms.TextBox();
            this.label24 = new System.Windows.Forms.Label();
            this.btnGetHistoricalFills = new System.Windows.Forms.Button();
            this.btnGetOpenOrders = new System.Windows.Forms.Button();
            this.label23 = new System.Windows.Forms.Label();
            this.txtAccount = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.txtPrice2 = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.txtPrice1 = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.txtQty = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.cmbxOrderType = new System.Windows.Forms.ComboBox();
            this.btnSell = new System.Windows.Forms.Button();
            this.btnBuy = new System.Windows.Forms.Button();
            this.label18 = new System.Windows.Forms.Label();
            this.txtExchangeTrade = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.txtSymbolTrade = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.timerLevel1Update = new System.Windows.Forms.Timer(this.components);
            this.logControlConnect = new TestClient.LogControl();
            this.logControlSymbols = new TestClient.LogControl();
            this.logControlLevel1 = new TestClient.LogControl();
            this.logControlHistorical = new TestClient.LogControl();
            this.logControlTrades = new TestClient.LogControl();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageConnect.SuspendLayout();
            this.gbxListenerClientConnector.SuspendLayout();
            this.tabPageSymbols.SuspendLayout();
            this.tabPageLevel1.SuspendLayout();
            this.tabPageHistorical.SuspendLayout();
            this.tabPageTrade.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server:";
            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(93, 10);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(180, 20);
            this.txtServer.TabIndex = 1;
            this.txtServer.Text = "localhost";
            // 
            // txtPortListening
            // 
            this.txtPortListening.Location = new System.Drawing.Point(93, 35);
            this.txtPortListening.Name = "txtPortListening";
            this.txtPortListening.Size = new System.Drawing.Size(44, 20);
            this.txtPortListening.TabIndex = 3;
            this.txtPortListening.Text = "11099";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Listening Port:";
            // 
            // txtPortHistorical
            // 
            this.txtPortHistorical.Location = new System.Drawing.Point(93, 65);
            this.txtPortHistorical.Name = "txtPortHistorical";
            this.txtPortHistorical.Size = new System.Drawing.Size(44, 20);
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
            // btnConnectListener
            // 
            this.btnConnectListener.Location = new System.Drawing.Point(12, 99);
            this.btnConnectListener.Name = "btnConnectListener";
            this.btnConnectListener.Size = new System.Drawing.Size(102, 23);
            this.btnConnectListener.TabIndex = 6;
            this.btnConnectListener.Text = "Connect Listener";
            this.btnConnectListener.UseVisualStyleBackColor = true;
            this.btnConnectListener.Click += new System.EventHandler(this.btnConnectListener_Click);
            // 
            // btnDisconnectListener
            // 
            this.btnDisconnectListener.Location = new System.Drawing.Point(120, 99);
            this.btnDisconnectListener.Name = "btnDisconnectListener";
            this.btnDisconnectListener.Size = new System.Drawing.Size(113, 23);
            this.btnDisconnectListener.TabIndex = 7;
            this.btnDisconnectListener.Text = "Disconnect Listener";
            this.btnDisconnectListener.UseVisualStyleBackColor = true;
            this.btnDisconnectListener.Click += new System.EventHandler(this.btnDisconnectListener_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(3, 586);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1191, 22);
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
            this.tabControl1.Controls.Add(this.tabPageTrade);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1205, 637);
            this.tabControl1.TabIndex = 10;
            // 
            // tabPageConnect
            // 
            this.tabPageConnect.Controls.Add(this.cbxShowHeartbeats);
            this.tabPageConnect.Controls.Add(this.gbxListenerClientConnector);
            this.tabPageConnect.Controls.Add(this.btnConnectHistorical);
            this.tabPageConnect.Controls.Add(this.btnDisconnectHistorical);
            this.tabPageConnect.Controls.Add(this.label11);
            this.tabPageConnect.Controls.Add(this.cbxEncoding);
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
            this.tabPageConnect.Controls.Add(this.btnConnectListener);
            this.tabPageConnect.Controls.Add(this.btnDisconnectListener);
            this.tabPageConnect.Controls.Add(this.logControlConnect);
            this.tabPageConnect.Location = new System.Drawing.Point(4, 22);
            this.tabPageConnect.Name = "tabPageConnect";
            this.tabPageConnect.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageConnect.Size = new System.Drawing.Size(1197, 611);
            this.tabPageConnect.TabIndex = 0;
            this.tabPageConnect.Text = "Connect";
            this.tabPageConnect.UseVisualStyleBackColor = true;
            // 
            // cbxShowHeartbeats
            // 
            this.cbxShowHeartbeats.AutoSize = true;
            this.cbxShowHeartbeats.Checked = true;
            this.cbxShowHeartbeats.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxShowHeartbeats.Location = new System.Drawing.Point(486, 102);
            this.cbxShowHeartbeats.Name = "cbxShowHeartbeats";
            this.cbxShowHeartbeats.Size = new System.Drawing.Size(106, 17);
            this.cbxShowHeartbeats.TabIndex = 143;
            this.cbxShowHeartbeats.Text = "Show heartbeats";
            this.cbxShowHeartbeats.UseVisualStyleBackColor = true;
            // 
            // gbxListenerClientConnector
            // 
            this.gbxListenerClientConnector.Controls.Add(this.btnStopListenerClientConnector);
            this.gbxListenerClientConnector.Controls.Add(this.btnStartListenerClientConnector);
            this.gbxListenerClientConnector.Location = new System.Drawing.Point(486, 14);
            this.gbxListenerClientConnector.Name = "gbxListenerClientConnector";
            this.gbxListenerClientConnector.Size = new System.Drawing.Size(158, 67);
            this.gbxListenerClientConnector.TabIndex = 142;
            this.gbxListenerClientConnector.TabStop = false;
            this.gbxListenerClientConnector.Text = "Listener ClientConnector";
            // 
            // btnStopListenerClientConnector
            // 
            this.btnStopListenerClientConnector.Location = new System.Drawing.Point(75, 25);
            this.btnStopListenerClientConnector.Name = "btnStopListenerClientConnector";
            this.btnStopListenerClientConnector.Size = new System.Drawing.Size(63, 23);
            this.btnStopListenerClientConnector.TabIndex = 142;
            this.btnStopListenerClientConnector.Text = "Stop";
            this.btnStopListenerClientConnector.UseVisualStyleBackColor = true;
            this.btnStopListenerClientConnector.Click += new System.EventHandler(this.btnStopListenerClientConnector_Click);
            // 
            // btnStartListenerClientConnector
            // 
            this.btnStartListenerClientConnector.Location = new System.Drawing.Point(6, 25);
            this.btnStartListenerClientConnector.Name = "btnStartListenerClientConnector";
            this.btnStartListenerClientConnector.Size = new System.Drawing.Size(63, 23);
            this.btnStartListenerClientConnector.TabIndex = 141;
            this.btnStartListenerClientConnector.Text = "Start";
            this.btnStartListenerClientConnector.UseVisualStyleBackColor = true;
            this.btnStartListenerClientConnector.Click += new System.EventHandler(this.btnStartListenerClientConnector_Click);
            // 
            // btnConnectHistorical
            // 
            this.btnConnectHistorical.Location = new System.Drawing.Point(239, 99);
            this.btnConnectHistorical.Name = "btnConnectHistorical";
            this.btnConnectHistorical.Size = new System.Drawing.Size(104, 23);
            this.btnConnectHistorical.TabIndex = 139;
            this.btnConnectHistorical.Text = "Connect Historical";
            this.btnConnectHistorical.UseVisualStyleBackColor = true;
            this.btnConnectHistorical.Click += new System.EventHandler(this.btnConnectHistorical_Click);
            // 
            // btnDisconnectHistorical
            // 
            this.btnDisconnectHistorical.Location = new System.Drawing.Point(349, 99);
            this.btnDisconnectHistorical.Name = "btnDisconnectHistorical";
            this.btnDisconnectHistorical.Size = new System.Drawing.Size(119, 23);
            this.btnDisconnectHistorical.TabIndex = 140;
            this.btnDisconnectHistorical.Text = "Disconnect Historical";
            this.btnDisconnectHistorical.UseVisualStyleBackColor = true;
            this.btnDisconnectHistorical.Click += new System.EventHandler(this.btnDisconnectHistorical_Click);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(279, 65);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(55, 13);
            this.label11.TabIndex = 138;
            this.label11.Text = "Encoding:";
            // 
            // cbxEncoding
            // 
            this.cbxEncoding.FormattingEnabled = true;
            this.cbxEncoding.Location = new System.Drawing.Point(345, 61);
            this.cbxEncoding.Name = "cbxEncoding";
            this.cbxEncoding.Size = new System.Drawing.Size(121, 21);
            this.cbxEncoding.TabIndex = 137;
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(345, 32);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(86, 20);
            this.txtPassword.TabIndex = 136;
            this.txtPassword.Text = "password";
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(279, 36);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(56, 13);
            this.label10.TabIndex = 135;
            this.label10.Text = "Password:";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(343, 7);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(86, 20);
            this.txtUsername.TabIndex = 11;
            this.txtUsername.Text = "testUsername";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(279, 11);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(58, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Username:";
            // 
            // tabPageSymbols
            // 
            this.tabPageSymbols.Controls.Add(this.btnSymbolsForExchange);
            this.tabPageSymbols.Controls.Add(this.cbxInstrumentTypes);
            this.tabPageSymbols.Controls.Add(this.label15);
            this.tabPageSymbols.Controls.Add(this.txtExchangeSymbols);
            this.tabPageSymbols.Controls.Add(this.label14);
            this.tabPageSymbols.Controls.Add(this.btnSecurityDefinition);
            this.tabPageSymbols.Controls.Add(this.btnExchanges);
            this.tabPageSymbols.Controls.Add(this.label4);
            this.tabPageSymbols.Controls.Add(this.txtSymbolDef);
            this.tabPageSymbols.Controls.Add(this.logControlSymbols);
            this.tabPageSymbols.Location = new System.Drawing.Point(4, 22);
            this.tabPageSymbols.Name = "tabPageSymbols";
            this.tabPageSymbols.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSymbols.Size = new System.Drawing.Size(1197, 611);
            this.tabPageSymbols.TabIndex = 1;
            this.tabPageSymbols.Text = "Symbols";
            this.tabPageSymbols.UseVisualStyleBackColor = true;
            // 
            // btnSymbolsForExchange
            // 
            this.btnSymbolsForExchange.Location = new System.Drawing.Point(685, 7);
            this.btnSymbolsForExchange.Name = "btnSymbolsForExchange";
            this.btnSymbolsForExchange.Size = new System.Drawing.Size(122, 23);
            this.btnSymbolsForExchange.TabIndex = 23;
            this.btnSymbolsForExchange.Text = "Symbols for Exchange";
            this.btnSymbolsForExchange.UseVisualStyleBackColor = true;
            this.btnSymbolsForExchange.Click += new System.EventHandler(this.btnSymbolsForExxchange_Click);
            // 
            // cbxInstrumentTypes
            // 
            this.cbxInstrumentTypes.FormattingEnabled = true;
            this.cbxInstrumentTypes.Location = new System.Drawing.Point(558, 7);
            this.cbxInstrumentTypes.Name = "cbxInstrumentTypes";
            this.cbxInstrumentTypes.Size = new System.Drawing.Size(121, 21);
            this.cbxInstrumentTypes.TabIndex = 22;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(281, 9);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(58, 13);
            this.label15.TabIndex = 20;
            this.label15.Text = "Exchange:";
            // 
            // txtExchangeSymbols
            // 
            this.txtExchangeSymbols.Location = new System.Drawing.Point(345, 6);
            this.txtExchangeSymbols.Name = "txtExchangeSymbols";
            this.txtExchangeSymbols.Size = new System.Drawing.Size(28, 20);
            this.txtExchangeSymbols.TabIndex = 21;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(12, 11);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(105, 13);
            this.label14.TabIndex = 19;
            this.label14.Text = "Uses Listener Server";
            // 
            // btnSecurityDefinition
            // 
            this.btnSecurityDefinition.Location = new System.Drawing.Point(379, 4);
            this.btnSecurityDefinition.Name = "btnSecurityDefinition";
            this.btnSecurityDefinition.Size = new System.Drawing.Size(91, 23);
            this.btnSecurityDefinition.TabIndex = 6;
            this.btnSecurityDefinition.Text = "Get Definition";
            this.btnSecurityDefinition.UseVisualStyleBackColor = true;
            this.btnSecurityDefinition.Click += new System.EventHandler(this.btnSymbolDefinition_Click);
            // 
            // btnExchanges
            // 
            this.btnExchanges.Location = new System.Drawing.Point(476, 4);
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
            this.label4.Location = new System.Drawing.Point(123, 10);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(44, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Symbol:";
            // 
            // txtSymbolDef
            // 
            this.txtSymbolDef.Location = new System.Drawing.Point(173, 6);
            this.txtSymbolDef.Name = "txtSymbolDef";
            this.txtSymbolDef.Size = new System.Drawing.Size(110, 20);
            this.txtSymbolDef.TabIndex = 3;
            this.txtSymbolDef.Text = "MNQM21-CME";
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
            this.tabPageLevel1.Size = new System.Drawing.Size(1197, 611);
            this.tabPageLevel1.TabIndex = 2;
            this.tabPageLevel1.Text = "Level 1";
            this.tabPageLevel1.UseVisualStyleBackColor = true;
            // 
            // btnSubscribeCallbacks2
            // 
            this.btnSubscribeCallbacks2.Location = new System.Drawing.Point(428, 34);
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
            this.cbShowBidAsk2.Location = new System.Drawing.Point(203, 37);
            this.cbShowBidAsk2.Name = "cbShowBidAsk2";
            this.cbShowBidAsk2.Size = new System.Drawing.Size(103, 17);
            this.cbShowBidAsk2.TabIndex = 19;
            this.cbShowBidAsk2.Text = "Show Bid/Ask 2";
            this.cbShowBidAsk2.UseVisualStyleBackColor = true;
            // 
            // btnUnsubscribe2
            // 
            this.btnUnsubscribe2.Location = new System.Drawing.Point(554, 34);
            this.btnUnsubscribe2.Name = "btnUnsubscribe2";
            this.btnUnsubscribe2.Size = new System.Drawing.Size(91, 23);
            this.btnUnsubscribe2.TabIndex = 18;
            this.btnUnsubscribe2.Text = "Unsubscribe 2";
            this.btnUnsubscribe2.UseVisualStyleBackColor = true;
            this.btnUnsubscribe2.Click += new System.EventHandler(this.btnUnsubscribe2_Click);
            // 
            // btnSubscribeEvents2
            // 
            this.btnSubscribeEvents2.Location = new System.Drawing.Point(312, 34);
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
            this.txtSymbolLevel1_2.Size = new System.Drawing.Size(138, 20);
            this.txtSymbolLevel1_2.TabIndex = 16;
            this.txtSymbolLevel1_2.Text = "NQZ6";
            // 
            // btnSubscribeCallbacks1
            // 
            this.btnSubscribeCallbacks1.Location = new System.Drawing.Point(428, 8);
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
            this.cbShowBidAsk1.Location = new System.Drawing.Point(202, 12);
            this.cbShowBidAsk1.Name = "cbShowBidAsk1";
            this.cbShowBidAsk1.Size = new System.Drawing.Size(103, 17);
            this.cbShowBidAsk1.TabIndex = 12;
            this.cbShowBidAsk1.Text = "Show Bid/Ask 1";
            this.cbShowBidAsk1.UseVisualStyleBackColor = true;
            // 
            // btnUnsubscribe1
            // 
            this.btnUnsubscribe1.Location = new System.Drawing.Point(554, 9);
            this.btnUnsubscribe1.Name = "btnUnsubscribe1";
            this.btnUnsubscribe1.Size = new System.Drawing.Size(91, 23);
            this.btnUnsubscribe1.TabIndex = 11;
            this.btnUnsubscribe1.Text = "Unsubscribe 1";
            this.btnUnsubscribe1.UseVisualStyleBackColor = true;
            this.btnUnsubscribe1.Click += new System.EventHandler(this.btnUnsubscribe1_Click);
            // 
            // btnSubscribeEvents1
            // 
            this.btnSubscribeEvents1.Location = new System.Drawing.Point(311, 8);
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
            this.txtSymbolLevel1_1.Size = new System.Drawing.Size(138, 20);
            this.txtSymbolLevel1_1.TabIndex = 8;
            this.txtSymbolLevel1_1.Text = "ESZ6";
            // 
            // tabPageHistorical
            // 
            this.tabPageHistorical.Controls.Add(this.btnGetHistoricalDays);
            this.tabPageHistorical.Controls.Add(this.label13);
            this.tabPageHistorical.Controls.Add(this.label12);
            this.tabPageHistorical.Controls.Add(this.txtExchangeHistorical);
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
            this.tabPageHistorical.Size = new System.Drawing.Size(1197, 611);
            this.tabPageHistorical.TabIndex = 3;
            this.tabPageHistorical.Text = "Historical";
            this.tabPageHistorical.UseVisualStyleBackColor = true;
            // 
            // btnGetHistoricalDays
            // 
            this.btnGetHistoricalDays.Location = new System.Drawing.Point(675, 5);
            this.btnGetHistoricalDays.Name = "btnGetHistoricalDays";
            this.btnGetHistoricalDays.Size = new System.Drawing.Size(75, 23);
            this.btnGetHistoricalDays.TabIndex = 19;
            this.btnGetHistoricalDays.Text = "Get Days";
            this.btnGetHistoricalDays.UseVisualStyleBackColor = true;
            this.btnGetHistoricalDays.Click += new System.EventHandler(this.btnGetHistoricalDays_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(8, 10);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(111, 13);
            this.label13.TabIndex = 18;
            this.label13.Text = "Uses Historical Server";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(279, 10);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(58, 13);
            this.label12.TabIndex = 16;
            this.label12.Text = "Exchange:";
            // 
            // txtExchangeHistorical
            // 
            this.txtExchangeHistorical.Location = new System.Drawing.Point(343, 6);
            this.txtExchangeHistorical.Name = "txtExchangeHistorical";
            this.txtExchangeHistorical.Size = new System.Drawing.Size(41, 20);
            this.txtExchangeHistorical.TabIndex = 17;
            // 
            // cbZip
            // 
            this.cbZip.AutoSize = true;
            this.cbZip.Location = new System.Drawing.Point(628, 8);
            this.cbZip.Name = "cbZip";
            this.cbZip.Size = new System.Drawing.Size(41, 17);
            this.cbZip.TabIndex = 15;
            this.cbZip.Text = "Zip";
            this.cbZip.UseVisualStyleBackColor = true;
            // 
            // btnGetHistoricalMinutes
            // 
            this.btnGetHistoricalMinutes.Location = new System.Drawing.Point(756, 5);
            this.btnGetHistoricalMinutes.Name = "btnGetHistoricalMinutes";
            this.btnGetHistoricalMinutes.Size = new System.Drawing.Size(75, 23);
            this.btnGetHistoricalMinutes.TabIndex = 14;
            this.btnGetHistoricalMinutes.Text = "Get Minutes";
            this.btnGetHistoricalMinutes.UseVisualStyleBackColor = true;
            this.btnGetHistoricalMinutes.Click += new System.EventHandler(this.btnGetHistoricalMinutes_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(404, 10);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "Starting (local):";
            // 
            // dtpStart
            // 
            this.dtpStart.CustomFormat = "MMM d, yyyy HH:mm";
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpStart.Location = new System.Drawing.Point(487, 6);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(132, 20);
            this.dtpStart.TabIndex = 11;
            this.dtpStart.Value = new System.DateTime(2016, 10, 21, 9, 30, 0, 0);
            // 
            // btnGetHistoricalTicks
            // 
            this.btnGetHistoricalTicks.Location = new System.Drawing.Point(837, 5);
            this.btnGetHistoricalTicks.Name = "btnGetHistoricalTicks";
            this.btnGetHistoricalTicks.Size = new System.Drawing.Size(75, 23);
            this.btnGetHistoricalTicks.TabIndex = 9;
            this.btnGetHistoricalTicks.Text = "Get Ticks";
            this.btnGetHistoricalTicks.UseVisualStyleBackColor = true;
            this.btnGetHistoricalTicks.Click += new System.EventHandler(this.btnGetHistoricalTicks_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(125, 10);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(44, 13);
            this.label7.TabIndex = 7;
            this.label7.Text = "Symbol:";
            // 
            // txtSymbolHistorical
            // 
            this.txtSymbolHistorical.Location = new System.Drawing.Point(175, 6);
            this.txtSymbolHistorical.Name = "txtSymbolHistorical";
            this.txtSymbolHistorical.Size = new System.Drawing.Size(98, 20);
            this.txtSymbolHistorical.TabIndex = 8;
            this.txtSymbolHistorical.Text = "MNQM21-CME";
            // 
            // tabPageTrade
            // 
            this.tabPageTrade.Controls.Add(this.label27);
            this.tabPageTrade.Controls.Add(this.txtQtyOCO);
            this.tabPageTrade.Controls.Add(this.label28);
            this.tabPageTrade.Controls.Add(this.cmbxOrderTypeOCO);
            this.tabPageTrade.Controls.Add(this.label26);
            this.tabPageTrade.Controls.Add(this.txtPrice2OCO);
            this.tabPageTrade.Controls.Add(this.label25);
            this.tabPageTrade.Controls.Add(this.txtPrice1OCO);
            this.tabPageTrade.Controls.Add(this.label24);
            this.tabPageTrade.Controls.Add(this.btnGetHistoricalFills);
            this.tabPageTrade.Controls.Add(this.btnGetOpenOrders);
            this.tabPageTrade.Controls.Add(this.label23);
            this.tabPageTrade.Controls.Add(this.txtAccount);
            this.tabPageTrade.Controls.Add(this.label22);
            this.tabPageTrade.Controls.Add(this.txtPrice2);
            this.tabPageTrade.Controls.Add(this.label21);
            this.tabPageTrade.Controls.Add(this.txtPrice1);
            this.tabPageTrade.Controls.Add(this.label20);
            this.tabPageTrade.Controls.Add(this.txtQty);
            this.tabPageTrade.Controls.Add(this.label19);
            this.tabPageTrade.Controls.Add(this.cmbxOrderType);
            this.tabPageTrade.Controls.Add(this.btnSell);
            this.tabPageTrade.Controls.Add(this.btnBuy);
            this.tabPageTrade.Controls.Add(this.label18);
            this.tabPageTrade.Controls.Add(this.txtExchangeTrade);
            this.tabPageTrade.Controls.Add(this.label17);
            this.tabPageTrade.Controls.Add(this.txtSymbolTrade);
            this.tabPageTrade.Controls.Add(this.label16);
            this.tabPageTrade.Controls.Add(this.logControlTrades);
            this.tabPageTrade.Location = new System.Drawing.Point(4, 22);
            this.tabPageTrade.Name = "tabPageTrade";
            this.tabPageTrade.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTrade.Size = new System.Drawing.Size(1197, 611);
            this.tabPageTrade.TabIndex = 4;
            this.tabPageTrade.Text = "Trade";
            this.tabPageTrade.UseVisualStyleBackColor = true;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(722, 44);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(26, 13);
            this.label27.TabIndex = 159;
            this.label27.Text = "Qty:";
            // 
            // txtQtyOCO
            // 
            this.txtQtyOCO.Location = new System.Drawing.Point(754, 40);
            this.txtQtyOCO.Name = "txtQtyOCO";
            this.txtQtyOCO.Size = new System.Drawing.Size(41, 20);
            this.txtQtyOCO.TabIndex = 160;
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(529, 44);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(60, 13);
            this.label28.TabIndex = 158;
            this.label28.Text = "OrderType:";
            // 
            // cmbxOrderTypeOCO
            // 
            this.cmbxOrderTypeOCO.FormattingEnabled = true;
            this.cmbxOrderTypeOCO.Location = new System.Drawing.Point(595, 40);
            this.cmbxOrderTypeOCO.Name = "cmbxOrderTypeOCO";
            this.cmbxOrderTypeOCO.Size = new System.Drawing.Size(121, 21);
            this.cmbxOrderTypeOCO.TabIndex = 157;
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(914, 44);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(40, 13);
            this.label26.TabIndex = 155;
            this.label26.Text = "Price2:";
            // 
            // txtPrice2OCO
            // 
            this.txtPrice2OCO.Location = new System.Drawing.Point(960, 40);
            this.txtPrice2OCO.Name = "txtPrice2OCO";
            this.txtPrice2OCO.Size = new System.Drawing.Size(61, 20);
            this.txtPrice2OCO.TabIndex = 156;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(801, 44);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(40, 13);
            this.label25.TabIndex = 153;
            this.label25.Text = "Price1:";
            // 
            // txtPrice1OCO
            // 
            this.txtPrice1OCO.Location = new System.Drawing.Point(847, 40);
            this.txtPrice1OCO.Name = "txtPrice1OCO";
            this.txtPrice1OCO.Size = new System.Drawing.Size(61, 20);
            this.txtPrice1OCO.TabIndex = 154;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(396, 44);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(120, 13);
            this.label24.TabIndex = 152;
            this.label24.Text = "OCO if QtyOCO is not 0:";
            // 
            // btnGetHistoricalFills
            // 
            this.btnGetHistoricalFills.Location = new System.Drawing.Point(120, 39);
            this.btnGetHistoricalFills.Name = "btnGetHistoricalFills";
            this.btnGetHistoricalFills.Size = new System.Drawing.Size(106, 23);
            this.btnGetHistoricalFills.TabIndex = 151;
            this.btnGetHistoricalFills.Text = "Get Historical Fills";
            this.btnGetHistoricalFills.UseVisualStyleBackColor = false;
            this.btnGetHistoricalFills.Click += new System.EventHandler(this.btnGetHistoricalFills_Click);
            // 
            // btnGetOpenOrders
            // 
            this.btnGetOpenOrders.Location = new System.Drawing.Point(8, 39);
            this.btnGetOpenOrders.Name = "btnGetOpenOrders";
            this.btnGetOpenOrders.Size = new System.Drawing.Size(106, 23);
            this.btnGetOpenOrders.TabIndex = 149;
            this.btnGetOpenOrders.Text = "Get Open Orders";
            this.btnGetOpenOrders.UseVisualStyleBackColor = true;
            this.btnGetOpenOrders.Click += new System.EventHandler(this.btnGetOpenOrders_ClickAsync);
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(120, 11);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(50, 13);
            this.label23.TabIndex = 147;
            this.label23.Text = "Account:";
            // 
            // txtAccount
            // 
            this.txtAccount.Location = new System.Drawing.Point(175, 7);
            this.txtAccount.Name = "txtAccount";
            this.txtAccount.Size = new System.Drawing.Size(50, 20);
            this.txtAccount.TabIndex = 148;
            this.txtAccount.Text = "Sim1";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(914, 11);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(40, 13);
            this.label22.TabIndex = 145;
            this.label22.Text = "Price2:";
            // 
            // txtPrice2
            // 
            this.txtPrice2.Location = new System.Drawing.Point(960, 7);
            this.txtPrice2.Name = "txtPrice2";
            this.txtPrice2.Size = new System.Drawing.Size(61, 20);
            this.txtPrice2.TabIndex = 146;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(801, 11);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(40, 13);
            this.label21.TabIndex = 143;
            this.label21.Text = "Price1:";
            // 
            // txtPrice1
            // 
            this.txtPrice1.Location = new System.Drawing.Point(847, 7);
            this.txtPrice1.Name = "txtPrice1";
            this.txtPrice1.Size = new System.Drawing.Size(61, 20);
            this.txtPrice1.TabIndex = 144;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(722, 11);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(26, 13);
            this.label20.TabIndex = 141;
            this.label20.Text = "Qty:";
            // 
            // txtQty
            // 
            this.txtQty.Location = new System.Drawing.Point(754, 7);
            this.txtQty.Name = "txtQty";
            this.txtQty.Size = new System.Drawing.Size(41, 20);
            this.txtQty.TabIndex = 142;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(529, 11);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(60, 13);
            this.label19.TabIndex = 140;
            this.label19.Text = "OrderType:";
            // 
            // cmbxOrderType
            // 
            this.cmbxOrderType.FormattingEnabled = true;
            this.cmbxOrderType.Location = new System.Drawing.Point(595, 7);
            this.cmbxOrderType.Name = "cmbxOrderType";
            this.cmbxOrderType.Size = new System.Drawing.Size(121, 21);
            this.cmbxOrderType.TabIndex = 139;
            // 
            // btnSell
            // 
            this.btnSell.Location = new System.Drawing.Point(315, 39);
            this.btnSell.Name = "btnSell";
            this.btnSell.Size = new System.Drawing.Size(75, 23);
            this.btnSell.TabIndex = 25;
            this.btnSell.Text = "Sell";
            this.btnSell.UseVisualStyleBackColor = true;
            this.btnSell.Click += new System.EventHandler(this.btnSell_Click);
            // 
            // btnBuy
            // 
            this.btnBuy.Location = new System.Drawing.Point(234, 39);
            this.btnBuy.Name = "btnBuy";
            this.btnBuy.Size = new System.Drawing.Size(75, 23);
            this.btnBuy.TabIndex = 24;
            this.btnBuy.Text = "Buy";
            this.btnBuy.UseVisualStyleBackColor = true;
            this.btnBuy.Click += new System.EventHandler(this.btnBuy_Click);
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(385, 11);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(58, 13);
            this.label18.TabIndex = 22;
            this.label18.Text = "Exchange:";
            // 
            // txtExchangeTrade
            // 
            this.txtExchangeTrade.Location = new System.Drawing.Point(449, 7);
            this.txtExchangeTrade.Name = "txtExchangeTrade";
            this.txtExchangeTrade.Size = new System.Drawing.Size(41, 20);
            this.txtExchangeTrade.TabIndex = 23;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(231, 11);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(44, 13);
            this.label17.TabIndex = 20;
            this.label17.Text = "Symbol:";
            // 
            // txtSymbolTrade
            // 
            this.txtSymbolTrade.Location = new System.Drawing.Point(281, 7);
            this.txtSymbolTrade.Name = "txtSymbolTrade";
            this.txtSymbolTrade.Size = new System.Drawing.Size(98, 20);
            this.txtSymbolTrade.TabIndex = 21;
            this.txtSymbolTrade.Text = "EURUSD";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(3, 11);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(111, 13);
            this.label16.TabIndex = 19;
            this.label16.Text = "Uses Historical Server";
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
            this.logControlConnect.HideTimestamps = false;
            this.logControlConnect.Location = new System.Drawing.Point(12, 128);
            this.logControlConnect.MaximumLogLengthChars = 104857600;
            this.logControlConnect.Name = "logControlConnect";
            this.logControlConnect.Size = new System.Drawing.Size(1163, 455);
            this.logControlConnect.TabIndex = 9;
            this.logControlConnect.Title = "Log - Shows every event from server";
            // 
            // logControlSymbols
            // 
            this.logControlSymbols.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControlSymbols.HideTimestamps = false;
            this.logControlSymbols.Location = new System.Drawing.Point(3, 35);
            this.logControlSymbols.MaximumLogLengthChars = 104857600;
            this.logControlSymbols.Name = "logControlSymbols";
            this.logControlSymbols.Size = new System.Drawing.Size(1188, 568);
            this.logControlSymbols.TabIndex = 4;
            this.logControlSymbols.Title = "Log";
            // 
            // logControlLevel1
            // 
            this.logControlLevel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logControlLevel1.HideTimestamps = false;
            this.logControlLevel1.Location = new System.Drawing.Point(3, 66);
            this.logControlLevel1.MaximumLogLengthChars = 104857600;
            this.logControlLevel1.Name = "logControlLevel1";
            this.logControlLevel1.Size = new System.Drawing.Size(801, 537);
            this.logControlLevel1.TabIndex = 10;
            this.logControlLevel1.Title = "Log";
            // 
            // logControlHistorical
            // 
            this.logControlHistorical.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.logControlHistorical.HideTimestamps = false;
            this.logControlHistorical.Location = new System.Drawing.Point(0, 34);
            this.logControlHistorical.MaximumLogLengthChars = 104857600;
            this.logControlHistorical.Name = "logControlHistorical";
            this.logControlHistorical.Size = new System.Drawing.Size(1197, 577);
            this.logControlHistorical.TabIndex = 10;
            this.logControlHistorical.Title = "Log";
            // 
            // logControlTrades
            // 
            this.logControlTrades.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.logControlTrades.HideTimestamps = false;
            this.logControlTrades.Location = new System.Drawing.Point(3, 103);
            this.logControlTrades.MaximumLogLengthChars = 104857600;
            this.logControlTrades.Name = "logControlTrades";
            this.logControlTrades.Size = new System.Drawing.Size(1191, 505);
            this.logControlTrades.TabIndex = 150;
            this.logControlTrades.Title = "Log";
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1205, 637);
            this.Controls.Add(this.tabControl1);
            this.Name = "ClientForm";
            this.Text = "TestClient";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClientForm_FormClosing);
            this.Load += new System.EventHandler(this.ClientForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageConnect.ResumeLayout(false);
            this.tabPageConnect.PerformLayout();
            this.gbxListenerClientConnector.ResumeLayout(false);
            this.tabPageSymbols.ResumeLayout(false);
            this.tabPageSymbols.PerformLayout();
            this.tabPageLevel1.ResumeLayout(false);
            this.tabPageLevel1.PerformLayout();
            this.tabPageHistorical.ResumeLayout(false);
            this.tabPageHistorical.PerformLayout();
            this.tabPageTrade.ResumeLayout(false);
            this.tabPageTrade.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Button btnGetHistoricalFills;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtExchangeHistorical;

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.TextBox txtPortListening;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPortHistorical;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnConnectListener;
        private System.Windows.Forms.Button btnDisconnectListener;
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
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cbxEncoding;
        private System.Windows.Forms.Button btnConnectHistorical;
        private System.Windows.Forms.Button btnDisconnectHistorical;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Button btnGetHistoricalDays;
        private System.Windows.Forms.Button btnSymbolsForExchange;
        private System.Windows.Forms.ComboBox cbxInstrumentTypes;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtExchangeSymbols;
        private System.Windows.Forms.GroupBox gbxListenerClientConnector;
        private System.Windows.Forms.Button btnStopListenerClientConnector;
        private System.Windows.Forms.Button btnStartListenerClientConnector;
        private System.Windows.Forms.Label label13;
        private LogControl logControlHistorical;
        private System.Windows.Forms.TabPage tabPageTrade;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox txtSymbolTrade;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Button btnSell;
        private System.Windows.Forms.Button btnBuy;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox txtExchangeTrade;
        private System.Windows.Forms.Button btnGetOpenOrders;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TextBox txtAccount;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.TextBox txtPrice2;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TextBox txtPrice1;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox txtQty;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.ComboBox cmbxOrderType;
        private LogControl logControlTrades;
        private System.Windows.Forms.CheckBox cbxShowHeartbeats;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.TextBox txtQtyOCO;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.ComboBox cmbxOrderTypeOCO;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.TextBox txtPrice2OCO;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.TextBox txtPrice1OCO;
        private System.Windows.Forms.Label label24;
    }
}

