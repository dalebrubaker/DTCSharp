using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTCClient;
using DTCCommon;
using DTCPB;
using Google.Protobuf;

namespace TestClient
{
    public partial class ClientForm : Form
    {
        private const int MaxLevel1Rows = 100;
        private ClientDTC _clientListener;
        private ClientDTC _clientHistorical;
        private CancellationTokenSource _ctsLevel1Symbol1;
        private CancellationTokenSource _ctsLevel1Symbol2;
        private List<HistoricalPriceDataRecordResponse> _historicalPriceDataRecordResponses;
        private int _numRegistrationsForMarketData;
        private uint _symbolId1;
        private uint _symbolId2;
        private List<MarketDataUpdateTradeCompact> _ticks;

        /// <summary>
        /// This is the list of Symbol.Exchange by symbolId, which is 1-based
        /// </summary>
        private readonly Dictionary<string, uint> _symbolIdBySymbolDotExchange = new Dictionary<string, uint>();

        /// <summary>
        /// This is the inverse of _symbolIdBySymbolDotExchange
        /// </summary>
        private readonly Dictionary<uint, string> _symbolDotExchangeBySymbolId = new Dictionary<uint, string>();

        private Stopwatch _stopWatch;
        private LogonResponse _logonResponseHistorical;
        private ClientConnector _clientConnector;

        private static uint s_nextClientId;

        public static uint NextClientId => ++s_nextClientId;

