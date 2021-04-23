﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using DTCClient;
using DTCCommon.EventArgsF;
using DTCCommon.Exceptions;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using NLog;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class ClientServerNotZippedTests : IDisposable
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        private readonly ITestOutputHelper _output;

        public ClientServerNotZippedTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {
            //_output.WriteLine("Disposing");
        }

        private Server StartExampleServer(int timeoutNoActivity, int port, ExampleService exampleService = null)
        {
            if (exampleService == null)
            {
                exampleService = new ExampleService();
            }
            var server = new Server(exampleService.HandleRequest, IPAddress.Loopback, port, timeoutNoActivity);
            Task.Run(async () => await server.RunAsync().ConfigureAwait(false));
            return server;
        }

        private async Task<Client> ConnectClientAsync(int timeoutNoActivity, int timeoutForConnect, int port,
            EncodingEnum encoding = EncodingEnum.ProtocolBuffers)
        {
            var client = new Client(IPAddress.Loopback.ToString(), port, timeoutNoActivity);
            var encodingResponse = await client.ConnectAsync(encoding, "TestClient" + port, timeoutForConnect).ConfigureAwait(false);
            if (encodingResponse == null)
            {
                throw new DTCSharpException("Encoding response is null");
            }
            return client;
        }

        /// <summary>
        /// See ClientForm.btnGetHistoricalTicks_Click() for a WinForms example
        /// Also see Client.GetHistoricalPriceDataRecordResponsesAsync() which does something very similar
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HistoricalPriceDataRecordResponseTickNotZippedTest()
        {
            const int TimeoutNoActivity = 10000;
            const int TimeoutForConnect = 10000;
            const bool UseZLibCompression = false;
            var isFinalRecordReceived = false;
            var sw = Stopwatch.StartNew();

            // Set up the exampleService responses
            var exampleService = new ExampleService();
            var port = ClientServerTests.NextServerPort;

            using (var server = StartExampleServer(TimeoutNoActivity, port, exampleService))
            {
                using (var clientHistorical = await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port, EncodingEnum.BinaryEncoding)
                    .ConfigureAwait(false))
                {
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
                    var loginResponse = await clientHistorical.LogonAsync(0, false, TimeoutForConnect).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

                    var numHistoricalPriceDataResponseHeader = 0;
                    var numTrades = 0;

                    // Set up the handler to capture the HistoricalPriceDataResponseHeader event
                    void ResponseHeaderEvent(object s, EventArgs<HistoricalPriceDataResponseHeader> e)
                    {
                        var header = e.Data;
                        _output.WriteLine($"Client1 received a HistoricalPriceDataResponseHeader after {sw.ElapsedMilliseconds} msecs");
                        numHistoricalPriceDataResponseHeader++;
                    }

                    clientHistorical.HistoricalPriceDataResponseHeaderEvent += ResponseHeaderEvent;

                    // Set up the handler to capture the HistoricalPriceDataRecordResponse events
                    void HistoricalPriceDataRecordResponseEvent(object s, EventArgs<HistoricalPriceDataRecordResponse> e)
                    {
                        var trade = e.Data;
                        numTrades++;
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
                    var endDateTime = request.EndDateTimeUtc;
                    sw.Restart();
                    clientHistorical.SendRequest(DTCMessageType.HistoricalPriceDataRequest, request);
                    while (!isFinalRecordReceived)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    var elapsed = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Client1 received all {numTrades} historical trades in {elapsed} msecs");

                    Assert.Equal(1, numHistoricalPriceDataResponseHeader);
                    Assert.Equal(exampleService.NumHistoricalPriceDataRecordsToSend + 1, numTrades); // Add 1 for the 0's FinalRecord
                }
            }
        }
    }
}