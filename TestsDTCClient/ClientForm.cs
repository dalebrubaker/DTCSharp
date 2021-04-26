using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTCClient;
using DTCCommon;
using DTCCommon.Extensions;
using DTCPB;

namespace TestsDTCClient
{
    public partial class ClientForm : Form
    {
        private const int MaxLevel1Rows = 100;
        private Client _client;
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

        public ClientForm()
        {
            InitializeComponent();
            btnDisconnect.Enabled = false;
            Disposed += Form1_Disposed;
            toolStripStatusLabel1.Text = "Disconnected";
            btnUnsubscribe1.Enabled = false;
            btnUnsubscribe2.Enabled = false;
            _ticks = new List<MarketDataUpdateTradeCompact>();
            cbxEncoding.DataSource = Enum.GetValues(typeof(EncodingEnum));
            cbxEncoding.SelectedItem = EncodingEnum.ProtocolBuffers;
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
            await DisposeClientAsync().ConfigureAwait(false);
        }

        private async Task DisposeClientAsync()
        {
            if (_client != null)
            {
                // Wait for pending message to finish
                await Task.Delay(100).ConfigureAwait(false);
                _client.Dispose(); // will throw Disconnected event
                UnregisterClientEvents(_client);
                _client = null;
                toolStripStatusLabel1.Text = "Disconnected";
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            await DisposeClientAsync().ConfigureAwait(true); // remove the old client just in case it was missed elsewhere
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            _client = new Client(txtServer.Text, PortListener, 30000);
            RegisterClientEvents(_client);
            var clientName = "TestClient";
            try
            {
                const int heartbeatIntervalInSeconds = 10;
                const int timeout = 5000;
                const bool useHeartbeat = true;

                var encoding = (EncodingEnum)cbxEncoding.SelectedItem;

                // Make a connection
                var encodingResponse = await _client.ConnectAsync(encoding, "TestClientPrimary", timeout).ConfigureAwait(true);
                //var encodingResponse = await _client.ConnectAsync(EncodingEnum.ProtocolBuffers, "TestClientPrimary", timeout).ConfigureAwait(true);
                if (encodingResponse == null)
                {
                    // timed out
                    MessageBox.Show("Timed out trying to connect.");
                    return;
                }
                DisplayEncodingResponse(logControlConnect, _client, encodingResponse);
                var logonResponse = await _client.LogonAsync(heartbeatIntervalInSeconds, useHeartbeat, timeout, txtUsername.Text, txtPassword.Text)
                    .ConfigureAwait(true);
                if (logonResponse == null)
                {
                    toolStripStatusLabel1.Text = "Disconnected";
                    logControlConnect.LogMessage("Null logon response from logon attempt to " + clientName);
                    return;
                }
                toolStripStatusLabel1.Text = logonResponse.Result == LogonStatusEnum.LogonSuccess ? "Connected" : "Disconnected";
                switch (logonResponse.Result)
                {
                    case LogonStatusEnum.LogonStatusUnset:
                        throw new ArgumentException("Unexpected logon result");
                    case LogonStatusEnum.LogonSuccess:
                        DisplayLogonResponse(logControlConnect, _client, logonResponse);
                        break;
                    case LogonStatusEnum.LogonErrorNoReconnect:
                        logControlConnect.LogMessage($"{_client} Login failed: {logonResponse.Result} {logonResponse.ResultText}. Reconnect not allowed.");
                        await DisposeClientAsync().ConfigureAwait(false);
                        break;
                    case LogonStatusEnum.LogonError:
                        logControlConnect.LogMessage($"{_client} Login failed: {logonResponse.Result} {logonResponse.ResultText}.");
                        await DisposeClientAsync().ConfigureAwait(false);
                        break;
                    case LogonStatusEnum.LogonReconnectNewAddress:
                        logControlConnect.LogMessage(
                            $"{_client} Login failed: {logonResponse.Result} {logonResponse.ResultText}\nReconnect to: {logonResponse.ReconnectAddress}");
                        await DisposeClientAsync().ConfigureAwait(false);
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

        private void DisplayEncodingResponse(LogControl logControl, Client client, EncodingResponse encodingResponse)
        {
            logControl.LogMessage($"Encoding is set to {encodingResponse.Encoding}");
        }

        private void UnregisterClientEvents(Client client)
        {
            client.EncodingResponseEvent -= Client_EncodingResponseEvent;
            client.UserMessageEvent -= Client_UserMessageEvent;
            client.GeneralLogMessageEvent -= Client_GeneralLogMessageEvent;
            client.ExchangeListResponseEvent -= Client_ExchangeListResponseEvent;
            client.HeartbeatEvent -= Client_OnHeartbeatEvent;
            client.Connected -= Client_Connected;
            client.Disconnected -= Client_Disconnected;
        }

        private void UnregisterClientEventsMarketData(Client client)
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
        }

        private void RegisterClientEvents(Client client)
        {
            client.EncodingResponseEvent += Client_EncodingResponseEvent;
            client.UserMessageEvent += Client_UserMessageEvent;
            client.GeneralLogMessageEvent += Client_GeneralLogMessageEvent;
            client.ExchangeListResponseEvent += Client_ExchangeListResponseEvent;
            client.HeartbeatEvent += Client_OnHeartbeatEvent;
            client.Connected += Client_Connected;
            client.Disconnected += Client_Disconnected;
        }

        private void Client_OnHeartbeatEvent(object sender, Heartbeat e)
        {
            logControlConnect.LogMessage("Heartbeat received from server.");
        }

        private void Client_Disconnected(object sender, Error error)
        {
            logControlConnect.LogMessage(error.ResultText, "Disconnected");
            var client = (Client)sender;
            logControlConnect.LogMessage($"Disconnected from client:{client.ClientName} due to {error.ResultText}");
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
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            }
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            var client = (Client)sender;
            logControlConnect.LogMessage($"Connected to client:{client.ClientName}");
        }

        private void RegisterClientEventsMarketData(Client client)
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
            logControlLevel1.LogMessagesReversed(lines);
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
            logControlSymbols.LogMessagesReversed(lines);
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
            var client = (Client)sender;
            logControlConnect.LogMessage($"{client.ClientName} encoding is {response.Encoding}");
        }

        /// <summary>
        ///     https://dtcprotocol.org/index.php?page=doc/DTCMessages_AuthenticationConnectionMonitoringMessages.php#Messages-LOGON_RESPONSE
        /// </summary>
        /// <param name="logControl"></param>
        /// <param name="client"></param>
        /// <param name="response"></param>
        private void DisplayLogonResponse(LogControl logControl, Client client, LogonResponse response)
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
                $"UseIntegerPriceOrderMessages: {response.UseIntegerPriceOrderMessages}",
                $"UsesMultiplePositionsPerSymbolAndTradeAccount: {response.UsesMultiplePositionsPerSymbolAndTradeAccount}",
                $"MarketDataSupported: {response.MarketDataSupported}",
                $"ProtocolVersion: {response.ProtocolVersion}",
                $"ReconnectAddress: {response.ReconnectAddress}",
                $"Integer_1: {response.Integer1}"
            };
            logControl.LogMessagesReversed(lines);
        }

