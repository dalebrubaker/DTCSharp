using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Exceptions;
using DTCPB;
using Google.Protobuf;
using Timer = System.Timers.Timer;

namespace DTCServer
{
    public class ClientHandler : IDisposable
    {
        private readonly IServerStub _serverStub;
        private readonly TcpClient _tcpClient;
        private readonly bool _useHeartbeat;
        private bool _isDisposed;
        private readonly Timer _heartbeatTimer;
        private readonly string _remoteEndPoint;
        private readonly string _localEndPoint;
        private ICodecDTC _currentCodec;
        private NetworkStream _networkStream;
        private BinaryWriter _binaryWriter;
        private ConcurrentQueue<MessageWithType<IMessage>> _responsesQueue;

        public ClientHandler(IServerStub serverStub, TcpClient tcpClient, bool useHeartbeat)
        {
            _serverStub = serverStub;
            _tcpClient = tcpClient;
            _useHeartbeat = useHeartbeat;
            _networkStream = _tcpClient.GetStream();
            _binaryWriter = new BinaryWriter(_networkStream);
            _currentCodec = new CodecBinary();
            _heartbeatTimer = new Timer(10000);
            _remoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
            _localEndPoint = tcpClient.Client.LocalEndPoint.ToString();

        }

        /// <summary>
        /// This method runs "forever" on its own thread.
        /// All responses are sent on this thread
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ResponseLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MessageWithType<IMessage> messageWithType;
                if (_responsesQueue.TryDequeue(out messageWithType))
                {
                    try
                    {
                        _currentCodec.Write(messageWithType.MessageType, messageWithType.Message, _binaryWriter);
                    }
                    catch (Exception ex)
                    {
                        throw new DTCSharpException(ex.Message);
                    }
                }
                await Task.Delay(1, cancellationToken);
            }
        }

        /// <summary>
        /// This method runs "forever".
        /// All requests are received on this thread
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Run(CancellationToken cancellationToken)
        {
#pragma warning disable 4014
            Task.Run(() => ResponseLoop(cancellationToken), cancellationToken);
#pragma warning restore 4014
            var binaryReader = new BinaryReader(_networkStream); // Note that binaryReader may be redefined below in HistoricalPriceDataResponseHeader
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_networkStream.DataAvailable)
                {
                    await Task.Delay(1, cancellationToken);
                    continue;
                }

                // Read the header
                var size = binaryReader.ReadUInt16();
                var messageType = (DTCMessageType)binaryReader.ReadUInt16();
#if DEBUG
                if (messageType != DTCMessageType.Heartbeat)
                {
                    var debug = 1;
                }
#endif
                var bytes = binaryReader.ReadBytes(size - 4); // size included the header size+type
                switch (messageType)
                {
                    case DTCMessageType.LogonRequest:
                        var logonRequest = _currentCodec.Load<LogonRequest>(messageType, bytes);
                        var logonResponse = await _serverStub.LogonRequest(logonRequest);
                        _responsesQueue.Enqueue(new MessageWithType<IMessage>(DTCMessageType.LogonResponse, logonResponse));
                        break;
                    case DTCMessageType.Heartbeat:
                        break;
                    case DTCMessageType.Logoff:
                        break;
                    case DTCMessageType.EncodingRequest:
                        break;
                    case DTCMessageType.EncodingResponse:
                        break;
                    case DTCMessageType.MarketDataRequest:
                        break;
                    case DTCMessageType.MarketDataReject:
                        break;
                    case DTCMessageType.MarketDataSnapshot:
                        break;
                    case DTCMessageType.MarketDataSnapshotInt:
                        break;
                    case DTCMessageType.MarketDataUpdateTrade:
                        break;
                    case DTCMessageType.MarketDataUpdateTradeCompact:
                        break;
                    case DTCMessageType.MarketDataUpdateTradeInt:
                        break;
                    case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAsk:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAskCompact:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAskInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionOpen:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionOpenInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionHigh:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionHighInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionLow:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionLowInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionVolume:
                        break;
                    case DTCMessageType.MarketDataUpdateOpenInterest:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionSettlement:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionNumTrades:
                        break;
                    case DTCMessageType.MarketDataUpdateTradingSessionDate:
                        break;
                    case DTCMessageType.MarketDepthRequest:
                        break;
                    case DTCMessageType.MarketDepthReject:
                        break;
                    case DTCMessageType.MarketDepthSnapshotLevel:
                        break;
                    case DTCMessageType.MarketDepthSnapshotLevelInt:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevel:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevelCompact:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevelInt:
                        break;
                    case DTCMessageType.MarketDepthFullUpdate10:
                        break;
                    case DTCMessageType.MarketDepthFullUpdate20:
                        break;
                    case DTCMessageType.MarketDataFeedStatus:
                        break;
                    case DTCMessageType.MarketDataFeedSymbolStatus:
                        break;
                    case DTCMessageType.SubmitNewSingleOrder:
                        break;
                    case DTCMessageType.SubmitNewSingleOrderInt:
                        break;
                    case DTCMessageType.SubmitNewOcoOrder:
                        break;
                    case DTCMessageType.SubmitNewOcoOrderInt:
                        break;
                    case DTCMessageType.CancelOrder:
                        break;
                    case DTCMessageType.CancelReplaceOrder:
                        break;
                    case DTCMessageType.CancelReplaceOrderInt:
                        break;
                    case DTCMessageType.OpenOrdersRequest:
                        break;
                    case DTCMessageType.OpenOrdersReject:
                        break;
                    case DTCMessageType.OrderUpdate:
                        break;
                    case DTCMessageType.HistoricalOrderFillsRequest:
                        break;
                    case DTCMessageType.HistoricalOrderFillResponse:
                        break;
                    case DTCMessageType.CurrentPositionsRequest:
                        break;
                    case DTCMessageType.CurrentPositionsReject:
                        break;
                    case DTCMessageType.PositionUpdate:
                        break;
                    case DTCMessageType.TradeAccountsRequest:
                        break;
                    case DTCMessageType.TradeAccountResponse:
                        break;
                    case DTCMessageType.ExchangeListRequest:
                        break;
                    case DTCMessageType.ExchangeListResponse:
                        break;
                    case DTCMessageType.SymbolsForExchangeRequest:
                        break;
                    case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                        break;
                    case DTCMessageType.SymbolsForUnderlyingRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionForSymbolRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionResponse:
                        break;
                    case DTCMessageType.SymbolSearchRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionReject:
                        break;
                    case DTCMessageType.AccountBalanceRequest:
                        break;
                    case DTCMessageType.AccountBalanceReject:
                        break;
                    case DTCMessageType.AccountBalanceUpdate:
                        break;
                    case DTCMessageType.UserMessage:
                        break;
                    case DTCMessageType.GeneralLogMessage:
                        break;
                    case DTCMessageType.HistoricalPriceDataRequest:
                        break;
                    case DTCMessageType.HistoricalPriceDataResponseHeader:
                        break;
                    case DTCMessageType.HistoricalPriceDataReject:
                        break;
                    case DTCMessageType.HistoricalPriceDataRecordResponse:
                        break;
                    case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                        break;
                    case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                        break;
                    case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                        break;
                    case DTCMessageType.MessageTypeUnset:
                    case DTCMessageType.LogonResponse:
                    default:
                        throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(Run)}.");
                }
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _heartbeatTimer?.Dispose();
                _isDisposed = true;
            }
        }

        public override string ToString()
        {
            return $"Local:{_localEndPoint} Remote:{_remoteEndPoint}";
        }
    }
}
