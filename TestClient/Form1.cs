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
using DTCPB;
using Google.Protobuf;

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
            DisposeClient();
        }

        private void DisposeClient()
        {
            _client?.Dispose();
            _client = null;
            toolStripStatusLabel1.Text = "Disconnected";
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            int port;
            int.TryParse(txtPortListening.Text, out port);
            DisposeClient(); // remove the old client just in case it was missed elsewhere
            _client = new Client(txtServer.Text, port);
            _client.LogonReponseEvent += Client_LogonResponseEvent;
            _client.EncodingResponseEvent += Client_EncodingResponseEvent;

            await _client.LogonAsync(5, "TestClient");
        }

        private void Client_EncodingResponseEvent(object sender, DTCCommon.EventArgs<DTCPB.EncodingResponse> e)
        {
            var response = e.Data;
            if (response.Encoding != EncodingEnum.ProtocolBuffers)
            {
                MessageBox.Show("Server cannot support Protocol Buffers.");
                DisposeClient();
            }
        }

        private void Client_LogonResponseEvent(object sender, DTCCommon.EventArgs<DTCPB.LogonResponse> e)
        {
            var response = e.Data;
            toolStripStatusLabel1.Text = response.Result == LogonStatusEnum.LogonSuccess ? "Connected" : "Disconnected";
            switch (response.Result)
            {
                case LogonStatusEnum.LogonStatusUnset:
                    throw new ArgumentException("Unexpected logon result");
                case LogonStatusEnum.LogonSuccess:
                    break;
                case LogonStatusEnum.LogonErrorNoReconnect:
                    MessageBox.Show("Login failed: " + response.Result + " " + response.ResultText + "Reconnect not allowed.");
                    break;
                case LogonStatusEnum.LogonError:
                    MessageBox.Show("Login failed: " + response.Result + " " + response.ResultText);
                    DisposeClient();
                    break;
                case LogonStatusEnum.LogonReconnectNewAddress:
                    MessageBox.Show("Login failed: " + response.Result + " " + response.ResultText + "\nReconnect to:" + response.ReconnectAddress);
                    DisposeClient();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (response.Result != LogonStatusEnum.LogonSuccess)
            {
            }
        }

        private async void btnDisconnect_Click(object sender, EventArgs e)
        {
            var logoffRequest = new Logoff
            {
                DoNotReconnect = 1,
                Reason = "User disconnected"
            };
            _client.SendRequest(DTCMessageType.Logoff, logoffRequest.ToByteArray());
            await Task.Delay(100);
            DisposeClient();
        }
    }
}
