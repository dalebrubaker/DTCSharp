using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        private ServerStub _serverStubPrimary;
        private ServerStub _serverStubHistorical;

        public ServerForm()
        {
            InitializeComponent();
            var serverName = Environment.MachineName;
            lblServerName.Text = $"Server Name: {serverName}";
            var hostEntry = Dns.GetHostEntry(serverName);
            _ipAddress = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            lblServerIPAddress.Text = $"Server IP Address: {_ipAddress}";
            _serverStubPrimary = new ServerStub();
            _serverStubHistorical = new ServerStub();
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
            _serverPrimary = new Server(_serverStubPrimary, _ipAddress, PortListener, heartbeatIntervalInSeconds, useHeartbeat);
        }

        private void btnStopPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = true;
            btnStopPrimary.Enabled = false;
        }

        private void btnStartHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = false;
            btnStopHistorical.Enabled = true;
            const int heartbeatIntervalInSeconds = 0;
            const bool useHeartbeat = false; // See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
            _serverHistorical = new Server(_serverStubHistorical, _ipAddress, PortListener, heartbeatIntervalInSeconds, useHeartbeat);

        }

        private void btnStopHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = true;
            btnStopHistorical.Enabled = false;
        }
    }
}
