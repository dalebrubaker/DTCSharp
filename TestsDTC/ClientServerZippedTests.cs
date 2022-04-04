using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using DTCClient;
using DTCCommon;
using DTCPB;
using FluentAssertions;
using NLog;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace TestsDTC
{
    public class ClientServerZippedTests : IDisposable
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

        private readonly ITestOutputHelper _output;

        public ClientServerZippedTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {
            _output.WriteLine("Disposing");
        }

        private ClientDTC ConnectClientHistorical(int port, EncodingEnum encoding = EncodingEnum.ProtocolBuffers)
        {
            var client = new ClientDTC("localhost", port);
            var (loginResponse, error) = client.Logon("TestClient", requestedEncoding: encoding);
            Assert.NotNull(loginResponse);
            return client;
        }

        /// <summary>
        /// See ClientForm.btnGetHistoricalTicks_Click() for a WinForms example
        /// Also see Client.GetHistoricalPriceDataRecordResponsesAsync() which does something very similar
        /// Shows getting zipped records with Protocol Buffers, then continuing as a normal client, then getting zipped again and back to normal
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HistoricalPriceDataRecordResponseTickZippedTest()
        {
            const bool UseZLibCompression = true;
            var isFinalRecordReceived = false;

            // Set up the exampleService responses
            var port = ClientServerTests.NextServerPort;
            var server = new ExampleService(IPAddress.Loopback, port, 10, 20);

            // SierraChart supports compression only with BinaryEncoding, but we support it also with EncodingEnum.ProtocolBuffers
            using var clientHistorical = ConnectClientHistorical(port);
            Assert.NotNull(clientHistorical);
            s_logger.ConditionalDebug($"Started zip ExampleService and client on port={port}");

            var sw = Stopwatch.StartNew();
            Assert.Equal(1, server.NumberOfClientHandlers);
            Assert.Equal(1, server.NumberOfClientHandlersConnected);

            var numHistoricalPriceDataResponseHeader = 0;
            var numTrades = 0;

            // Set up the handler to capture the HistoricalPriceDataResponseHeader event
            void ResponseHeaderEvent(object s, HistoricalPriceDataResponseHeader header)
            {
                _output.WriteLine($"Client1 received a HistoricalPriceDataResponseHeader after {sw.ElapsedMilliseconds} msecs");
                numHistoricalPriceDataResponseHeader++;
            }

            clientHistorical.HistoricalPriceDataResponseHeaderEvent += ResponseHeaderEvent;

            // Set up the handler to capture the HistoricalPriceDataRecordResponse events
            void HistoricalPriceDataRecordResponseEvent(object s, HistoricalPriceDataRecordResponse trade)
            {
                if (trade.StartDateTime != 0)
                {
                    // Ignore record with 0 per  https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#HistoricalPriceData
                    numTrades++;
                }
                if (trade.IsFinalRecord != 0)
                {
                    isFinalRecordReceived = true;
                }
            }

            clientHistorical.HistoricalPriceDataRecordResponseEvent += HistoricalPriceDataRecordResponseEvent;
            const string Symbol = "ESZ16";
            var (securityDefinitionResponse, result) = clientHistorical.GetSecurityDefinition(Symbol, "");
            result.IsError.Should().BeFalse();
            securityDefinitionResponse.MinPriceIncrement.Should().Be((float)0.25);

            // Now request the data
            var historicalPriceDataRequest1 = new HistoricalPriceDataRequest
            {
                RequestID = 1,
                Symbol = Symbol,
                Exchange = "",
                RecordInterval = HistoricalDataIntervalEnum.IntervalTick,
                StartDateTime = DateTime.MinValue.UtcToDtcDateTime(),
                EndDateTime = DateTime.MaxValue.UtcToDtcDateTime(),
                MaxDaysToReturn = 1, // ignored in this test
                UseZLibCompression = UseZLibCompression ? 1 : 0,
                RequestDividendAdjustedStockData = 0,
                Integer1 = 0,
            };
            sw.Restart();
            clientHistorical.SendRequest(DTCMessageType.HistoricalPriceDataRequest, historicalPriceDataRequest1);
            if (!isFinalRecordReceived)
            {
                _output.WriteLine("Waiting for isFinalRecordReceived");
                while (!isFinalRecordReceived)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
                _output.WriteLine("Done waiting for isFinalRecordReceived");
            }
            var elapsed = sw.ElapsedMilliseconds;
            _output.WriteLine($"clientHistorical received all {numTrades:N0} historical trades in {elapsed} msecs");
            Assert.Equal(1, numHistoricalPriceDataResponseHeader);
            Assert.Equal(server.NumHistoricalPriceDataRecordsToSend, numTrades);

            // Now do another security definition request to prove clientHistorical still works
            var (securityDefinitionResponse2, result2) = clientHistorical.GetSecurityDefinition(Symbol, "");
            result2.IsError.Should().BeFalse();
            securityDefinitionResponse2.MinPriceIncrement.Should().Be((float)0.25);

            // And do another batch of zipped historical records to prove we can do that again
            isFinalRecordReceived = false;
            s_logger.ConditionalDebug("Starting to request a second batch of historical records.");
            clientHistorical.SendRequest(DTCMessageType.HistoricalPriceDataRequest, historicalPriceDataRequest1);
            if (!isFinalRecordReceived)
            {
                _output.WriteLine("Waiting for isFinalRecordReceived");
                while (!isFinalRecordReceived)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
                _output.WriteLine("Done waiting for isFinalRecordReceived");
            }
            Assert.Equal(2, numHistoricalPriceDataResponseHeader);
            Assert.Equal(server.NumHistoricalPriceDataRecordsToSend * 2, numTrades);
        }
    }
}