        public ClientForm()
        {
            InitializeComponent();
            btnDisconnectListener.Enabled = false;
            Disposed += Form1_Disposed;
            toolStripStatusLabel1.Text = "Disconnected";
            btnUnsubscribe1.Enabled = false;
            btnUnsubscribe2.Enabled = false;
            _ticks = new List<MarketDataUpdateTradeCompact>();
            cbxEncoding.DataSource = Enum.GetValues(typeof(EncodingEnum));
            cbxEncoding.SelectedItem = EncodingEnum.ProtocolBuffers;
            cbxInstrumentTypes.DataSource = Enum.GetValues(typeof(SecurityTypeEnum));
            cmbxOrderType.DataSource = Enum.GetValues(typeof(OrderTypeEnum));
            cmbxOrderTypeOCO.DataSource = Enum.GetValues(typeof(OrderTypeEnum));
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

        private async void Form1_Disposed(object sender, EventArgs e)
        {
            await DisposeClientListenerAsync().ConfigureAwait(false);
        }

        private async Task DisposeClientListenerAsync()
        {
            if (_clientHistorical != null)
            {
                // Wait for pending message to finish
                await Task.Delay(100).ConfigureAwait(false);
                _clientHistorical.Dispose(); // will throw Disconnected event
                UnregisterClientEvents(_clientHistorical);
                _clientHistorical = null;
                toolStripStatusLabel1.Text = "Disconnected Client Historical";
            }
        }

        private async Task DisposeClientHistoricalAsync()
        {
            if (_clientListener != null)
            {
                // Wait for pending message to finish
                await Task.Delay(100).ConfigureAwait(false);
                _clientListener.Dispose(); // will throw Disconnected event
                UnregisterClientEvents(_clientListener);
                _clientListener = null;
                toolStripStatusLabel1.Text = "Disconnected Client Listener";
            }
        }

        private async void btnConnectListener_Click(object sender, EventArgs e)
        {
            await DisposeClientListenerAsync(); // remove the old client just in case it was missed elsewhere
            btnConnectListener.Enabled = false;
            btnDisconnectListener.Enabled = true;
            const string ClientName = "TestClient Listener";
            try
            {
                _clientListener = new ClientDTC();
                _clientListener.StartClient(txtServer.Text, PortListener);
                if (_clientListener == null)
                {
                    MessageBox.Show($"Cannot connect to {txtServer.Text}:{PortListener}");
                    btnConnectListener.Enabled = true;
                    btnDisconnectListener.Enabled = false;
                    return;
                }
                RegisterClientEvents(_clientListener);
                const int HeartbeatIntervalInSeconds = 10;
                var encoding = (EncodingEnum)cbxEncoding.SelectedItem;
                DisplayEncodingResponse(logControlConnect, encoding);
                var generalTextData = ""; // "cme";
                var (logonResponse, error) =
                    _clientListener.Logon(ClientName, HeartbeatIntervalInSeconds, encoding, txtUsername.Text, txtPassword.Text, generalTextData);
                if (logonResponse == null)
                {
                    toolStripStatusLabel1.Text = "Disconnected";
                    logControlConnect.LogMessage("Null logon response from logon attempt to " + ClientName);
                    btnConnectListener.Enabled = true;
                    btnDisconnectListener.Enabled = false;
                    logControlConnect.LogMessage(error.ResultText);
                    return;
                }
                toolStripStatusLabel1.Text = logonResponse.Result == LogonStatusEnum.LogonSuccess ? "Connected" : "Disconnected";
                switch (logonResponse.Result)
                {
                    case LogonStatusEnum.LogonStatusUnset:
                        throw new ArgumentException("Unexpected logon result");
                    case LogonStatusEnum.LogonSuccess:
                        DisplayLogonResponse(logControlConnect, _clientListener, logonResponse);
                        break;
                    case LogonStatusEnum.LogonErrorNoReconnect:
                        logControlConnect.LogMessage(
                            $"{_clientListener} Login failed: {logonResponse.Result} {logonResponse.ResultText}. Reconnect not allowed.");
                        await DisposeClientListenerAsync().ConfigureAwait(false);
                        break;
                    case LogonStatusEnum.LogonError:
                        logControlConnect.LogMessage($"{_clientListener} Login failed: {logonResponse.Result} {logonResponse.ResultText}.");
                        await DisposeClientListenerAsync().ConfigureAwait(false);
                        break;
                    case LogonStatusEnum.LogonReconnectNewAddress:
                        logControlConnect.LogMessage(
                            $"{_clientListener} Login failed: {logonResponse.Result} {logonResponse.ResultText}\nReconnect to: {logonResponse.ReconnectAddress}");
                        await DisposeClientListenerAsync().ConfigureAwait(false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (TaskCanceledException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DisplayEncodingResponse(LogControl logControl, EncodingEnum encoding)
        {
            logControl.LogMessage($"Encoding is set to {encoding}");
        }

        private void UnregisterClientEvents(ClientDTC client)
        {
            client.EncodingResponseEvent -= Client_EncodingResponseEvent;
            client.UserMessageEvent -= Client_UserMessageEvent;
            client.GeneralLogMessageEvent -= Client_GeneralLogMessageEvent;
            client.ExchangeListResponseEvent -= Client_ExchangeListResponseEvent;
            client.HeartbeatEvent -= Client_OnHeartbeatEvent;
            client.ConnectedEvent -= Client_Connected;
            client.DisconnectedEvent -= Client_Disconnected;
        }

        private void UnregisterClientEventsMarketData(ClientDTC client)
        {
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
            client.MarketDataFeedStatusEvent -= ClientOnMarketDataFeedStatusEvent;
            client.MarketDataSnapshotEvent -= ClientOnMarketDataSnapshotEvent;
            client.MarketDataSnapshotIntEvent -= ClientOnMarketDataSnapshotIntEvent;
            client.TradingSymbolStatusEvent -= ClientOnTradingSymbolStatusEvent;
        }

        private void RegisterClientEvents(ClientDTC client)
        {
            client.EncodingResponseEvent += Client_EncodingResponseEvent;
            client.UserMessageEvent += Client_UserMessageEvent;
            client.GeneralLogMessageEvent += Client_GeneralLogMessageEvent;
            client.ExchangeListResponseEvent += Client_ExchangeListResponseEvent;
            client.HeartbeatEvent += Client_OnHeartbeatEvent;
            client.ConnectedEvent += Client_Connected;
            client.DisconnectedEvent += Client_Disconnected;
            client.EveryMessageFromServer += ClientOnEveryMessageFromServer;
            client.OrderUpdateEvent += ClientOnOrderUpdateEvent;
            client.MarketDataFeedStatusEvent += ClientOnMarketDataFeedStatusEvent;
            client.MarketDataSnapshotEvent += ClientOnMarketDataSnapshotEvent;
            client.MarketDataSnapshotIntEvent += ClientOnMarketDataSnapshotIntEvent;
            client.TradingSymbolStatusEvent += ClientOnTradingSymbolStatusEvent;
        }

        private void ClientOnTradingSymbolStatusEvent(object sender, TradingSymbolStatus e)
        {
        }

        private void ClientOnMarketDataSnapshotIntEvent(object sender, MarketDataSnapshot_Int e)
        {
        }

        private void ClientOnMarketDataSnapshotEvent(object sender, MarketDataSnapshot e)
        {
        }

        private void ClientOnMarketDataFeedStatusEvent(object sender, MarketDataFeedStatus e)
        {
        }

        private void ClientOnOrderUpdateEvent(object sender, OrderUpdate orderUpdate)
        {
            logControlTrades.LogMessage($"OrderUpdate:{orderUpdate}");
        }

        private void ClientOnEveryMessageFromServer(object sender, IMessage e)
        {
            if (e is Heartbeat && !cbxShowHeartbeats.Checked)
            {
                return;
            }
            logControlConnect.LogMessage($"EveryMessageEvent {e.GetType().Name}:{e}");
        }

        private void Client_OnHeartbeatEvent(object sender, Heartbeat e)
        {
            // This is being shown with EveryMessageFromServer event
            //logControlConnect.LogMessage($"{e.GetType().Name} received from server. {e}");
        }

        private void Client_Disconnected(object sender, EventArgs args)
        {
            var client = (ClientDTC)sender;
            logControlConnect.LogMessage($"Disconnected from client:{client}");
            ShowDisconnected();
        }

        private void ShowDisconnected()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(ShowDisconnected));
            }
            else
            {
                btnConnectListener.Enabled = true;
                btnDisconnectListener.Enabled = false;
            }
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            var client = (ClientDTC)sender;
            logControlConnect.LogMessage($"Connected to client:{client}");
        }

        private void RegisterClientEventsMarketData(ClientDTC client)
        {
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

        private void Client_MarketDataUpdateSessionLowEvent(object sender, MarketDataUpdateSessionLow response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data new session low for {combo}: {response.Price}");
        }

        private void Client_MarketDataUpdateSessionHighEvent(object sender, MarketDataUpdateSessionHigh response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data new session high for {combo}: {response.Price}");
        }

        private void Client_MarketDataUpdateSessionVolumeEvent(object sender, MarketDataUpdateSessionVolume response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data session volume correction for {combo}: {response.Volume}");
        }

        private void Client_MarketDataUpdateBidAskIntEvent(object sender, MarketDataUpdateBidAsk_Int response)
        {
            if (!cbShowBidAsk1.Checked)
            {
                return;
            }
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTime4ByteToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Bid/Ask Int for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime:yyyyMMdd.HHmmss.fff}");
        }

        private void Client_MarketDataUpdateBidAskEvent(object sender, MarketDataUpdateBidAsk response)
        {
            if (!cbShowBidAsk1.Checked)
            {
                return;
            }
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTime4ByteToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Bid/Ask for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime:yyyyMMdd.HHmmss.fff}");
        }

        private void Client_MarketDataUpdateBidAskCompactEvent(object sender, MarketDataUpdateBidAskCompact e)
        {
            MarketDataUpdateBidAskCompactCallback(e);
        }

        private void Client_MarketDataUpdateTradeIntEvent(object sender, MarketDataUpdateTrade_Int response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTimeWithMillisecondsToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Trade Int for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime:yyyyMMdd.HHmmss.fff} B/A:{response.AtBidOrAsk}");
        }

        private void Client_MarketDataUpdateTradeEvent(object sender, MarketDataUpdateTrade response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTimeWithMillisecondsToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Trade for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime:yyyyMMdd.HHmmss.fff} B/A:{response.AtBidOrAsk}");
        }

