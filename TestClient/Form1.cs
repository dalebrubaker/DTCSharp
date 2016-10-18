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
                logControl1.LogMessage("Server cannot support Protocol Buffers.");
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
                    DisplayLogonResponse(response);
                    break;
                case LogonStatusEnum.LogonErrorNoReconnect:
                    logControl1.LogMessage("Login failed: " + response.Result + " " + response.ResultText + "Reconnect not allowed.");
                    break;
                case LogonStatusEnum.LogonError:
                    logControl1.LogMessage("Login failed: " + response.Result + " " + response.ResultText);
                    DisposeClient();
                    break;
                case LogonStatusEnum.LogonReconnectNewAddress:
                    logControl1.LogMessage("Login failed: " + response.Result + " " + response.ResultText + "\nReconnect to:" + response.ReconnectAddress);
                    DisposeClient();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DisplayLogonResponse(LogonResponse response)
        {
            logControl1.LogMessage("Login succeeded: " + response.Result + " " + response.ResultText);
            var lines = new List<string>
            {
                "Logon Response info:",
                $"Result: {response.Result}",
                $"ResultText: {response.ResultText}",
                $"ServerName: {response.ServerName}",
                $"MarketDepthUpdatesBestBidAndAsk: {response.MarketDepthUpdatesBestBidAndAsk}",
                $"TradingIsSupported: {response.TradingIsSupported}",
                $"OCOOrdersSupported: {response.OCOOrdersSupported}",
                $"OrderCancelReplaceSupported: {response.OrderCancelReplaceSupported}",
                $"SymbolExchangeDelimiter: {response.SymbolExchangeDelimiter}",
                $"SecurityDefinitionsSupported: {response.SecurityDefinitionsSupported}",
                $"ResubscribeWhenMarketDataFeedAvailable: {response.ResubscribeWhenMarketDataFeedAvailable}",
                $"MarketDepthIsSupported: {response.MarketDepthIsSupported}",
                $"OneHistoricalPriceDataRequestPerConnection: {response.OneHistoricalPriceDataRequestPerConnection}",
                $"BracketOrdersSupported: {response.BracketOrdersSupported}",
                $"UseIntegerPriceOrderMessages: {response.UseIntegerPriceOrderMessages}",
                $"UsesMultiplePositionsPerSymbolAndTradeAccount: {response.UsesMultiplePositionsPerSymbolAndTradeAccount}",
                $"MarketDataSupported: {response.MarketDataSupported}",
                $"ProtocolVersion: {response.ProtocolVersion}",
                $"ReconnectAddress: {response.ReconnectAddress}",
                $"Integer_1: {response.Integer1}"
            };
            logControl1.LogMessagesReversed(lines);
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
