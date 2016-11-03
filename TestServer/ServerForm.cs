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
        private readonly ExampleService _exampleServer;
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
            _serverPrimary = new Server(_exampleServer.HandleRequest, _ipAddress, PortListener, timeoutNoActivity:30000, useHeartbeat:true);
            _ctsPrimary = new CancellationTokenSource();
            await _serverPrimary.RunAsync(_ctsPrimary.Token).ConfigureAwait(false);
        }

        private void btnStopPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = true;
            btnStopPrimary.Enabled = false;
            _ctsPrimary.Cancel();
        }

        private async void btnStartHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = false;
            btnStopHistorical.Enabled = true;
            
            // useHeartbeat = false See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
            _serverHistorical = new Server(_exampleServer.HandleRequest, _ipAddress, PortListener, timeoutNoActivity: 30000, useHeartbeat: true);
            _ctsHistorical = new CancellationTokenSource();
            await _serverHistorical.RunAsync(_ctsHistorical.Token).ConfigureAwait(false);
        }

        private void btnStopHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = true;
            btnStopHistorical.Enabled = false;
            _ctsHistorical.Cancel();
        }
    }
}
