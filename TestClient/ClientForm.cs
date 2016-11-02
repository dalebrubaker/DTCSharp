using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTCClient;
using DTCCommon.Exceptions;
using DTCCommon.Extensions;
using DTCPB;

namespace TestClient
{
    public partial class ClientForm : Form
    {
        private Client _client;
        private uint _symbolId1;
        List<HistoricalPriceDataRecordResponse> _historicalPriceDataRecordResponses;
        private uint _symbolId2;
        private List<MarketDataUpdateTradeCompact> _ticks;
        const int MaxLevel1Rows = 100;
        private int _numRegistrationsForMarketData;
        private CancellationTokenSource _ctsLevel1Symbol1;
        private CancellationTokenSource _ctsLevel1Symbol2;

        public ClientForm()
        {
            InitializeComponent();
            this.Disposed += Form1_Disposed;
            toolStripStatusLabel1.Text = "Disconnected";
            btnUnsubscribe1.Enabled = false;
            btnUnsubscribe2.Enabled = false;
            _ticks = new List<MarketDataUpdateTradeCompact>();
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
            _client = new Client(txtServer.Text, PortListener, callbackToMainThread:true);
            RegisterClientEvents(_client);
            var clientName = "TestClient";
            try
            {
                const int heartbeatIntervalInSeconds = 10;
                const int timeout = 5000;
                const bool useHeartbeat = true;
                var response = await _client.LogonAsync(EncodingEnum.ProtocolBuffers, heartbeatIntervalInSeconds, useHeartbeat, timeout, 
                    clientName, txtUsername.Text, txtPassword.Text).ConfigureAwait(true);
                if (response == null)
                {
                    toolStripStatusLabel1.Text = "Disconnected";
                    logControlConnect.LogMessage("Null logon response from logon attempt to " + clientName);
                    return;
                }
                toolStripStatusLabel1.Text = response.Result == LogonStatusEnum.LogonSuccess ? "Connected" : "Disconnected";
                switch (response.Result)
                {
                    case LogonStatusEnum.LogonStatusUnset:
                        throw new ArgumentException("Unexpected logon result");
                    case LogonStatusEnum.LogonSuccess:
                        DisplayLogonResponse(logControlConnect, _client, response);
                        break;
                    case LogonStatusEnum.LogonErrorNoReconnect:
                        logControlConnect.LogMessage($"{_client} Login failed: {response.Result} {response.ResultText}. Reconnect not allowed.");
                        DisposeClient();
                        break;
                    case LogonStatusEnum.LogonError:
                        logControlConnect.LogMessage($"{_client} Login failed: {response.Result} {response.ResultText}.");
                        DisposeClient();
                        break;
                    case LogonStatusEnum.LogonReconnectNewAddress:
                        logControlConnect.LogMessage($"{_client} Login failed: {response.Result} {response.ResultText}\nReconnect to: {response.ReconnectAddress}");
                        DisposeClient();
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

        private void UnregisterClientEvents(Client client)
        {
            client.EncodingResponseEvent -= Client_EncodingResponseEvent;
            client.UserMessageEvent -= Client_UserMessageEvent;
            client.GeneralLogMessageEvent -= Client_GeneralLogMessageEvent;
            client.ExchangeListResponseEvent -= Client_ExchangeListResponseEvent;
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

        private void Client_MarketDataUpdateSessionLowEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateSessionLow> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data new session low for {combo}: {response.Price}");
        }

        private void Client_MarketDataUpdateSessionHighEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateSessionHigh> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data new session high for {combo}: {response.Price}");
        }

        private void Client_MarketDataUpdateSessionVolumeEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateSessionVolume> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data session volume correction for {combo}: {response.Volume}");
        }

        private void Client_MarketDataUpdateBidAskIntEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateBidAsk_Int> e)
        {
            if (!cbShowBidAsk1.Checked)
            {
                return;
            }
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTime4ByteToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Bid/Ask Int for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime:yyyyMMdd.HHmmss.fff}");
        }

        private void Client_MarketDataUpdateBidAskEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateBidAsk> e)
        {
            if (!cbShowBidAsk1.Checked)
            {
                return;
            }
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTime4ByteToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Bid/Ask for {combo}: BP:{response.BidPrice} BQ:{response.BidQuantity} AP:{response.AskPrice} AQ:{response.AskQuantity} D:{dateTime:yyyyMMdd.HHmmss.fff}");
        }

        private void Client_MarketDataUpdateBidAskCompactEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateBidAskCompact> e)
        {
            MarketDataUpdateBidAskCompactCallback(e.Data);
        }

        private void Client_MarketDataUpdateTradeIntEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateTrade_Int> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTimeWithMillisecondsToUtc().ToLocalTime();
            logControlLevel1.LogMessage(
                $"Market Data Update Trade Int for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime:yyyyMMdd.HHmmss.fff} B/A:{response.AtBidOrAsk}");
        }

        private void Client_MarketDataUpdateTradeEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateTrade> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            var dateTime = response.DateTime.DtcDateTimeWithMillisecondsToUtc().ToLocalTime();
            logControlLevel1.LogMessage($"Market Data Update Trade for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime:yyyyMMdd.HHmmss.fff} B/A:{response.AtBidOrAsk}");
        }

        private void Client_MarketDataUpdateTradeCompactEvent(object sender, DTCCommon.EventArgs<MarketDataUpdateTradeCompact> e)
        {
            MarketDataUpdateTradeCompactCallback(e.Data);
        }

        private void Client_MarketDataFeedSymbolStatusEvent(object sender, DTCCommon.EventArgs<MarketDataFeedSymbolStatus> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market Data Feed status for {combo}: {response.Status}");
        }

        private void Client_MarketDataFeedStatusEvent(object sender, DTCCommon.EventArgs<MarketDataFeedStatus> e)
        {
            var response = e.Data;
            logControlLevel1.LogMessage($"Market Data Feed status: {response.Status}");
        }

        private void Client_MarketDataSnapshotEvent(object sender, DTCCommon.EventArgs<MarketDataSnapshot> e)
        {
            MarketDataSnapshotCallback(e.Data);
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
            logControlLevel1.LogMessagesReversed(lines);
        }

        private void Client_MarketDataRejectEvent(object sender, DTCCommon.EventArgs<MarketDataReject> e)
        {
            var response = e.Data;
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
            logControlLevel1.LogMessage($"Market data request rejected for {combo} because {response.RejectText}");
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
            logControlSymbols.LogMessagesReversed(lines);
        }

        private void Client_GeneralLogMessageEvent(object sender, DTCCommon.EventArgs<GeneralLogMessage> e)
        {
            logControlConnect.LogMessage($"GeneralLogMessage: {e.Data.MessageText}");
        }

        private void Client_UserMessageEvent(object sender, DTCCommon.EventArgs<UserMessage> e)
        {
            logControlConnect.LogMessage($"UserMessage: {e.Data.UserMessage_}");
        }

        private void Client_EncodingResponseEvent(object sender, DTCCommon.EventArgs<DTCPB.EncodingResponse> e)
        {
            var client = (Client)sender;
            var response = e.Data;
            logControlConnect.LogMessage($"{client.ClientName} encoding is {response.Encoding}");
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessages_AuthenticationConnectionMonitoringMessages.php#Messages-LOGON_RESPONSE
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
            logControlSymbols.LogMessage($"Sent exchangeListRequest, RequestID={exchangeListRequest.RequestID}");
            _client.SendMessage(DTCMessageType.ExchangeListRequest, exchangeListRequest);
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
                $"HasMarketDepthData: {response.HasMarketDepthData}",
            };
            logControlSymbols.LogMessagesReversed(lines);
        }

        private async void btnGetHistoricalTicks_Click(object sender, EventArgs e)
        {
            await RequestHistoricalDataAsync(HistoricalDataIntervalEnum.IntervalTick);
        }

        private async void btnGetHistoricalMinutes_Click(object sender, EventArgs e)
        {
            await RequestHistoricalDataAsync(HistoricalDataIntervalEnum.Interval1Minute);
        }

        private async Task RequestHistoricalDataAsync(HistoricalDataIntervalEnum recordInterval)
        {
            _historicalPriceDataRecordResponses = new List<HistoricalPriceDataRecordResponse>();
            using (var client = new Client(txtServer.Text, PortHistorical, callbackToMainThread:false))
            {
                const int timeout = 5000;
                try
                {
                    const int heartbeatIntervalInSeconds = 10;
                    const bool useHeartbeat = false;
                    var clientName = $"HistoricalClient|{txtSymbolHistorical.Text}";
                    var response = await client.LogonAsync(EncodingEnum.ProtocolBuffers, heartbeatIntervalInSeconds, useHeartbeat, timeout, clientName, 
                        txtUsername.Text, txtPassword.Text).ConfigureAwait(true);
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
                            DisplayLogonResponse(logControlHistorical, client, response);
                            break;
                        case LogonStatusEnum.LogonErrorNoReconnect:
                            logControlHistorical.LogMessage($"{client} Login failed: {response.Result} {response.ResultText}. Reconnect not allowed.");
                            return;
                        case LogonStatusEnum.LogonError:
                            logControlHistorical.LogMessage($"{client} Login failed: {response.Result} {response.ResultText}.");
                            return;
                        case LogonStatusEnum.LogonReconnectNewAddress:
                            logControlHistorical.LogMessage($"{client} Login failed: {response.Result} {response.ResultText}\nReconnect to: {response.ReconnectAddress}");
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
                var historicalPriceDataReject = await client.GetHistoricalPriceDataRecordResponsesAsync(timeout, txtSymbolHistorical.Text, "", recordInterval,
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
                logControlHistorical.LogMessage(
                    $"HistoricalPriceDataTickRecordResponse RequestId:{response.RequestID} received {_historicalPriceDataRecordResponses.Count} records through {lastTime.ToLocalTime():yyyyMMdd.HHmmss.fff} (local).");
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
            _symbolId1 = _client.SubscribeMarketData(txtSymbolLevel1_1.Text, "");
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
            _symbolId2 = _client.SubscribeMarketData(txtSymbolLevel1_2.Text, "");
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
            try
            {
                var reject = await _client.GetMarketDataUpdateTradeCompactAsync(_ctsLevel1Symbol1.Token, 5000, symbol, "", MarketDataSnapshotCallback,
                            MarketDataUpdateTradeCompactCallback, MarketDataUpdateBidAskCompactCallback);
                if (reject != null)
                {
                    var message = $"Subscription to {symbol} rejected: {reject.RejectText}";
                    logControlLevel1.LogMessage(message);
                    MessageBox.Show(message);
                }
            }
            catch (TaskCanceledException ex)
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
            try
            {
                var reject = await _client.GetMarketDataUpdateTradeCompactAsync(_ctsLevel1Symbol2.Token, 5000, symbol, "", MarketDataSnapshotCallback,
                            MarketDataUpdateTradeCompactCallback, MarketDataUpdateBidAskCompactCallback);
                if (reject != null)
                {
                    var message = $"Subscription to {symbol} rejected: {reject.RejectText}";
                    logControlLevel1.LogMessage(message);
                    MessageBox.Show(message);
                }
            }
            catch (TaskCanceledException ex)
            {
                // ignore this exception thrown when we unsubscribe
            }
        }

        private void MarketDataSnapshotCallback(MarketDataSnapshot response)
        {
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
            logControlLevel1.LogMessagesReversed(lines);
        }
        private void MarketDataUpdateBidAskCompactCallback(MarketDataUpdateBidAskCompact response)
        {
            if (!cbShowBidAsk1.Checked)
            {
                return;
            }
            var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
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
        /// This event pulls level 1 ticks out of the list and displays them. 
        /// Necessary to avoid overwhelming the UI in a fast market.
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
            if (newTicks.Count > MaxLevel1Rows)
            {
                // If we get a backfill of 1000's of ticks, we don't want to insert them all!
                var count = Math.Min(newTicks.Count, MaxLevel1Rows);
                newTicks = newTicks.GetRange(newTicks.Count - count, count);
                logControlLevel1.LogMessage($"Skipping {MaxLevel1Rows - count} rows due to high incoming volume.");
            }
            var lines = new List<string>(newTicks.Count);
            foreach (var response in newTicks)
            {
                var combo = _client.SymbolExchangeComboBySymbolId[response.SymbolID];
                var dateTime = response.DateTime.DtcDateTime4ByteToUtc().ToLocalTime();
                var line = $"Market Data Update Trade Compact for {combo}: P:{response.Price} V:{response.Volume} D:{dateTime:yyyyMMdd.HHmmss.fff} B/A:{response.AtBidOrAsk}";
                lines.Add(line);
            }
            logControlLevel1.LogMessages(lines);
        }
    }
}
