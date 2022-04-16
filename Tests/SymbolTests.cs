using System.IO;
using DTCCommon;
using FluentAssertions;
using Xunit;

namespace Tests
{
    [Collection("Logging collection")]
    public class SymbolTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;

        public SymbolTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void LoadSymbolSettingsTests()
        {
            var path = @"SymbolSettings.rithmic_v2.trading.xml";
            var exists = File.Exists(path);
            exists.Should().BeTrue();

            // use Rithmic format for this unit test (smaller .xml file)
            var symbolSettings = new SymbolSettings(path);
            var historicalChartSymbol = symbolSettings.GetInnerText("MNQM1.CME", "historical-chart-symbol");
            historicalChartSymbol.Should().NotBeNullOrEmpty();

            var tickSizeStr = symbolSettings.GetInnerText("MNQM1.CME", "tick-size");
            tickSizeStr.Should().NotBeNullOrEmpty();

            var all = symbolSettings.GetAllElementNamesWithInnerText("MNQM1.CME");
            all.Count.Should().BeGreaterThan(0);
        }
    }
}