        private void Client_MarketDataUpdateTradeCompactEvent(object sender, MarketDataUpdateTradeCompact e)
        {
            MarketDataUpdateTradeCompactCallback(e);
        }

        private void Client_MarketDataFeedSymbolStatusEvent(object sender, MarketDataFeedSymbolStatus response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data Feed status for {combo}: {response.Status}");
        }

        private void Client_MarketDataFeedStatusEvent(object sender, MarketDataFeedStatus response)
        {
            logControlLevel1.LogMessage($"Market Data Feed status: {response.Status}");
        }

        private void Client_MarketDataSnapshotEvent(object sender, MarketDataSnapshot e)
        {
            MarketDataSnapshotCallback(e);
        }

        private void Client_MarketDataSnapshotIntEvent(object sender, MarketDataSnapshot_Int response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
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
                $"TradingSessionDate: {response.TradingSessionDate}"
            };
            lines.Reverse();
            logControlLevel1.LogMessages(lines);
        }

        private void Client_MarketDataRejectEvent(object sender, MarketDataReject marketDataReject)
        {
            var combo = _symbolDotExchangeBySymbolId[marketDataReject.SymbolID];
            logControlLevel1.LogMessage($"Market data request rejected for {combo} because {marketDataReject.RejectText}");
        }

        private void Client_ExchangeListResponseEvent(object sender, ExchangeListResponse response)
        {
            var lines = new List<string>
            {
                "Exchanges List:",
                $"RequestID: {response.RequestID}",
                $"Exchange: {response.Exchange}",
                $"Description: {response.Description}"
            };
            lines.Reverse();
            logControlSymbols.LogMessages(lines);
        }

        private void Client_GeneralLogMessageEvent(object sender, GeneralLogMessage e)
        {
            logControlConnect.LogMessage($"GeneralLogMessage: {e.MessageText}");
        }

        private void Client_UserMessageEvent(object sender, UserMessage e)
        {
            logControlConnect.LogMessage($"UserMessage: {e.UserMessage_}");
        }

        private void Client_EncodingResponseEvent(object sender, EncodingResponse response)
        {
            var client = (ClientDTC)sender;
            logControlConnect.LogMessage($"{client} encoding is {response.Encoding}");
        }

        /// <summary>
        ///     https://dtcprotocol.org/index.php?page=doc/DTCMessages_AuthenticationConnectionMonitoringMessages.php#Messages-LOGON_RESPONSE
        /// </summary>
        /// <param name="logControl"></param>
        /// <param name="client"></param>
        /// <param name="response"></param>
        private void DisplayLogonResponse(LogControl logControl, ClientDTC client, LogonResponse response)
        {
            logControl.LogMessage("");
            logControl.LogMessage("Login succeeded: " + response.Result + " " + response.ResultText);
            var lines = new List<string>
            {
                $"{client} Logon Response info:",
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
                $"UsesMultiplePositionsPerSymbolAndTradeAccount: {response.UsesMultiplePositionsPerSymbolAndTradeAccount}",
                $"MarketDataSupported: {response.MarketDataSupported}",
                $"ProtocolVersion: {response.ProtocolVersion}",
                $"ReconnectAddress: {response.ReconnectAddress}",
                $"Integer_1: {response.Integer1}"
            };
            lines.Reverse();
            logControl.LogMessages(lines);
        }

