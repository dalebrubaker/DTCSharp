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
using DTCCommon.Exceptions;
using DTCCommon.Extensions;
using DTCPB;
using Google.Protobuf;

namespace TestClient
{
    public partial class Form1 : Form
    {
        private Client _client;
        private uint _symbolId;
        private Client _clientHistorical;

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
            if (_client != null)
            {
                UnregisterClientEvents(_client);
                _client.Dispose();
                _client = null;
                UnregisterClientEvents(_clientHistorical);
                _clientHistorical.Dispose();
                _clientHistorical = null;
                toolStripStatusLabel1.Text = "Disconnected";
            }
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

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            DisposeClient(); // remove the old client just in case it was missed elsewhere
            _client = new Client(txtServer.Text, PortListener);
            RegisterClientEvents(_client);
            await LogonAsync(_client, "TestClient", false, EncodingEnum.BinaryEncoding);

            //_clientHistorical = new Client(txtServer.Text, PortHistorical);
            //RegisterClientEvents(_clientHistorical);
            //try
            //{
            //    await LogonAsync(_clientHistorical, "TestClientHistorical", true);
            //}
            //catch (TaskCanceledException)
            //{
                
            //    throw;
            //}
        }

        private void UnregisterClientEvents(Client client)
        {
            client.EncodingResponseEvent -= Client_EncodingResponseEvent;
            client.UserMessageEvent -= Client_UserMessageEvent;
            client.GeneralLogMessageEvent -= Client_GeneralLogMessageEvent;
            client.ExchangeListResponseEvent -= Client_ExchangeListResponseEvent;
            client.MarketDataRejectEvent -= Client_MarketDataRejectEvent;
            client.MarketDataFeedStatusEvent -= Client_MarketDataFeedStatusEvent;
            client.MarketDataFeedSymbolStatusEvent -= Client_MarketDataFeedSymbolStatusEvent;
            client.MarketDataSnapshotEvent -= Client_MarketDataSnapshotEvent;
            client.MarketDataSnapshotIntEvent -= Client_MarketDataSnapshotIntEvent;

            client.MarketDataUpdateTradeCompactEvent -= Client_MarketDataUpdateTradeCompactEvent;
            client.MarketDataUpdateTradeEvent -= Client_MarketDataUpdateTradeEvent;
            client.MarketDataUpdateTradeIntEvent -= Client_MarketDataUpdateTradeIntEvent;
            client.MarketDataUpdateBidAskCompactEvent -= Client_MarketDataUpdateBidAskCompactEvent;
            client.MarketDataUpdateBidAskEvent -= Client_MarketDataUpdateBidAskEvent;
            client.MarketDataUpdateBidAskIntEvent -= Client_MarketDataUpdateBidAskIntEvent;

            client.MarketDataUpdateSessionVolumeEvent -= Client_MarketDataUpdateSessionVolumeEvent;
            client.MarketDataUpdateSessionHighEvent -= Client_MarketDataUpdateSessionHighEvent;
            client.MarketDataUpdateSessionLowEvent -= Client_MarketDataUpdateSessionLowEvent;
        }

