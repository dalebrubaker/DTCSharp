using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTCClient;

namespace TestClient
{
    public partial class Form1 : Form
    {
        private Client _client;

        public Form1()
        {
            InitializeComponent();
            this.Disposed += Form1_Disposed;
            toolStripStatusLabel1.Text = "Disconnected";
        }

        private void Form1_Disposed(object sender, EventArgs e)
        {
            _client?.Dispose();
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            int port;
            int.TryParse(txtPortListening.Text, out port);
            _client = new Client(txtServer.Text, port);
            _client.LogonReponseEvent += Client_LogonReponseEvent;
            var connected = await _client.ConnectAsync();
            toolStripStatusLabel1.Text = connected ? "Connected" : "Disconnected";
            if (connected)
            {
                await _client.LogonAsync(5, "TestClient");
            }
        }

        private void Client_LogonReponseEvent(object sender, DTCCommon.EventArgs<DTCPB.LogonResponse> e)
        {
            var response = e.Data;
        }
    }
}