        private async void btnDisconnectListener_Click(object sender, EventArgs e)
        {
            btnConnectListener.Enabled = true;
            btnDisconnectListener.Enabled = false;
            var logoffRequest = new Logoff
            {
                DoNotReconnect = 1,
                Reason = "User disconnected"
            };
            if (_clientListener != null)
            {
                _clientListener.SendRequest(DTCMessageType.Logoff, logoffRequest);
                await DisposeClientListenerAsync().ConfigureAwait(false);
            }
        }

        private void btnExchanges_Click(object sender, EventArgs e)
        {
            if (_clientListener == null)
            {
                MessageBox.Show("Client is not connected");
                return;
            }
            var list = _clientListener.GetExchanges();
            if (list.Count == 0)
            {
                logControlSymbols.LogMessage("No exchanges returned.");
            }
            else
            {
                list.Reverse();
                logControlSymbols.LogMessages(list);
            }
        }

        private void btnSymbolDefinition_Click(object sender, EventArgs e)
        {
            if (_clientListener == null)
            {
                MessageBox.Show("You must first connect the Listener");
                return;
            }
            var (response, result) = _clientListener.GetSecurityDefinition(txtSymbolDef.Text, txtExchangeSymbols.Text);
            if (result.IsError)
            {
                logControlSymbols.LogMessage(result.ResultText);
                return;
            }

            LogSecurityDefinitionResponse(response);
        }

