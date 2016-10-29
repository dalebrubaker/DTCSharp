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
using DTCServer;

namespace TestServer
{
    public partial class ServerForm : Form
    {
        private Server _serverPrimary;
        private Server _serverHistorical;
        private IPAddress _ipAddress;
        private readonly ExampleServerImpl _serverImpl;
        private CancellationTokenSource _ctsPrimary;
        private CancellationTokenSource _ctsHistorical;

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
            _serverImpl = new ExampleServerImpl();
            _serverImpl.MessageEvent += ServerImplMessageEvent;
        }

        private void ServerImplMessageEvent(object sender, string message)
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

        private void btnStartPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = false;
            btnStopPrimary.Enabled = true;
            const int heartbeatIntervalInSeconds = 10;
            const bool useHeartbeat = true;
            _serverPrimary = new Server(_serverImpl, _ipAddress, PortListener, heartbeatIntervalInSeconds, useHeartbeat);
            _ctsPrimary = new CancellationTokenSource();
            _serverPrimary.Run(_ctsPrimary.Token);
        }

        private void btnStopPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = true;
            btnStopPrimary.Enabled = false;
            _ctsPrimary.Cancel();
        }

        private void btnStartHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = false;
            btnStopHistorical.Enabled = true;
            const int heartbeatIntervalInSeconds = 0;
            const bool useHeartbeat = false; // See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
            _serverHistorical = new Server(_serverImpl, _ipAddress, PortListener, heartbeatIntervalInSeconds, useHeartbeat);
            _ctsHistorical = new CancellationTokenSource();
            _serverHistorical.Run(_ctsHistorical.Token);
        }

        private void btnStopHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = true;
            btnStopHistorical.Enabled = false;
            _ctsHistorical.Cancel();
        }
    }
}
