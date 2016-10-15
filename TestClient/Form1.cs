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
        }


        private void btnConnect_Click(object sender, EventArgs e)
        {
            int port;
            int.TryParse(txtPortListening.Text, out port);
            _client = new Client(txtServer.Text, port);
            var connected = _client.Connect();
        }
    }
}