        private void LogSecurityDefinitionResponse(SecurityDefinitionResponse response)
        {
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
                $"ExchangeSymbol: {response.ExchangeSymbol}",
                $"RolloverDate: {response.RolloverDate.DtcDateTime4ByteToUtc()}",
                $"InitialMarginRequirement: {response.InitialMarginRequirement}",
                $"MaintenanceMarginRequirement: {response.MaintenanceMarginRequirement}",
                $"Currency: {response.Currency}",
                $"ContractSize: {response.ContractSize}",
                $"OpenInterest: {response.OpenInterest}",
                $"IsDelayed: {response.IsDelayed}"
            };
            lines.Reverse();
            logControlSymbols.LogMessages(lines);
        }

        private void btnGetHistoricalTicks_Click(object sender, EventArgs e)
        {
            RequestHistoricalData(HistoricalDataIntervalEnum.IntervalTick);
        }

        private void btnGetHistoricalMinutes_Click(object sender, EventArgs e)
        {
            RequestHistoricalData(HistoricalDataIntervalEnum.Interval1Minute);
        }

        private void btnGetHistoricalDays_Click(object sender, EventArgs e)
        {
            RequestHistoricalData(HistoricalDataIntervalEnum.Interval1Day);
        }

        private void RequestHistoricalData(HistoricalDataIntervalEnum recordInterval)
        {
            if (_clientHistorical == null)
            {
                MessageBox.Show("You must connect first.");
                return;
            }
            if (cbZip.Checked && _logonResponseHistorical.ResultText.Contains("Compression not supported"))
            {
                MessageBox.Show("Compression is not supported per historical LogonResponse. Only supported for BinaryEncoding.");
                return;
            }
            _historicalPriceDataRecordResponses = new List<HistoricalPriceDataRecordResponse>();
            var clientName = $"HistoricalClient|{txtSymbolHistorical.Text}";
            using var clientHistorical = new ClientDTC();
            try
            {
                clientHistorical.StartClient(txtServer.Text, PortHistorical);
                // Note that heartbeatIntervalInSeconds must be 0 so the server doesn't throw us a heartbeat 
                var encoding = (EncodingEnum)cbxEncoding.SelectedItem;
                DisplayEncodingResponse(logControlHistorical, encoding);
                var (logonResponse, result) =
                    clientHistorical.Logon(clientName, requestedEncoding: encoding, userName: txtUsername.Text, password: txtPassword.Text);
                if (result.IsError)
                {
                    logControlHistorical.LogMessage($"{result} on logon attempt to " + clientName);
                    return;
                }
                switch (logonResponse.Result)
                {
                    case LogonStatusEnum.LogonStatusUnset:
                        throw new ArgumentException("Unexpected logon result");
                    case LogonStatusEnum.LogonSuccess:
                        DisplayLogonResponse(logControlHistorical, clientHistorical, logonResponse);
                        break;
                    case LogonStatusEnum.LogonErrorNoReconnect:
                        logControlHistorical.LogMessage(
                            $"{clientHistorical} Login failed: {logonResponse.Result} {logonResponse.ResultText}. Reconnect not allowed.");
                        return;
                    case LogonStatusEnum.LogonError:
                        logControlHistorical.LogMessage($"{clientHistorical} Login failed: {logonResponse.Result} {logonResponse.ResultText}.");
                        return;
                    case LogonStatusEnum.LogonReconnectNewAddress:
                        logControlHistorical.LogMessage(
                            $"{clientHistorical} Login failed: {logonResponse.Result} {logonResponse.ResultText}\nReconnect to: {logonResponse.ReconnectAddress}");
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (TaskCanceledException exc)
            {
                MessageBox.Show(exc.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Now we have successfully logged on
            _stopWatch = Stopwatch.StartNew();
            var error = clientHistorical.GetHistoricalData(ClientDTC.NextRequestId, txtSymbolHistorical.Text, txtExchangeHistorical.Text, recordInterval,
                dtpStart.Value.ToUniversalTime(), DateTime.MinValue, 0U, cbZip.Checked, false, 0x10, HistoricalPriceDataResponseHeaderCallback,
                HistoricalPriceDataRecordResponseCallback);
            if (error.IsError)
            {
                logControlHistorical.LogMessage(error.ResultText);
            }
        }

        private void HistoricalPriceDataRecordResponseCallback(HistoricalPriceDataRecordResponse response)
        {
            _historicalPriceDataRecordResponses.Add(response);
            if (response.IsFinalRecord != 0)
            {
                var lastTime = response.StartDateTime.DtcDateTimeToUtc();
                var firstRecord = _historicalPriceDataRecordResponses[0];
                var startTime = firstRecord.StartDateTime.DtcDateTimeToUtc();

                logControlHistorical.LogMessage($"HistoricalPriceDataTickRecordResponse RequestId:{response.RequestID} received "
                                                + $"{_historicalPriceDataRecordResponses.Count:N0} records in {_stopWatch?.ElapsedMilliseconds:N0} ms "
                                                + $"from {startTime.ToLocalTime()} through {lastTime.ToLocalTime():yyyyMMdd.HHmmss.fff} (local).");
                _stopWatch = null;
            }
        }

        private void HistoricalPriceDataResponseHeaderCallback(HistoricalPriceDataResponseHeader response)
        {
            logControlHistorical.LogMessage(
                $"HistoricalPriceDataResponseHeader RequestId:{response.RequestID} RecordInterval:{response.RecordInterval} UseZLibCompression:{response.UseZLibCompression} NoRecordsToReturn:{response.NoRecordsToReturn}");
        }

        private void btnSubscribeEvents1_Click(object sender, EventArgs e)
        {
            btnUnsubscribe1.Enabled = true;
            btnSubscribeEvents1.Enabled = false;
            if (_numRegistrationsForMarketData++ == 0)
            {
                RegisterClientEventsMarketData(_clientListener);
            }
            logControlLevel1.LogMessage($"Subscribing to market data for {txtSymbolLevel1_1.Text}");
            _symbolId1 = RequireSymbolId(txtSymbolLevel1_1.Text, "");
            _clientListener.SubscribeMarketData(_symbolId1, txtSymbolLevel1_1.Text, "");
        }

        private uint RequireSymbolId(string symbol, string exchange)
        {
            var key = $"{symbol}.{exchange}";
            if (_symbolIdBySymbolDotExchange.TryGetValue(key, out var symbolId))
            {
                return symbolId;
            }
            symbolId = (uint)_symbolIdBySymbolDotExchange.Count + 1; // must be 1-based. 0 means Error
            _symbolIdBySymbolDotExchange.Add(key, symbolId);
            _symbolDotExchangeBySymbolId.Add(symbolId, key);
            return symbolId;
        }

        private void btnUnsubscribe1_Click(object sender, EventArgs e)
        {
            btnUnsubscribe1.Enabled = false;
            btnSubscribeEvents1.Enabled = true;
            btnSubscribeCallbacks1.Enabled = true;
            if (--_numRegistrationsForMarketData == 0)
            {
                UnregisterClientEventsMarketData(_clientListener);
            }
            _ctsLevel1Symbol1?.Cancel();
            _ctsLevel1Symbol1 = null;
            _clientListener.UnsubscribeMarketData(_symbolId1, txtSymbolLevel1_1.Text, "");
        }

        private void btnSubscribeEvents2_Click(object sender, EventArgs e)
        {
            btnUnsubscribe2.Enabled = true;
            btnSubscribeEvents2.Enabled = false;
            if (_numRegistrationsForMarketData++ == 0)
            {
                RegisterClientEventsMarketData(_clientListener);
            }
            logControlLevel1.LogMessage($"Subscribing to market data for {txtSymbolLevel1_2.Text}", "");
            _symbolId2 = RequireSymbolId(txtSymbolLevel1_2.Text, "");
            _clientListener.SubscribeMarketData(_symbolId2, txtSymbolLevel1_2.Text, "");
        }

        private void btnUnsubscribe2_Click(object sender, EventArgs e)
        {
            btnUnsubscribe2.Enabled = false;
            btnSubscribeEvents2.Enabled = true;
            btnSubscribeCallbacks2.Enabled = true;
            if (--_numRegistrationsForMarketData == 0)
            {
                UnregisterClientEventsMarketData(_clientListener);
            }
            _ctsLevel1Symbol2?.Cancel();
            _ctsLevel1Symbol2 = null;
            _clientListener.UnsubscribeMarketData(_symbolId2, txtSymbolLevel1_2.Text, "");
        }

        private void btnSubscribeCallbacks1_Click(object sender, EventArgs e)
        {
            btnUnsubscribe1.Enabled = true;
            btnSubscribeCallbacks1.Enabled = false;
            _ctsLevel1Symbol1 = new CancellationTokenSource();
            var symbol = txtSymbolLevel1_1.Text;
            _symbolId1 = RequireSymbolId(symbol, "");
            logControlLevel1.LogMessage($"Getting market data for {symbol}");
            try
            {
                const int Timeout = 5000;
                var reject = _clientListener.GetMarketDataUpdateTradeCompact(_symbolId1, Timeout, symbol, "", MarketDataSnapshotCallback,
                    MarketDataUpdateTradeCompactCallback, MarketDataUpdateBidAskCompactCallback);
                if (reject != null)
                {
                    var message = $"Subscription to {symbol} rejected: {reject.RejectText}";
                    logControlLevel1.LogMessage(message);
                    MessageBox.Show(message);
                }
            }
            catch (TaskCanceledException)
            {
                // ignore this exception thrown when we unsubscribe
            }
        }

        private void btnSubscribeCallbacks2_Click(object sender, EventArgs e)
        {
            btnUnsubscribe2.Enabled = true;
            btnSubscribeCallbacks2.Enabled = false;
            _ctsLevel1Symbol2 = new CancellationTokenSource();
            var symbol = txtSymbolLevel1_2.Text;
            _symbolId2 = RequireSymbolId(symbol, "");
            logControlLevel1.LogMessage($"Getting market data for {symbol}");
            try
            {
                var reject = _clientListener.GetMarketDataUpdateTradeCompact(_symbolId2, 5000, symbol, "", MarketDataSnapshotCallback,
                    MarketDataUpdateTradeCompactCallback, MarketDataUpdateBidAskCompactCallback);
                if (reject != null)
                {
                    var message = $"Subscription to {symbol} rejected: {reject.RejectText}";
                    logControlLevel1.LogMessage(message);
                    MessageBox.Show(message);
                }
            }
            catch (TaskCanceledException)
            {
                // ignore this exception thrown when we unsubscribe
            }
        }

        private void MarketDataSnapshotCallback(MarketDataSnapshot response)
        {
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
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
                $"TradingSessionDate: {response.TradingSessionDate}"
            };
            lines.Reverse();
            logControlLevel1.LogMessages(lines);
        }

        private void MarketDataUpdateBidAskCompactCallback(MarketDataUpdateBidAskCompact response)
        {
            if (!cbShowBidAsk1.Checked)
            {
                return;
            }
            var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTime4ByteToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Bid/Ask Compact for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime:yyyyMMdd.HHmmss.fff}");
        }

        private void MarketDataUpdateTradeCompactCallback(MarketDataUpdateTradeCompact response)
        {
            // Just add it to the list. timerLevel1Update_Tick() will occasionally pull them off the list.
            _ticks.Add(response);
        }

        /// <summary>
        ///     This event pulls level 1 ticks out of the list and displays them.
        ///     Necessary to avoid overwhelming the UI in a fast market.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerLevel1Update_Tick(object sender, EventArgs e)
        {
            if (_ticks.Count == 0)
            {
                return;
            }
            var newTicks = Interlocked.Exchange(ref _ticks, new List<MarketDataUpdateTradeCompact>());
            logControlLevel1.LogMessage($"Received {newTicks.Count} rows.");
            var count = newTicks.Count;
            if (count > MaxLevel1Rows)
            {
                // If we get a backfill of 1000's of ticks, we don't want to insert them all!
                logControlLevel1.LogMessage($"Skipping {count - MaxLevel1Rows} rows due to high incoming volume.");
                newTicks = newTicks.GetRange(count - MaxLevel1Rows, MaxLevel1Rows);
            }
            var lines = new List<string>(newTicks.Count);
            foreach (var response in newTicks)
            {
                var combo = _symbolDotExchangeBySymbolId[response.SymbolID];
                var dateTime = response.DateTime.DtcDateTime4ByteToUtc().ToLocalTime();
                var line =
                    $"Market Data Update Trade Compact for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime:yyyyMMdd.HHmmss.fff} B/A:{response.AtBidOrAsk}";
                lines.Add(line);
            }
            logControlLevel1.LogMessages(lines);
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            WindowConfig.WindowPlacement.SetPlacement(Handle, Settings1.Default.ClientWindowPlacement);
            txtServer.Text = Settings1.Default.Server;
            txtPortListening.Text = Settings1.Default.PortListening;
            txtPortHistorical.Text = Settings1.Default.PortHistorical;
            txtUsername.Text = Settings1.Default.UserName;
            txtPassword.Text = Settings1.Default.Password;
            txtSymbolDef.Text = Settings1.Default.SymbolDef;
            txtSymbolLevel1_1.Text = Settings1.Default.SymbolLevel1_1;
            txtSymbolLevel1_2.Text = Settings1.Default.SymbolLevel1_2;
            txtSymbolHistorical.Text = Settings1.Default.SymbolHistorical;
            dtpStart.Value = Settings1.Default.HistStart;
            cbZip.Checked = Settings1.Default.Zip;
            txtExchangeSymbols.Text = Settings1.Default.ExchangeHistorical;
            txtExchangeHistorical.Text = Settings1.Default.ExchangeSymbols;
            txtAccount.Text = Settings1.Default.Account;
            txtSymbolTrade.Text = Settings1.Default.SymbolTrade;
            txtExchangeTrade.Text = Settings1.Default.ExchangeTrade;
            txtAccount.Text = Settings1.Default.Account;
            cmbxOrderType.SelectedItem = Settings1.Default.OrderType;
            txtQty.Text = Settings1.Default.Qty;
            txtPrice1.Text = Settings1.Default.Price1;
            txtPrice2.Text = Settings1.Default.Price2;
            cmbxOrderTypeOCO.SelectedItem = Settings1.Default.OrderTypeOCO;
            txtQtyOCO.Text = Settings1.Default.QtyOCO;
            txtPrice1OCO.Text = Settings1.Default.Price1OCO;
            txtPrice2OCO.Text = Settings1.Default.Price2OCO;
            cbxShowHeartbeats.Checked = Settings1.Default.ShowHeartbeats;
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings1.Default.ClientWindowPlacement = WindowConfig.WindowPlacement.GetPlacement(Handle);
            Settings1.Default.Server = txtServer.Text;
            Settings1.Default.PortListening = txtPortListening.Text;
            Settings1.Default.PortHistorical = txtPortHistorical.Text;
            Settings1.Default.UserName = txtUsername.Text;
            Settings1.Default.Password = txtPassword.Text;
            Settings1.Default.SymbolDef = txtSymbolDef.Text;
            Settings1.Default.SymbolLevel1_1 = txtSymbolLevel1_1.Text;
            Settings1.Default.SymbolLevel1_2 = txtSymbolLevel1_2.Text;
            Settings1.Default.SymbolHistorical = txtSymbolHistorical.Text;
            Settings1.Default.HistStart = dtpStart.Value;
            Settings1.Default.Zip = cbZip.Checked;
            Settings1.Default.ExchangeHistorical = txtExchangeSymbols.Text;
            Settings1.Default.ExchangeSymbols = txtExchangeHistorical.Text;
            Settings1.Default.Account = txtAccount.Text;
            Settings1.Default.SymbolTrade = txtSymbolTrade.Text;
            Settings1.Default.ExchangeTrade = txtExchangeTrade.Text;
            Settings1.Default.Account = txtAccount.Text;
            Settings1.Default.OrderType = (OrderTypeEnum)cmbxOrderType.SelectedItem;
            Settings1.Default.Qty = txtQty.Text;
            Settings1.Default.Price1 = txtPrice1.Text;
            Settings1.Default.Price2 = txtPrice2.Text;
            Settings1.Default.OrderTypeOCO = (OrderTypeEnum)cmbxOrderTypeOCO.SelectedItem;
            Settings1.Default.QtyOCO = txtQtyOCO.Text;
            Settings1.Default.Price1OCO = txtPrice1OCO.Text;
            Settings1.Default.Price2OCO = txtPrice2OCO.Text;
            Settings1.Default.ShowHeartbeats = cbxShowHeartbeats.Checked;

            Settings1.Default.Save();
        }

        private async void btnConnectHistorical_Click(object sender, EventArgs e)
        {
            await DisposeClientHistoricalAsync(); // remove the old client just in case it was missed elsewhere
            btnConnectHistorical.Enabled = false;
            btnDisconnectHistorical.Enabled = true;
            const string ClientName = "TestClientHistorical";
            _clientHistorical = new ClientDTC();
            _clientHistorical.StartClient(txtServer.Text, PortHistorical);
            RegisterClientEvents(_clientHistorical);
            try
            {
                var encoding = (EncodingEnum)cbxEncoding.SelectedItem;
                DisplayEncodingResponse(logControlConnect, encoding);
                var (logonResponse, error) =
                    _clientHistorical.Logon("Historical", requestedEncoding: encoding, userName: txtUsername.Text, password: txtPassword.Text);
                _logonResponseHistorical = logonResponse;
                if (error.IsError)
                {
                    toolStripStatusLabel1.Text = "Disconnected historical";
                    logControlConnect.LogMessage($"{error} on logon attempt to " + ClientName);
                    btnConnectHistorical.Enabled = true;
                    btnDisconnectHistorical.Enabled = false;
                    return;
                }
                toolStripStatusLabel1.Text = logonResponse.Result == LogonStatusEnum.LogonSuccess ? "Connected Historical" : "Disconnected Historical";
                switch (logonResponse.Result)
                {
                    case LogonStatusEnum.LogonStatusUnset:
                        throw new ArgumentException("Unexpected logon result");
                    case LogonStatusEnum.LogonSuccess:
                        DisplayLogonResponse(logControlConnect, _clientListener, logonResponse);
                        break;
                    case LogonStatusEnum.LogonErrorNoReconnect:
                        logControlConnect.LogMessage(
                            $"{_clientHistorical} Login failed: {logonResponse.Result} {logonResponse.ResultText}. Reconnect not allowed.");
                        await DisposeClientListenerAsync().ConfigureAwait(false);
                        break;
                    case LogonStatusEnum.LogonError:
                        logControlConnect.LogMessage($"{_clientHistorical} Login failed: {logonResponse.Result} {logonResponse.ResultText}.");
                        await DisposeClientListenerAsync().ConfigureAwait(false);
                        break;
                    case LogonStatusEnum.LogonReconnectNewAddress:
                        logControlConnect.LogMessage(
                            $"{_clientHistorical} Login failed: {logonResponse.Result} {logonResponse.ResultText}\nReconnect to: {logonResponse.ReconnectAddress}");
                        await DisposeClientListenerAsync().ConfigureAwait(false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (TaskCanceledException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void btnDisconnectHistorical_Click(object sender, EventArgs e)
        {
            btnConnectHistorical.Enabled = true;
            btnDisconnectHistorical.Enabled = false;
            var logoffRequest = new Logoff
            {
                DoNotReconnect = 1,
                Reason = "User disconnected"
            };
            if (_clientHistorical != null)
            {
                _clientHistorical.SendRequest(DTCMessageType.Logoff, logoffRequest);
                await DisposeClientHistoricalAsync().ConfigureAwait(false);
            }
        }

        private void btnSymbolsForExxchange_Click(object sender, EventArgs e)
        {
            if (_clientListener == null)
            {
                MessageBox.Show("Client is not connected");
                return;
            }
            var list = _clientListener.GetSymbolsForExchange(txtExchangeSymbols.Text, (SecurityTypeEnum)cbxInstrumentTypes.SelectedItem);
            if (list.Count == 0)
            {
                logControlSymbols.LogMessage("No symbols returned.");
            }
            else
            {
                list.Reverse();
                foreach (var securityDefinitionResponse in list)
                {
                    LogSecurityDefinitionResponse(securityDefinitionResponse);
                }
            }
        }

        private void btnStartListenerClientConnector_Click(object sender, EventArgs e)
        {
            btnStartListenerClientConnector.Enabled = false;
            btnStopListenerClientConnector.Enabled = true;
            const string ClientName = "TestClient Listener ClientConnector";
            const int HeartbeatIntervalInSeconds = 10;
            var encoding = (EncodingEnum)cbxEncoding.SelectedItem;
            _clientConnector = new ClientConnector(txtServer.Text, PortListener, ClientName, 2000, HeartbeatIntervalInSeconds, encoding);
            _clientConnector.ClientConnected += ClientConnectorOnClientConnected;
            _clientConnector.ClientDisconnected += ClientConnectorOnClientDisconnected;
        }

        private void ClientConnectorOnClientDisconnected(object sender, ClientDTC e)
        {
            logControlConnect.LogMessage($"{_clientConnector} is disconnected");
        }

        private void ClientConnectorOnClientConnected(object sender, ClientDTC e)
        {
            logControlConnect.LogMessage($"{_clientConnector} is connected");
        }

        private void btnStopListenerClientConnector_Click(object sender, EventArgs e)
        {
            btnStartListenerClientConnector.Enabled = true;
            btnStopListenerClientConnector.Enabled = false;
            _clientConnector?.Dispose();
        }

        private void btnGetOpenOrders_ClickAsync(object sender, EventArgs e)
        {
            if (_clientListener == null)
            {
                MessageBox.Show("Must connect to listener first");
                return;
            }
            var (openOrderUpdates, error) = _clientListener.GetOpenOrderUpdates(txtAccount.Text);
            foreach (var orderUpdate in openOrderUpdates)
            {
                logControlTrades.LogMessage(orderUpdate.ToString());
            }
        }

        private void btnGetHistoricalFills_Click(object sender, EventArgs e)
        {
            if (_clientListener == null)
            {
                MessageBox.Show("Must connect to listener first");
                return;
            }
            var (historicalFills, error) = _clientListener.GetHistoricalOrderFills(txtAccount.Text);
            foreach (var fill in historicalFills)
            {
                logControlTrades.LogMessage(fill.ToString());
            }
        }

        private void btnBuy_Click(object sender, EventArgs e)
        {
            if (_clientListener == null)
            {
                MessageBox.Show("Must connect to listener first");
                return;
            }
            PlaceOrder(OrderAction.Buy);
        }

        private void btnSell_Click(object sender, EventArgs e)
        {
            if (_clientListener == null)
            {
                MessageBox.Show("Must connect to listener first");
                return;
            }
            PlaceOrder(OrderAction.Sell);
        }

        private void PlaceOrder(OrderAction orderAction)
        {
            var clientId = NextClientId.ToString();
            var orderType = (OrderTypeEnum)cmbxOrderType.SelectedItem;
            int.TryParse(txtQty.Text, out var qty);
            double.TryParse(txtPrice1.Text, out var price1);
            double.TryParse(txtPrice2.Text, out var price2);
            int.TryParse(txtQtyOCO.Text, out var qtyOCO);
            if (qtyOCO == 0)
            {
                _clientListener.SubmitOrder(txtAccount.Text, txtSymbolTrade.Text, clientId, orderType, orderAction, qty, price1, price2);
                return;
            }

            // Do an OCO order
            var clientIdOCO = NextClientId.ToString();
            var orderTypeOCO = (OrderTypeEnum)cmbxOrderTypeOCO.SelectedItem;
            double.TryParse(txtPrice1OCO.Text, out var price1OCO);
            double.TryParse(txtPrice2OCO.Text, out var price2OCO);
            _clientListener.SubmitOcoOrders(txtAccount.Text, txtSymbolTrade.Text, clientId, clientIdOCO, orderType, orderAction, qty, orderTypeOCO, orderAction,
                qtyOCO, price1, price2, price1OCO, price2OCO);
        }
    }
}