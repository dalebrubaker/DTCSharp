using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using DTCCommon;
using DTCServer;
using static DTCCommon.WindowConfig;

namespace TestServer
{
    public partial class ServerForm : Form
    {
        private ExampleService _serverPrimary;
        private ExampleService _serverHistorical;
        private IPAddress _ipAddress;

        public ServerForm()
        {
            InitializeComponent();
            SetIpAddress();
        }

        private void ExampleServiceMessageEvent(object sender, string message)
        {
            logControl1.LogMessage(message);
        }

        private int PortListener
        {
            get
            {
                int.TryParse(txtPortListening.Text, out var port);
                return port;
            }
        }

        private int PortHistorical
        {
            get
            {
                int.TryParse(txtPortHistorical.Text, out var port);
                return port;
            }
        }

        private void btnStartPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = false;
            btnStopPrimary.Enabled = true;
            _serverPrimary = new ExampleService(_ipAddress, PortListener, 100, 200);
            _serverPrimary.MessageEvent += ExampleServiceMessageEvent;
            logControl1.LogMessage($"Started {_serverPrimary}");
        }

        private void btnStopPrimary_Click(object sender, EventArgs e)
        {
            btnStartPrimary.Enabled = true;
            btnStopPrimary.Enabled = false;
            _serverPrimary.Dispose();
            logControl1.LogMessage($"Stopped {_serverPrimary}");
        }

        private void btnStartHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = false;
            btnStopHistorical.Enabled = true;
            _serverHistorical = new ExampleService(_ipAddress, PortHistorical, 1000, 2000);
            _serverHistorical.MessageEvent += ExampleServiceMessageEvent;
            logControl1.LogMessage($"Started {_serverHistorical}");
        }

        private void btnStopHistorical_Click(object sender, EventArgs e)
        {
            btnStartHistorical.Enabled = true;
            btnStopHistorical.Enabled = false;
            _serverHistorical.Dispose();
            logControl1.LogMessage($"Stopped {_serverHistorical}");
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            LoadConfig();
            SetIpAddress();
        }

        private void SetIpAddress()
        {
            var serverName = Environment.MachineName;
            lblServerName.Text = $"Server Name: {serverName}";
            var hostEntry = Dns.GetHostEntry(serverName);
            var ipAddress = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            lblServerIPAddress.Text = $"Server IP Address: {ipAddress}";
            _ipAddress = string.Equals(txtServer.Text.Trim(), "localhost", StringComparison.OrdinalIgnoreCase) ? IPAddress.Loopback : ipAddress;
            lblUsingIpAddress.Text = $"Using IP Address: {_ipAddress}";
        }

        private void LoadConfig()
        {
            WindowPlacement.SetPlacement(Handle, Settings1.Default.MainWindowPlacement);
            txtServer.Text = string.IsNullOrEmpty(Settings1.Default.ServerName) ? "localhost" : Settings1.Default.ServerName;
            txtPortListening.Text = Settings1.Default.PortListening == 0 ? "49999" : Settings1.Default.PortListening.ToString();
            txtPortHistorical.Text = Settings1.Default.PortHistorical == 0 ? "49998" : Settings1.Default.PortHistorical.ToString();
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        private void SaveConfig()
        {
            Settings1.Default.MainWindowPlacement = WindowPlacement.GetPlacement(Handle);
            Settings1.Default.ServerName = txtServer.Text;
            Settings1.Default.PortListening = txtPortListening.Text.ToInt();
            Settings1.Default.PortHistorical = txtPortHistorical.Text.ToInt();
            Settings1.Default.Save();
        }

        private void txtServer_Leave(object sender, EventArgs e)
        {
            SetIpAddress();
        }
    }
}