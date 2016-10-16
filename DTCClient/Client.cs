using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCPB;
using Google.Protobuf;
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public partial class Client : IDisposable
    {
        private readonly string _server;
        private readonly int _port;
        private readonly Timer _heartbeatTimer;
        private bool _isDisposed;
        private CancellationToken _cancellationToken;
        private BinaryReader _binaryReader;
        private BinaryWriter _binaryWriter;
        private TcpClient _tcpClient;
        private const int HeartbeatInterval = 60 * 1000; // 1 minute in milliseconds
        private DateTime _lastHeartbeatReceivedTime;
        private NetworkStream _networkStream;
        private EncodingEnum _currentEncoding;

        public Client(string server, int port)
        {
            _server = server;
            _port = port;
            _heartbeatTimer = new Timer(HeartbeatInterval);
            _heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            _currentEncoding = EncodingEnum.BinaryEncoding; // until we've set it to ProtocolBuffers
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Send a heartbeat to the server
            var heartBeat = new Heartbeat();
            SendMessage(DTCMessageType.Heartbeat, heartBeat.ToByteArray());
        }

        /// <summary>
        /// Make the connection to server at port. 
        /// Start the heartbeats.
        /// Start the listener that will throw events for messages received from the server.
        /// To Disconnect simply Dispose() of this class.
        /// </summary>
        /// <param name="cancellationToken">optional token to stop receiving messages</param>
        /// <returns><c>true</c> if successful. <c>false</c> means protocol buffers are not supported by server</returns>
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _cancellationToken = cancellationToken;
            _tcpClient = new TcpClient();
            _tcpClient.NoDelay = true;
            await _tcpClient.ConnectAsync(_server, _port); // connect to the server
            _networkStream = _tcpClient.GetStream();


            _binaryReader = new BinaryReader(_networkStream);
            _binaryWriter = new BinaryWriter(_networkStream);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            // Fire and forget
            Task.Run(MessageReader, _cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            // Write encoding request with binary encoding
            var size = 2 + 2 + 4 + 4 + 3 + 1; // 12
            _binaryWriter.Write((short)size);
            _binaryWriter.Write((short)DTCMessageType.EncodingRequest); // enum size is 4
            _binaryWriter.Write((int)DTCVersion.CurrentVersion);
            _binaryWriter.Write((int)EncodingEnum.ProtocolBuffers);
            _binaryWriter.Write("DTC"); // 3 chars plus null terminator

            //if (!await IsProtobufSupported())
            //{
            //    return false;
            //}
            _lastHeartbeatReceivedTime = DateTime.Now;
            //_heartbeatTimer.Start();
            return true;
        }

        /// <summary>
        /// Do the up-front binary-encoded request/response per 
        /// </summary>
        /// <returns><c>true</c> if the server can support protobuf encoding</returns>
        private async Task<bool> IsProtobufSupported()
        {
            var size = 2 + 2 + 4 + 4 + 3 + 1; // 12
            _binaryWriter.Write((short)size);
            _binaryWriter.Write((short)DTCMessageType.EncodingRequest); // enum size is 4
            _binaryWriter.Write((int)DTCVersion.CurrentVersion);
            _binaryWriter.Write((int)EncodingEnum.ProtocolBuffers);
            _binaryWriter.Write("DTC"); // 3 chars plus null terminator

            await Task.Delay(60000, _cancellationToken);


            var sizeReceived = _binaryReader.ReadInt16();
            if (sizeReceived != size)
            {
                throw new ArgumentException("Unexpected size mismatch");
            }
            var typeReceived = (DTCMessageType)_binaryReader.ReadInt16();
            if (typeReceived != DTCMessageType.EncodingResponse)
            {
                throw new InvalidDataException("Unexpected message type");
            }
            var bytes = _binaryReader.ReadBytes(size);
            var protocolVersion = BitConverter.ToInt32(bytes, 0); //_binaryReader.ReadInt32();
            var encoding = (EncodingEnum)BitConverter.ToInt32(bytes, 4); //_binaryReader.ReadInt32();
            if (encoding != EncodingEnum.ProtocolBuffers)
            {
                // server can't support it
                return false;
            }
            var protocolType = System.Text.Encoding.Default.GetString(bytes, 8, 3); // BitConverter.ToString(bytes, 8); // _binaryReader.ReadChars(3);
            return true;
        }

        /// <summary>
        /// Logon
        /// </summary>
        /// <param name="heartbeatIntervalInSeconds">The interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="userName">Optional username for the server to authenticate the Client</param>
        /// <param name="password">Optional password for the server to authenticate the Client</param>
        /// <param name="generalTextData">Optional general-purpose text string. For example, this could be used to pass a license key that the Server may require</param>
        /// <param name="integer1">Optional. General-purpose integer</param>
        /// <param name="integer2">Optional. General-purpose integer</param>
        /// <param name="tradeMode">optional to indicate to the Server that the requested trading mode to be one of the following: Demo, Simulated, Live.</param>
        /// <param name="tradeAccount">optional identifier if that is required to login</param>
        /// <param name="hardwareIdentifier">optional computer hardware identifier</param>
        /// <param name="clientName">optional</param>
        /// <returns></returns>
        public async Task LogonAsync(int heartbeatIntervalInSeconds, string clientName = "", string userName = "", string password = "", string generalTextData = "",
            int integer1 = 0, int integer2 = 0,
            TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset, string tradeAccount = "", string hardwareIdentifier = "")
        {
            _heartbeatTimer.Interval = heartbeatIntervalInSeconds * 1000;
            var logonRequest = new LogonRequest
            {
                ClientName = clientName,
                GeneralTextData = generalTextData,
                HardwareIdentifier = hardwareIdentifier,
                HeartbeatIntervalInSeconds = heartbeatIntervalInSeconds,
                Integer1 = integer1,
                Integer2 = integer2,
                Password = password,
                ProtocolVersion = (int)DTCVersion.CurrentVersion,
                TradeAccount = tradeAccount,
                TradeMode = tradeMode
            };

            SendMessage(DTCMessageType.LogonRequest, logonRequest.ToByteArray());

            //// Read the logon response
            //var size = _binaryReader.ReadInt16();
            //var typeReceived = (DTCMessageType)_binaryReader.ReadInt16();
            //if (typeReceived != DTCMessageType.LogonResponse)
            //{
            //    throw new InvalidDataException("Unexpected message type");
            //}
            //var bytes = _binaryReader.ReadBytes(size);
            //var loginResponse = LogonResponse.Parser.ParseFrom(bytes);
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
                _binaryReader.Dispose();
                _binaryWriter.Dispose();
                _tcpClient.Dispose();
                _heartbeatTimer.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Send the message represented by bytes
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        private void SendMessage(DTCMessageType type, byte[] bytes)
        {
            // Write header
            _binaryWriter.Write((short)bytes.Length);
            _binaryWriter.Write((short)type);
            if (bytes.Length > 0)
            {
                _binaryWriter.Write(bytes);
            }
        }

        /// <summary>
        /// This message runs in a continuous loop on its own thread, throwing events as messages are received.
        /// </summary>
        private async Task MessageReader()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (!_networkStream.DataAvailable)
                {
                    await Task.Delay(1, _cancellationToken);
                    continue;
                }

                // Read the header
                int size;
                try
                {
                    size = _binaryReader.ReadInt16();
                }
                catch (Exception ex)
                {
                    
                    throw;
                }
                var type = (DTCMessageType)_binaryReader.ReadInt16();
                var bytes = _binaryReader.ReadBytes(size - 4); // size included the header size+type
                switch (type)
                {
                    case DTCMessageType.MessageTypeUnset:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.LogonRequest:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.LogonResponse:
                        var logonResponse = LogonResponse.Parser.ParseFrom(bytes);
                        var tempLogonReponseEvent = LogonReponseEvent; // for thread safety
                        tempLogonReponseEvent?.Invoke(this, new EventArgs<LogonResponse>(logonResponse));
                        break;
                    case DTCMessageType.Heartbeat:
                        _lastHeartbeatReceivedTime = DateTime.Now;
                        break;
                    case DTCMessageType.Logoff:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.EncodingRequest:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.EncodingResponse:
                        // Note that we must use binary encoding here, per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                        if (_currentEncoding == EncodingEnum.BinaryEncoding)
                        {
                            var protocolVersion = BitConverter.ToInt32(bytes, 0); //_binaryReader.ReadInt32();
                            _currentEncoding = (EncodingEnum)BitConverter.ToInt32(bytes, 4); //_binaryReader.ReadInt32();
                            if (_currentEncoding != EncodingEnum.ProtocolBuffers)
                            {
                                // server can't support it
                                throw new NotImplementedException();
                            }
                            var protocolType = System.Text.Encoding.Default.GetString(bytes, 8, 3); // BitConverter.ToString(bytes, 8); // _binaryReader.ReadChars(3);

                        }
                        throw new NotImplementedException("Currently we only do at Connect, using binary encoding");
                        //var encodingReponse = EncodingResponse.Parser.ParseFrom(bytes);
                        //var tempEncodingReponseEvent = EncodingReponseEvent; // for thread safety
                        //tempEncodingReponseEvent?.Invoke(this, new EventArgs<EncodingResponse>(encodingReponse));
                        //break;
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        //public event EventHandler<EventArgs<EncodingResponse>> EncodingReponseEvent;
        public event EventHandler<EventArgs<LogonResponse>> LogonReponseEvent;

    }

    // from http://stackoverflow.com/questions/20694062/can-a-tcp-c-sharp-client-receive-and-send-continuously-consecutively-without-sle

//    class Receiver
//    {
//        internal event EventHandler<DataReceivedEventArgs> DataReceived;

//        internal Receiver(NetworkStream stream)
//        {
//            _stream = stream;
//            _thread = new Thread(Run);
//            _thread.Start();
//        }

//        private void Run()
//        {
//            try
//            {
//                // ShutdownEvent is a ManualResetEvent signaled by
//                // Client when its time to close the socket.
//                while (!ShutdownEvent.WaitOne(0))
//                {
//                    try
//                    {
//                        // We could use the ReadTimeout property and let Read()
//                        // block.  However, if no data is received prior to the
//                        // timeout period expiring, an IOException occurs.
//                        // While this can be handled, it leads to problems when
//                        // debugging if we are wanting to break when exceptions
//                        // are thrown (unless we explicitly ignore IOException,
//                        // which I always forget to do).
//                        if (!_stream.DataAvailable)
//                        {
//                            // Give up the remaining time slice.
//                            Thread.Sleep(1);
//                        }
//                        else if (_stream.Read(_data, 0, _data.Length) > 0)
//                        {
//                            // Raise the DataReceived event w/ data...
//                        }
//                        else
//                        {
//                            // The connection has closed gracefully, so stop the
//                            // thread.
//                            ShutdownEvent.Set();
//                        }
//                    }
//                    catch (IOException ex)
//                    {
//                        // Handle the exception...
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                // Handle the exception...
//            }
//            finally
//            {
//                _stream.Close();
//            }
//        }

//        private NetworkStream _stream;
//        private Thread _thread;
//    }


//    class Sender
//    {
//        internal void SendData(byte[] data)
//        {
//            // transition the data to the thread and send it...
//        }

//        internal Sender(NetworkStream stream)
//        {
//            _stream = stream;
//            _thread = new Thread(Run);
//            _thread.Start();
//        }

//        private void Run()
//        {
//            // main thread loop for sending data...
//        }

//        private NetworkStream _stream;
//        private Thread _thread;
//    }
}