        private async void btnDisconnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            var logoffRequest = new Logoff
            {
                DoNotReconnect = 1,
                Reason = "User disconnected"
            };
            if (_client != null)
            {
                _client.SendRequest(DTCMessageType.Logoff, logoffRequest);
                await DisposeClientAsync().ConfigureAwait(false);
            }
        }

        private async void btnExchanges_Click(object sender, EventArgs e)
        {
            // TODO: Change this later to a Client async method that returns the list of ExchangeListResponse objects
            var exchangeListRequest = new ExchangeListRequest
            {
                RequestID = _client.NextRequestId
            };
            logControlSymbols.LogMessage($"Sent exchangeListRequest, RequestID={exchangeListRequest.RequestID}");
            _client.SendRequest(DTCMessageType.ExchangeListRequest, exchangeListRequest);
            if (string.IsNullOrEmpty(_client.LogonResponse.SymbolExchangeDelimiter))
            {
                logControlSymbols.LogMessage("The LogonResponse.SymbolExchangeDelimiter is empty, so Exchanges probably aren't supported.");
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
                $"HasMarketDepthData: {response.HasMarketDepthData}"
            };
            logControlSymbols.LogMessagesReversed(lines);
        }

        private async void btnGetHistoricalTicks_Click(object sender, EventArgs e)
        {
            await RequestHistoricalDataAsync(HistoricalDataIntervalEnum.IntervalTick).ConfigureAwait(false);
        }

        private async void btnGetHistoricalMinutes_Click(object sender, EventArgs e)
        {
            await RequestHistoricalDataAsync(HistoricalDataIntervalEnum.Interval1Minute).ConfigureAwait(false);
        }

        private async Task RequestHistoricalDataAsync(HistoricalDataIntervalEnum recordInterval)
        {
            if (_client == null)
            {
                MessageBox.Show("You must connect first.");
                return;
            }
            _historicalPriceDataRecordResponses = new List<HistoricalPriceDataRecordResponse>();
            var timeoutNoActivity = (int)TimeSpan.FromMinutes(5)
                .TotalMilliseconds; // "several minutes" per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#HistoricalPriceData
            using (var clientHistorical = new Client(txtServer.Text, PortHistorical, timeoutNoActivity))
            {
                const int timeout = 5000;
                try
                {
                    const int heartbeatIntervalInSeconds = 3600;
                    const bool useHeartbeat = false;
                    var clientName = $"HistoricalClient|{txtSymbolHistorical.Text}";

                    // Make a connection
                    var encodingResponse = await clientHistorical.ConnectAsync(EncodingEnum.BinaryEncoding, clientName, timeout).ConfigureAwait(true);
                    if (encodingResponse == null)
                    {
                        // timed out
                        MessageBox.Show("Timed out trying to connect.");
                        return;
                    }
                    var response = await clientHistorical.LogonAsync(heartbeatIntervalInSeconds, useHeartbeat, timeout, txtUsername.Text, txtPassword.Text)
                        .ConfigureAwait(true);
                    if (response == null)
                    {
                        logControlHistorical.LogMessage("Null logon response from logon attempt to " + clientName);
                        return;
                    }
                    switch (response.Result)
                    {
                        case LogonStatusEnum.LogonStatusUnset:
                            throw new ArgumentException("Unexpected logon result");
                        case LogonStatusEnum.LogonSuccess:
                            DisplayLogonResponse(logControlHistorical, clientHistorical, response);
                            break;
                        case LogonStatusEnum.LogonErrorNoReconnect:
                            logControlHistorical.LogMessage(
                                $"{clientHistorical} Login failed: {response.Result} {response.ResultText}. Reconnect not allowed.");
                            return;
                        case LogonStatusEnum.LogonError:
                            logControlHistorical.LogMessage($"{clientHistorical} Login failed: {response.Result} {response.ResultText}.");
                            return;
                        case LogonStatusEnum.LogonReconnectNewAddress:
                            logControlHistorical.LogMessage(
                                $"{clientHistorical} Login failed: {response.Result} {response.ResultText}\nReconnect to: {response.ReconnectAddress}");
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
                var historicalPriceDataReject = await clientHistorical.GetHistoricalPriceDataRecordResponsesAsync(txtSymbolHistorical.Text, "", recordInterval,
                    dtpStart.Value.ToUniversalTime(), DateTime.MinValue, 0U, cbZip.Checked, false, false, HistoricalPriceDataResponseHeaderCallback,
                    HistoricalPriceDataRecordResponseCallback).ConfigureAwait(false);
                if (historicalPriceDataReject != null)
                {
                    logControlHistorical.LogMessage(
                        $"HistoricalPriceDataReject RequestId:{historicalPriceDataReject.RequestID} RejectReasonCode:{historicalPriceDataReject.RejectReasonCode} RejectText:{historicalPriceDataReject.RejectText} RetryTimeInSeconds:{historicalPriceDataReject.RetryTimeInSeconds}");
                }
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
                                                + $"{_historicalPriceDataRecordResponses.Count} records "
                                                + $"from {startTime.ToLocalTime()} through {lastTime.ToLocalTime():yyyyMMdd.HHmmss.fff} (local).");
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
                RegisterClientEventsMarketData(_client);
            }
            logControlLevel1.LogMessage($"Subscribing to market data for {txtSymbolLevel1_1.Text}");
            _symbolId1 = RequireSymbolId(txtSymbolLevel1_1.Text, "");
            _client.SubscribeMarketData(_symbolId1, txtSymbolLevel1_1.Text, "");
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
                UnregisterClientEventsMarketData(_client);
            }
            _ctsLevel1Symbol1?.Cancel();
            _ctsLevel1Symbol1 = null;
            _client.UnsubscribeMarketData(_symbolId1);
        }

        private void btnSubscribeEvents2_Click(object sender, EventArgs e)
        {
            btnUnsubscribe2.Enabled = true;
            btnSubscribeEvents2.Enabled = false;
            if (_numRegistrationsForMarketData++ == 0)
            {
                RegisterClientEventsMarketData(_client);
            }
            logControlLevel1.LogMessage($"Subscribing to market data for {txtSymbolLevel1_2.Text}");
            _symbolId2 = RequireSymbolId(txtSymbolLevel1_2.Text, "");
            _client.SubscribeMarketData(_symbolId2, txtSymbolLevel1_2.Text, "");
        }

        private void btnUnsubscribe2_Click(object sender, EventArgs e)
        {
            btnUnsubscribe2.Enabled = false;
            btnSubscribeEvents2.Enabled = true;
            btnSubscribeCallbacks2.Enabled = true;
            if (--_numRegistrationsForMarketData == 0)
            {
                UnregisterClientEventsMarketData(_client);
            }
            _ctsLevel1Symbol2?.Cancel();
            _ctsLevel1Symbol2 = null;
            _client.UnsubscribeMarketData(_symbolId2);
        }

        private async void btnSubscribeCallbacks1_Click(object sender, EventArgs e)
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
                var reject = await _client.GetMarketDataUpdateTradeCompactAsync(_symbolId1, _ctsLevel1Symbol1.Token, Timeout, symbol, "",
                    MarketDataSnapshotCallback, MarketDataUpdateTradeCompactCallback, MarketDataUpdateBidAskCompactCallback).ConfigureAwait(false);
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

        private async void btnSubscribeCallbacks2_Click(object sender, EventArgs e)
        {
            btnUnsubscribe2.Enabled = true;
            btnSubscribeCallbacks2.Enabled = false;
            _ctsLevel1Symbol2 = new CancellationTokenSource();
            var symbol = txtSymbolLevel1_2.Text;
            _symbolId2 = RequireSymbolId(symbol, "");
            logControlLevel1.LogMessage($"Getting market data for {symbol}");
            try
            {
                var reject = await _client.GetMarketDataUpdateTradeCompactAsync(_symbolId2, _ctsLevel1Symbol2.Token, 5000, symbol, "",
                    MarketDataSnapshotCallback, MarketDataUpdateTradeCompactCallback, MarketDataUpdateBidAskCompactCallback).ConfigureAwait(false);
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
            logControlLevel1.LogMessagesReversed(lines);
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

            Settings1.Default.Save();
        }
    }
}