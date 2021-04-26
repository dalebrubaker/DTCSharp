using System;
using System.IO;
using System.Threading;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Extensions;
using DTCPB;
using Google.Protobuf;
using Xunit;

namespace TestsDTC
{
    public class BinaryEncodingTests : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine("Disposing");
        }

        private void GenericTest<T>(DTCMessageType messageType, T message) where T : IMessage<T>, new()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            var codec = new CodecBinary(ms);
            codec.WriteAsync(messageType, message, CancellationToken.None);
            var bytes = ms.ToArray();
            Utility.ReadHeader(bytes, out var sizeExcludingHeader, out var messageTypeHeader);
            Assert.Equal(messageType, messageTypeHeader);
            var messageBytes = new byte[sizeExcludingHeader];
            Array.Copy(bytes, 4, messageBytes, 0, sizeExcludingHeader);
            var loadedMessage = codec.Load<T>(messageType, messageBytes);
            if (!loadedMessage.Equals(message))
            {
                //var debug = 1;
            }
            Assert.Equal(message, loadedMessage);
        }

        [Fact]
        public void HeartbeatTest()
        {
            var now = DateTime.UtcNow;
            var heartbeat = new Heartbeat
            {
                CurrentDateTime = now.UtcToDtcDateTime(),
                NumDroppedMessages = 29
            };
            GenericTest(DTCMessageType.Heartbeat, heartbeat);
        }

        [Fact]
        public void EncodingRequestTest()
        {
            var encodingRequest = new EncodingRequest
            {
                ProtocolVersion = 1,
                Encoding = EncodingEnum.ProtocolBuffers,
                ProtocolType = "test"
            };
            GenericTest(DTCMessageType.EncodingRequest, encodingRequest);
        }

        [Fact]
        public void EncodingResponseTest()
        {
            var encodingResponse = new EncodingResponse
            {
                ProtocolVersion = 1,
                Encoding = EncodingEnum.ProtocolBuffers,
                ProtocolType = "test"
            };
            GenericTest(DTCMessageType.EncodingResponse, encodingResponse);
        }

        [Fact]
        public void LogonRequestTest()
        {
            var logonRequest = new LogonRequest
            {
                ProtocolVersion = 1,
                Username = "username",
                Password = "pw",
                GeneralTextData = "gtd",
                Integer1 = 1,
                Integer2 = 2,
                HeartbeatIntervalInSeconds = 10,
                TradeMode = TradeModeEnum.TradeModeDemo,
                TradeAccount = "demo",
                HardwareIdentifier = "hwi",
                ClientName = "testClient"
            };
            GenericTest(DTCMessageType.LogonRequest, logonRequest);
        }

        [Fact]
        public void LogonResponseTest()
        {
            var logonResponse = new LogonResponse
            {
                ProtocolVersion = 1,
                Result = LogonStatusEnum.LogonSuccess,
                ResultText = "success",
                ReconnectAddress = "",
                Integer1 = 1,
                ServerName = "serverName",
                MarketDepthUpdatesBestBidAndAsk = 7U,
                TradingIsSupported = 8U,
                OCOOrdersSupported = 9U,
                OrderCancelReplaceSupported = 10U,
                SymbolExchangeDelimiter = "|",
                SecurityDefinitionsSupported = 12U,
                HistoricalPriceDataSupported = 13U,
                ResubscribeWhenMarketDataFeedAvailable = 14U,
                MarketDepthIsSupported = 15U,
                OneHistoricalPriceDataRequestPerConnection = 16U,
                BracketOrdersSupported = 17U,
                UseIntegerPriceOrderMessages = 18U,
                UsesMultiplePositionsPerSymbolAndTradeAccount = 19U,
                MarketDataSupported = 20U,
            };
            GenericTest(DTCMessageType.LogonResponse, logonResponse);
        }

        [Fact]
        public void LogoffTest()
        {
            var logoff = new Logoff
            {
                Reason = "test",
                DoNotReconnect = 1U
            };
            GenericTest(DTCMessageType.Logoff, logoff);
        }

        [Fact]
        public void ExchangeListRequestTest()
        {
            var exchangeListRequest = new ExchangeListRequest
            {
                RequestID = 1,
            };
            GenericTest(DTCMessageType.ExchangeListRequest, exchangeListRequest);
        }

        [Fact]
        public void SecurityDefinitionForSymbolRequestTest()
        {
            var securityDefinitionForSymbolRequest = new SecurityDefinitionForSymbolRequest
            {
                RequestID = 1,
                Symbol = "ESZ6",
                Exchange = "cme",
            };
            GenericTest(DTCMessageType.SecurityDefinitionForSymbolRequest, securityDefinitionForSymbolRequest);
        }

        [Fact]
        public void SecurityDefinitionResponseTest()
        {
            var securityDefinitionResponse = new SecurityDefinitionResponse
            {
                RequestID = 1,
                Symbol = "ESZ6",
                Exchange = "cme",
                SecurityType = SecurityTypeEnum.SecurityTypeFuture,
                Description = "desc",
                MinPriceIncrement = 0.25f,
                PriceDisplayFormat = PriceDisplayFormatEnum.PriceDisplayFormatDecimal2,
                CurrencyValuePerIncrement = 12.5f,
                IsFinalMessage = 1U,
                FloatToIntPriceMultiplier = 12.5f,
                IntToFloatPriceDivisor = 1.0f / 12.5f,
                UnderlyingSymbol = "ES",
                UpdatesBidAskOnly = 0U,
                StrikePrice = 0f,
                PutOrCall = PutCallEnum.PcUnset,
                ShortInterest = 49,
                SecurityExpirationDate = DateTime.UtcNow.UtcToDtcDateTime4Byte(),
                BuyRolloverInterest = 2,
                SellRolloverInterest = 3,
                EarningsPerShare = 5f,
                SharesOutstanding = 10,
                IntToFloatQuantityDivisor = 1,
                HasMarketDepthData = 1,
                DisplayPriceMultiplier = 1,
                ExchangeSymbol = "ESxxx"
            };
            GenericTest(DTCMessageType.SecurityDefinitionResponse, securityDefinitionResponse);
        }

        [Fact]
        public void SecurityDefinitionRejectTest()
        {
            var securityDefinitionReject = new SecurityDefinitionReject
            {
                RequestID = 1,
                RejectText = "shucks, no",
            };
            GenericTest(DTCMessageType.SecurityDefinitionReject, securityDefinitionReject);
        }

        [Fact]
        public void HistoricalPriceDataRequestTest()
        {
            var historicalPriceDataRequest = new HistoricalPriceDataRequest
            {
                RequestID = 1,
                Symbol = "ESZ6",
                Exchange = "cme",
                RecordInterval = HistoricalDataIntervalEnum.IntervalTick,
                StartDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                EndDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                MaxDaysToReturn = 7U,
                UseZLibCompression = 8u,
                RequestDividendAdjustedStockData = 9u,
                Integer1 = 10u,
            };
            GenericTest(DTCMessageType.HistoricalPriceDataRequest, historicalPriceDataRequest);
        }

        [Fact]
        public void HistoricalPriceDataResponseHeaderTest()
        {
            var historicalPriceDataResponseHeader = new HistoricalPriceDataResponseHeader
            {
                RequestID = 1,
                RecordInterval = HistoricalDataIntervalEnum.IntervalTick,
                UseZLibCompression = 8u,
                NoRecordsToReturn = 4u,
                IntToFloatPriceDivisor = 1,
            };
            GenericTest(DTCMessageType.HistoricalPriceDataResponseHeader, historicalPriceDataResponseHeader);
        }

        [Fact]
        public void HistoricalPriceDataRejectTest()
        {
            var historicalPriceDataReject = new HistoricalPriceDataReject
            {
                RequestID = 1,
                RejectText = "shucks, no",
                RejectReasonCode = HistoricalPriceDataRejectReasonCodeEnum.HpdrGeneralRejectError,
                RetryTimeInSeconds = 4u,
            };
            GenericTest(DTCMessageType.HistoricalPriceDataReject, historicalPriceDataReject);
        }

        [Fact]
        public void HistoricalPriceDataRecordResponseTest()
        {
            var historicalPriceDataRecordResponse = new HistoricalPriceDataRecordResponse
            {
                RequestID = 1,
                StartDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                OpenPrice = 3,
                HighPrice = 4,
                LowPrice = 5,
                LastPrice = 6,
                Volume = 7,
                NumTrades = 8,
                BidVolume = 9,
                AskVolume = 10,
                IsFinalRecord = 11,
            };
            GenericTest(DTCMessageType.HistoricalPriceDataRecordResponse, historicalPriceDataRecordResponse);
        }
    }
}