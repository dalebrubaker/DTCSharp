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

namespace TestServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var serverName = Environment.MachineName;
            lblServerName.Text = $"Server Name: {serverName}";
            var hostEntry = Dns.GetHostEntry(serverName);
            var ipAddress = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            lblServerIPAddress.Text = $"Server IP Address: {ipAddress}";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

        }

        private void btnStop_Click(object sender, EventArgs e)
        {

        }
    }
}
