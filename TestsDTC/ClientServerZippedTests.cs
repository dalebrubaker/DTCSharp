using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DTCClient;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace TestsDTC
{
    public class ClientServerZippedTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public ClientServerZippedTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {
            _output.WriteLine("Disposing");
        }

        private ExampleService StartExampleServer(int timeoutNoActivity, int port, ExampleService exampleService = null)
        {
            var server = new ExampleService(IPAddress.Loopback, port,timeoutNoActivity,10, 20);
            Task.Run(async () => await server.RunAsync().ConfigureAwait(false));
            return server;
        }

        private async Task<Client> ConnectClientAsync(int timeoutNoActivity, int timeoutForConnect, int port,
            EncodingEnum encoding = EncodingEnum.ProtocolBuffers)
        {
            var client = new Client(IPAddress.Loopback.ToString(), port, timeoutNoActivity);
            var encodingResponse = await client.ConnectAsync(encoding, "TestClient" + port, timeoutForConnect).ConfigureAwait(false);
            Assert.Equal(encoding, encodingResponse.Encoding);
            return client;
        }

        /// <summary>
        /// See ClientForm.btnGetHistoricalTicks_Click() for a WinForms example
        /// Also see Client.GetHistoricalPriceDataRecordResponsesAsync() which does something very similar
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HistoricalPriceDataRecordResponseTickZippedTest()
        {
            const int TimeoutNoActivity = 1000; // int.MaxValue; // 1000;
            const int TimeoutForConnect = 1000; // int.MaxValue; // 1000;
            const bool UseZLibCompression = true;
            var isFinalRecordReceived = false;

            // Set up the exampleService responses
            var port = ClientServerTests.NextServerPort;

            using var server = StartExampleServer(TimeoutNoActivity, port);
            using var clientHistorical =
                await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port, EncodingEnum.BinaryEncoding).ConfigureAwait(false);
            var sw = Stopwatch.StartNew();
            while (!clientHistorical.IsConnected) // && sw.ElapsedMilliseconds < 1000)
            {
                // Wait for the client to connect
                await Task.Delay(1).ConfigureAwait(false);
            }
            Assert.Equal(1, server.NumberOfClientHandlers);
            while (server.NumberOfClientHandlersConnected == 0 && sw.ElapsedMilliseconds < 1000)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            Assert.Equal(1, server.NumberOfClientHandlersConnected);

            // Note that heartbeatIntervalInSeconds must be 0 so the server doesn't throw us a heartbeat 
            var loginResponse = await clientHistorical.LogonAsync(0, false, 5000).ConfigureAwait(true);
            Assert.NotNull(loginResponse);

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
                    // Ignore per  https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#HistoricalPriceData
                    numTrades++;
                }
                if (trade.IsFinalRecord != 0)
                {
                    isFinalRecordReceived = true;
                }
            }

            clientHistorical.HistoricalPriceDataRecordResponseEvent += HistoricalPriceDataRecordResponseEvent;

            // Now request the data
            var request = new HistoricalPriceDataRequest
            {
                RequestID = 1,
                Symbol = "ESZ6",
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
            clientHistorical.SendRequest(DTCMessageType.HistoricalPriceDataRequest, request);
            while (!isFinalRecordReceived)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
            var elapsed = sw.ElapsedMilliseconds;
            _output.WriteLine($"Client1 received all {numTrades} historical trades in {elapsed} msecs");

            Assert.Equal(1, numHistoricalPriceDataResponseHeader);
            Assert.Equal(server.NumHistoricalPriceDataRecordsToSend, numTrades);
        }
    }
}