using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTCCommon;
using DTCServer;
using static DTCCommon.WindowConfig;

namespace TestServer
{
    public partial class ServerForm : Form
    {
        private Server _serverPrimary;
        private Server _serverHistorical;
        private readonly IPAddress _ipAddress;
        private readonly ExampleService _exampleServer;

        public ServerForm()
        {
            InitializeComponent();
            var serverName = Environment.MachineName;
            lblServerName.Text = $"Server Name: {serverName}";
            var hostEntry = Dns.GetHostEntry(serverName);
            var ipAddress = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            lblServerIPAddress.Text = $"Server IP Address: {ipAddress}";
            _ipAddress = txtServer.Text.Trim().ToLower() == "localhost" ? IPAddress.Loopback : ipAddress;
            lblUsingIpAddress.Text = $"Using IP Address: {_ipAddress}";
            _exampleServer = new ExampleService();
            _exampleServer.MessageEvent += ExampleServerMessageEvent;
        }

        private void ExampleServerMessageEvent(object sender, string message)
        {
            logControl1.LogMessage(message);
        }

        private int PortListener
        {
            get
            {
                int port;
                int.TryParse(txtPortListening.Text, out port);
                return port;
            }
        }

        private int PortHistorical
        {
            get
            {
                int port;
                int.TryParse(txtPortHistorical.Text, out port);
                return port;
            }
        }

        private async void btnStartPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = false;
            btnStopPrimary.Enabled = true;
            _serverPrimary = new Server(_exampleServer.HandleRequest, _ipAddress, PortListener, timeoutNoActivity: 30000, useHeartbeat: true);
            try
            {
                await _serverPrimary.RunAsync().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // ignore. Server was disconnected
            }
        }

        private void btnStopPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = true;
            btnStopPrimary.Enabled = false;
            _serverPrimary.Dispose();
        }

        private async void btnStartHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = false;
            btnStopHistorical.Enabled = true;
            
            // useHeartbeat = false See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
            _serverHistorical = new Server(_exampleServer.HandleRequest, _ipAddress, PortListener, timeoutNoActivity: 30000, useHeartbeat: true);
            try
            {
                await _serverHistorical.RunAsync().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // ignore. Server was disconnected
            }
        }

        private void btnStopHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = true;
            btnStopHistorical.Enabled = false;
            _serverHistorical.Dispose();
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            WindowPlacement.SetPlacement(this.Handle, Settings1.Default.MainWindowPlacement);
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings1.Default.MainWindowPlacement = WindowPlacement.GetPlacement(this.Handle);
            Settings1.Default.Save();
        }
    }
}