        private void RegisterClientEvents(Client client)
        {
            client.EncodingResponseEvent += Client_EncodingResponseEvent;
            client.UserMessageEvent += Client_UserMessageEvent;
            client.GeneralLogMessageEvent += Client_GeneralLogMessageEvent;
            client.ExchangeListResponseEvent += Client_ExchangeListResponseEvent;
            client.MarketDataRejectEvent += Client_MarketDataRejectEvent;
            client.MarketDataFeedStatusEvent += Client_MarketDataFeedStatusEvent;
            client.MarketDataFeedSymbolStatusEvent += Client_MarketDataFeedSymbolStatusEvent;
            client.MarketDataSnapshotEvent += Client_MarketDataSnapshotEvent;
            client.MarketDataSnapshotIntEvent += Client_MarketDataSnapshotIntEvent;

            client.MarketDataUpdateTradeCompactEvent += Client_MarketDataUpdateTradeCompactEvent;
            client.MarketDataUpdateTradeEvent += Client_MarketDataUpdateTradeEvent;
            client.MarketDataUpdateTradeIntEvent += Client_MarketDataUpdateTradeIntEvent;
            client.MarketDataUpdateBidAskCompactEvent += Client_MarketDataUpdateBidAskCompactEvent;
            client.MarketDataUpdateBidAskEvent += Client_MarketDataUpdateBidAskEvent;
            client.MarketDataUpdateBidAskIntEvent += Client_MarketDataUpdateBidAskIntEvent;

            client.MarketDataUpdateSessionVolumeEvent += Client_MarketDataUpdateSessionVolumeEvent;
            client.MarketDataUpdateSessionHighEvent += Client_MarketDataUpdateSessionHighEvent;
            client.MarketDataUpdateSessionLowEvent += Client_MarketDataUpdateSessionLowEvent;
        }

        private void Client_MarketDataUpdateSessionLowEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateSessionLow> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControl3.LogMessage($"Market Data new session low for {combo}: {response.Price}");
        }

        private void Client_MarketDataUpdateSessionHighEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateSessionHigh> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControl3.LogMessage($"Market Data new session high for {combo}: {response.Price}");
        }

        private void Client_MarketDataUpdateSessionVolumeEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateSessionVolume> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControl3.LogMessage($"Market Data session volume correction for {combo}: {response.Volume}");
        }

        private void Client_MarketDataUpdateBidAskIntEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateBidAsk_Int> e)
        {
            if (!cbShowBidAsk.Checked)
            {
                return;
            }
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DateTime4ByteToUtc().ToLocalTime();
            logControl3.LogMessage(
                $"Market Data Update Bid/Ask Int for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime}");
        }

        private void Client_MarketDataUpdateBidAskEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateBidAsk> e)
        {
            if (!cbShowBidAsk.Checked)
            {
                return;
            }
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DateTime4ByteToUtc().ToLocalTime();
            logControl3.LogMessage(
                $"Market Data Update Bid/Ask for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime}");
        }

        private void Client_MarketDataUpdateBidAskCompactEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateBidAskCompact> e)
        {
            if (!cbShowBidAsk.Checked)
            {
                return;
            }
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DateTime4ByteToUtc().ToLocalTime();
            logControl3.LogMessage(
                $"Market Data Update Bid/Ask Compact for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime}");
        }

        private void Client_MarketDataUpdateTradeIntEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateTrade_Int> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DateTimeDoubleToUtc().ToLocalTime();
            logControl3.LogMessage($"Market Data Update Trade Int for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime} B/A:{response.AtBidOrAsk}");
        }

        private void Client_MarketDataUpdateTradeEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateTrade> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DateTimeDoubleToUtc().ToLocalTime();
            logControl3.LogMessage($"Market Data Update Trade for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime} B/A:{response.AtBidOrAsk}");
        }

        private void Client_MarketDataUpdateTradeCompactEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateTradeCompact> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DateTime4ByteToUtc().ToLocalTime();
            logControl3.LogMessage($"Market Data Update Trade Compact for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime} B/A:{response.AtBidOrAsk}");
        }

        private void Client_MarketDataFeedSymbolStatusEvent(object sender, DTCCommon.EventArgs<MarketDataFeedSymbolStatus> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControl3.LogMessage($"Market Data Feed status for {combo}: {response.Status}");
        }

        private void Client_MarketDataFeedStatusEvent(object sender, DTCCommon.EventArgs<MarketDataFeedStatus> e)
        {
            var response = e.Data;
            logControl3.LogMessage($"Market Data Feed status: {response.Status}");
        }

        private void Client_MarketDataSnapshotEvent(object sender, DTCCommon.EventArgs<MarketDataSnapshot> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var lines = new List<string>
            {
                $"Market Data Snapshot for {combo}:",
                $"SessionSettlementPrice: {response.SessionSettlementPrice}",
                $"SessionOpenPrice: {response.SessionOpenPrice}",
                $"SessionHighPrice: {response.SessionHighPrice}",
                $"SessionLowPrice: {response.SessionLowPrice}",
                $"SessionVolume: {response.SessionVolume}",
                $"SessionNumTrades: {response.SessionNumTrades}",
                $"OpenInterest: {response.OpenInterest}",
                $"BidPrice: {response.BidPrice}",
                $"AskPrice: {response.AskPrice}",
                $"AskQuantity: {response.AskQuantity}",
                $"BidQuantity: {response.BidQuantity}",
                $"LastTradePrice: {response.LastTradePrice}",
                $"LastTradeVolume: {response.LastTradeVolume}",
                $"LastTradeDateTime: {response.LastTradeDateTime}",
                $"BidAskDateTime: {response.BidAskDateTime}",
                $"SessionSettlementDateTime: {response.SessionSettlementDateTime}",
                $"TradingSessionDate: {response.TradingSessionDate}",
            };
            logControl3.LogMessagesReversed(lines);
        }

        private void Client_MarketDataSnapshotIntEvent(object sender, DTCCommon.EventArgs<MarketDataSnapshot_Int> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var lines = new List<string>
            {
                $"Market Data Snapshot for {combo}:",
                $"SessionSettlementPrice: {response.SessionSettlementPrice}",
                $"SessionOpenPrice: {response.SessionOpenPrice}",
                $"SessionHighPrice: {response.SessionHighPrice}",
                $"SessionLowPrice: {response.SessionLowPrice}",
                $"SessionVolume: {response.SessionVolume}",
                $"SessionNumTrades: {response.SessionNumTrades}",
                $"OpenInterest: {response.OpenInterest}",
                $"BidPrice: {response.BidPrice}",
                $"AskPrice: {response.AskPrice}",
                $"AskQuantity: {response.AskQuantity}",
                $"BidQuantity: {response.BidQuantity}",
                $"LastTradePrice: {response.LastTradePrice}",
                $"LastTradeVolume: {response.LastTradeVolume}",
                $"LastTradeDateTime: {response.LastTradeDateTime}",
                $"BidAskDateTime: {response.BidAskDateTime}",
                $"SessionSettlementDateTime: {response.SessionSettlementDateTime}",
                $"TradingSessionDate: {response.TradingSessionDate}",
            };
            logControl3.LogMessagesReversed(lines);
        }


        private void Client_MarketDataRejectEvent(object sender, DTCCommon.EventArgs<MarketDataReject> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControl3.LogMessage($"Market data request rejected for {combo} because {response.RejectText}");
        }


        private async Task LogonAsync(Client client, string clientName, bool isHistoricalClient, EncodingEnum encoding)
        {
            try
            {
                const int heartbeatIntervalInSeconds = 10;
                const int timeout = 5000;
                var response = await client.LogonAsync(encoding, heartbeatIntervalInSeconds, isHistoricalClient, timeout, clientName);
                if (response == null)
                {
                    toolStripStatusLabel1.Text = "Disconnected";
                    logControl1.LogMessage("Null logon response from logon attempt to " + clientName);
                    return;
                }
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
            catch (TaskCanceledException exc)
            {

                throw;
            }
            catch (Exception ex)
            {
                throw new DTCSharpException(ex.Message, ex);
            }
        }

        private void Client_ExchangeListResponseEvent(object sender, DTCCommon.EventArgs<ExchangeListResponse> e)
        {
            var response = e.Data;
            var lines = new List<string>
            {
                "Exchanges List:",
                $"RequestID: {response.RequestID}",
                $"Exchange: {response.Exchange}",
                $"Description: {response.Description}",
            };
            logControl2.LogMessagesReversed(lines);
        }

        private void Client_GeneralLogMessageEvent(object sender, DTCCommon.EventArgs<GeneralLogMessage> e)
        {
            logControl1.LogMessage($"GeneralLogMessage: {e.Data.MessageText}");
        }

        private void Client_UserMessageEvent(object sender, DTCCommon.EventArgs<UserMessage> e)
        {
            logControl1.LogMessage($"UserMessage: {e.Data.UserMessage_}");
        }

        private void Client_EncodingResponseEvent(object sender, DTCCommon.EventArgs<DTCPB.EncodingResponse> e)
        {
            var client = (Client)sender;
            var response = e.Data;
            logControl1.LogMessage($"{_client.ClientName} encoding is {response.Encoding}");
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessages_AuthenticationConnectionMonitoringMessages.php#Messages-LOGON_RESPONSE
        /// </summary>
        /// <param name="response"></param>
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
                $"HistoricalPriceDataSupported: {response.HistoricalPriceDataSupported}",
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
            _client?.SendMessage(DTCMessageType.Logoff, logoffRequest);
            await Task.Delay(100);
            DisposeClient();
        }

        private void btnExchanges_Click(object sender, EventArgs e)
        {
            // TODO: Change this later to a Client async method that returns the list of ExchangeListResponse objects
            var exchangeListRequest = new ExchangeListRequest
            {
                RequestID = _client.NextRequestId
            };
            logControl2.LogMessage($"Sent exchangeListRequest, RequestID={exchangeListRequest.RequestID}");
            _client.SendMessage(DTCMessageType.ExchangeListRequest, exchangeListRequest);
            if (string.IsNullOrEmpty(_client.LogonResponse.SymbolExchangeDelimiter))
            {
                logControl2.LogMessage("The LogonResponse.SymbolExchangeDelimiter is empty, so Exchanges probably aren't supported.");
            }
        }

        private async void btnSymbolDefinition_Click(object sender, EventArgs e)
        {
            const int timeout = 5000;
            var response = await _client.GetSecurityDefinitionAsync(txtSymbolDef.Text, timeout);
            var lines = new List<string>
            {
                "Security Definition Response:",
                $"RequestID: {response.RequestID}",
                $"Symbol: {response.Symbol}",
                $"UnderlyingSymbol: {response.UnderlyingSymbol}",
                $"Exchange: {response.Exchange}",
                $"SecurityType: {response.SecurityType}",
                $"Description: {response.Description}",
                $"MinPriceIncrement: {response.MinPriceIncrement}",
                $"PriceDisplayFormat: {response.PriceDisplayFormat}",
                $"DisplayPriceMultiplier: {response.DisplayPriceMultiplier}",
                $"CurrencyValuePerIncrement: {response.CurrencyValuePerIncrement}",
                $"IsFinalMessage: {response.IsFinalMessage}",
                $"FloatToIntPriceMultiplier: {response.FloatToIntPriceMultiplier}",
                $"IntegerToFloatPriceDivisor: {response.IntToFloatPriceDivisor}",
                $"UpdatesBidAskOnly: {response.UpdatesBidAskOnly}",
                $"StrikePrice: {response.StrikePrice}",
                $"PutOrCall: {response.PutOrCall}",
                $"ShortInterest: {response.ShortInterest}",
                $"SecurityExpirationDate: {response.SecurityExpirationDate}",
                $"BuyRolloverInterest: {response.BuyRolloverInterest}",
                $"SellRolloverInterest: {response.SellRolloverInterest}",
                $"EarningsPerShare: {response.EarningsPerShare}",
                $"SharesOutstanding: {response.SharesOutstanding}",
                $"IntToFloatQuantityDivisor: {response.IntToFloatQuantityDivisor}",
                $"HasMarketDepthData: {response.HasMarketDepthData}",
            };
            logControl2.LogMessagesReversed(lines);
        }

        private void btnSubscribe_Click(object sender, EventArgs e)
        {
            _symbolId = _client.SubscribeMarketData(txtSymbolLevel1.Text, "");
        }

        private void btnUnsubscribe_Click(object sender, EventArgs e)
        {
            _client.UnsubscribeMarketData(_symbolId);
        }
    }
}
