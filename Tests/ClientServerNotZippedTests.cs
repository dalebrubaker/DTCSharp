using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using DTCClient;
using DTCCommon;
using DTCPB;
using DTCServer;
using FluentAssertions;
using Google.Protobuf;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

[Collection("Logging collection")]
public class ClientServerNotZippedTests : IDisposable
{
    private readonly TestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ClientServerNotZippedTests(TestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public void Dispose()
    {
        //_output.WriteLine("Disposing");
    }

    private ExampleService StartExampleServer(int port)
    {
        var server = new ExampleService(IPAddress.Loopback, port, 10, 20);
        server.StartServer();
        return server;
    }

    /// <summary>
    ///     See ClientForm.btnGetHistoricalTicks_Click() for a WinForms example
    ///     Also see Client.GetHistoricalPriceDataRecordResponsesAsync() which does something very similar
    /// </summary>
    /// <returns></returns>
    [Fact]
    public void HistoricalPriceDataRecordResponseTickNotZippedTest()
    {
        const bool UseZLibCompression = false;
        var signal = new ManualResetEvent(false);
        var sw = Stopwatch.StartNew();

        // Set up the exampleService responses
        var port = ClientServerTests.NextServerPort;
        using var exampleService = StartExampleServer(port);
        using var clientHistorical = new ClientDTC();
        clientHistorical.StartClient("localhost", port);
        var (loginResponse, error) = clientHistorical.Logon("TestClient", 1);
        Assert.NotNull(loginResponse);
        Assert.Equal(1, exampleService.NumberOfClientHandlers);
        Assert.Equal(1, exampleService.NumberOfClientHandlersConnected);

        var numHistoricalPriceDataResponseHeader = 0;
        var numTrades = 0;

        // Set up the handler to capture the HistoricalPriceDataResponseHeader event
        void ClientHistoricalOnHistoricalPriceDataResponseHeaderEvent(object sender, HistoricalPriceDataResponseHeader e)
        {
            _output.WriteLine($"Client1 received a HistoricalPriceDataResponseHeader after {sw.ElapsedMilliseconds} msecs");
            numHistoricalPriceDataResponseHeader++;
        }

        clientHistorical.HistoricalPriceDataResponseHeaderEvent += ClientHistoricalOnHistoricalPriceDataResponseHeaderEvent!;

        // Set up the handler to capture the HistoricalPriceDataRecordResponse events
        void ClientHistoricalOnHistoricalPriceDataResponseEvent(object sender, HistoricalPriceDataRecordResponse trade)
        {
            if (trade.StartDateTime != 0)
            {
                // Ignore per  https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#HistoricalPriceData
                numTrades++;
            }
            if (trade.IsFinalRecord != 0)
            {
                signal.Set();
            }
        }

        clientHistorical.HistoricalPriceDataRecordResponseEvent += ClientHistoricalOnHistoricalPriceDataResponseEvent!;

        var countEvents = 0;

        void ClientHistoricalOnEveryMessageFromServer(object sender, IMessage protobuf)
        {
            countEvents++;
        }

        clientHistorical.EveryMessageFromServer += ClientHistoricalOnEveryMessageFromServer!;

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
            Integer1 = 0
        };
        var endDateTime = request.EndDateTimeUtc;
        sw.Restart();
        clientHistorical.SendRequest(DTCMessageType.HistoricalPriceDataRequest, request);
        signal.WaitOne(1000);
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"Client1 received all {numTrades:N0} historical trades in {elapsed} msecs");

        Assert.Equal(1, numHistoricalPriceDataResponseHeader);
        Assert.Equal(exampleService.NumHistoricalPriceDataRecordsToSend, numTrades);

        countEvents.Should().BeGreaterThan(0);
    }
}