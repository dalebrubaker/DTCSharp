using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DTCClient;
using DTCCommon;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    class ClientServerZippedTests : IDisposable
    {

        private readonly ITestOutputHelper _output;
        private int _nextServerPort;

        public ClientServerZippedTests(ITestOutputHelper output)
        {
            _output = output;
            _nextServerPort = 54321;
        }

        public void Dispose()
        {
            _output.WriteLine("Disposing");
        }


        private Server StartExampleServer(int timeoutNoActivity, int port, ExampleService exampleService = null)
        {
            if (exampleService == null)
            {
                exampleService = new ExampleService();
            }
            var server = new Server(exampleService.HandleRequest, IPAddress.Loopback, port: port, timeoutNoActivity: timeoutNoActivity);
            TaskHelper.RunBg(async () => await server.RunAsync().ConfigureAwait(false));
            return server;
        }

        private async Task<Client> ConnectClientAsync(int timeoutNoActivity, int timeoutForConnect, int port, EncodingEnum encoding = EncodingEnum.ProtocolBuffers)
        {
            var client = new Client(IPAddress.Loopback.ToString(), serverPort: port, timeoutNoActivity: timeoutNoActivity);
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
            const int timeoutNoActivity = 10000;
            const int timeoutForConnect = 10000;
            const bool useZLibCompression = true;
            var isFinalRecordReceived = false;

            // Set up the exampleService responses
            var exampleService = new ExampleService();
            var port = _nextServerPort++;

            using (var server = StartExampleServer(timeoutNoActivity, port, exampleService))
            {
                using (var clientHistorical = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port, EncodingEnum.BinaryEncoding).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (!clientHistorical.IsConnected) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await clientHistorical.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: false, timeout: 5000).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

                    var numHistoricalPriceDataResponseHeader = 0;
                    var numTrades = 0;

                    // Set up the handler to capture the HistoricalPriceDataResponseHeader event
                    EventHandler<EventArgs<HistoricalPriceDataResponseHeader>> responseHeaderEvent = (s, e) =>
                    {
                        var header = e.Data;
                        _output.WriteLine($"Client1 received a HistoricalPriceDataResponseHeader after {sw.ElapsedMilliseconds} msecs");
                        numHistoricalPriceDataResponseHeader++;
                    };
                    clientHistorical.HistoricalPriceDataResponseHeaderEvent += responseHeaderEvent;

                    // Set up the handler to capture the HistoricalPriceDataRecordResponse events
                    EventHandler<EventArgs<HistoricalPriceDataRecordResponse>> historicalPriceDataRecordResponseEvent = (s, e) =>
                    {
                        var trade = e.Data;
                        numTrades++;
                        if (trade.IsFinalRecord != 0)
                        {
                            isFinalRecordReceived = true;
                        }
                    };
                    clientHistorical.HistoricalPriceDataRecordResponseEvent += historicalPriceDataRecordResponseEvent;

                    // Now request the data
                    var request = new HistoricalPriceDataRequest
                    {
                        RequestID = 1,
                        Symbol = "ESZ6",
                        Exchange = "",
                        RecordInterval = HistoricalDataIntervalEnum.IntervalTick,
                        StartDateTime = DateTime.UtcNow.UtcToDtcDateTime(), // ignored in this test
                        EndDateTime = DateTime.UtcNow.UtcToDtcDateTime(), // ignored in this test
                        MaxDaysToReturn = 1, // ignored in this test
                        UseZLibCompression = useZLibCompression ? 1 : 0,
                        RequestDividendAdjustedStockData = 0,
                        Flag1 = 0,
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
                    Assert.Equal(exampleService.NumHistoricalPriceDataRecordsToSend, numTrades);
                }
            }
        }

    }
}
