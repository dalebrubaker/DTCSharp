using System;
using System.Diagnostics;
using System.Linq;
using DTCCommon;
using FluentAssertions;
using Xunit;

namespace TestsDTC
{
    public class ScidTests
    {
        public const string TestFilePath = "MNQU21-CME.scid";

        [Fact]
        public void GetHeaderTest()
        {
            var header = SierraChartUtil.GetHeader(TestFilePath);
            header.Version.Should().Be(1);
            header.RecordSize.Should().Be(40);
        }

        [Fact]
        public void ReadRecordsOutOfOrder_Tests()
        {
            var sw = Stopwatch.StartNew();
            var (countTotal, countOutOfOrder, rangeStrs) = SierraChartUtil.GetRecordsOutOfOrder(TestFilePath);
            var elapsedMs = sw.ElapsedMilliseconds; // 270 first pass 215 unsafe, 261 with BinaryReader
            var elapsedMinutes = (double)elapsedMs / 60000;
            var minutesPerBillion = 1E9 / countTotal * elapsedMinutes; // 90.6 first pass 73.5 unsafe, 89.3 with BinaryReader
            countTotal.Should().Be(48737);
            countOutOfOrder.Should().Be(0);
            rangeStrs.Count.Should().Be(0);
        }

        [Fact]
        public void Read_Timing_Tests()
        {
            var count = SierraChartUtil.GetCountScid(TestFilePath);
            count.Should().Be(48737);
            var sw = Stopwatch.StartNew();
            var records = SierraChartUtil.ReadScid(TestFilePath).ToList();
            var countTotal = records.Count;
            countTotal.Should().Be((int)count);
            var elapsedMs = sw.ElapsedMilliseconds; // 70 first pass (with unsafe header) 28 with BinaryReader instead of Marshall.CopyFrom
            var elapsedMinutes = (double)elapsedMs / 60000;
            var minutesPerBillion = 1E9 / countTotal * elapsedMinutes; // 23.9 first pass (with unsafe header) 9.6 with BinaryReader instead of Marshall.CopyFrom
        }

        [Fact(Skip = "Manual")]
        //[Fact]
        public void ReadRecordsOutOfOrderWMT_Tests()
        {
            var sw = Stopwatch.StartNew();
            var (countTotal, countOutOfOrder, rangeStrs) = SierraChartUtil.GetRecordsOutOfOrder(@"D:\SierraChart1T\Data\WMT.scid"); // 1.1GB on 6/3/2021
            var elapsedMs = sw.ElapsedMilliseconds; // 20539 first pass 3504 unsafe
            var elapsedMinutes = (double)elapsedMs / 60000;
            var minutesPerBillion = 1E9 / countTotal * elapsedMinutes; // 13.15 first pass 2.03 unsafe
            countTotal.Should().BeGreaterThan(28000000);
            countOutOfOrder.Should().BeGreaterThan(20000);
            rangeStrs.Count.Should().BeGreaterThan(20000);
        }

        [Fact(Skip = "Manual")]
        //[Fact]
        public void Read_TimingWMT_Tests()
        {
            var sw = Stopwatch.StartNew();
            var records = SierraChartUtil.ReadScid(@"D:\SierraChart1T\Data\WMT.scid").ToList();
            var countTotal = records.Count;
            countTotal.Should().BeGreaterThan(1000000);
            var elapsedMs = sw.ElapsedMilliseconds; // 17466 first pass (with BinaryReader instead of Marshall.CopyFrom
            var elapsedMinutes = (double)elapsedMs / 60000;
            var minutesPerBillion = 1E9 / countTotal * elapsedMinutes; // 10.1 first pass (with BinaryReader instead of Marshall.CopyFrom
        }

        [Fact]
        public void GetCountAndStartIndex_Tests()
        {
            var sw = Stopwatch.StartNew();
            var (count, startIndex) = SierraChartUtil.GetCountAndStartIndex(TestFilePath, DateTime.MinValue);
            count.Should().Be(48737);
            startIndex.Should().Be(0);
            (count, startIndex) = SierraChartUtil.GetCountAndStartIndex(TestFilePath, DateTime.MaxValue);
            count.Should().Be(48737);
            startIndex.Should().Be(count - 1);
            (_, startIndex) = SierraChartUtil.GetCountAndStartIndex(TestFilePath, new DateTime(2021, 1, 1));
            startIndex.Should().Be(80);
            (count, startIndex) = SierraChartUtil.GetCountAndStartIndex(TestFilePath, new DateTime(2021, 5, 1));
            startIndex.Should().Be(23408);
            var elapsed = sw.ElapsedMilliseconds;
        }
        
        [Fact]
        public void BinarySearch_Tests()
        {
            var sw = Stopwatch.StartNew();
            var binarySearch = SierraChartUtil.BinarySearch(TestFilePath, DateTime.MinValue);
            binarySearch.Should().Be(-1, "Not found");
            var lowerBound = SierraChartUtil.GetLowerBound(TestFilePath, DateTime.MinValue);
            lowerBound.Should().Be(-1);
            var count = SierraChartUtil.GetCountScid(TestFilePath);
            count.Should().Be(48737);
            var upperBound = SierraChartUtil.GetUpperBound(TestFilePath, DateTime.MinValue);
            upperBound.Should().Be(0);
            binarySearch = SierraChartUtil.BinarySearch(TestFilePath, DateTime.MaxValue);
            var index = ~binarySearch;
            index.Should().Be(count);
            lowerBound = SierraChartUtil.GetLowerBound(TestFilePath, DateTime.MaxValue);
            lowerBound.Should().Be(count - 1);
            upperBound = SierraChartUtil.GetUpperBound(TestFilePath, DateTime.MaxValue);
            upperBound.Should().Be(count);

            var timestamp = new DateTime(2021, 1, 1);
            binarySearch = SierraChartUtil.BinarySearch(TestFilePath, timestamp);
            index = ~binarySearch;
            index.Should().Be(80);
            lowerBound = SierraChartUtil.GetLowerBound(TestFilePath, timestamp);
            lowerBound.Should().Be(79);
            upperBound = SierraChartUtil.GetUpperBound(TestFilePath, timestamp);
            upperBound.Should().Be(80);
            
            timestamp = new DateTime(2021, 5, 1);
            binarySearch = SierraChartUtil.BinarySearch(TestFilePath, timestamp);
            index = ~binarySearch;
            index.Should().Be(23408);
            lowerBound = SierraChartUtil.GetLowerBound(TestFilePath, timestamp);
            lowerBound.Should().Be(23407);
            upperBound = SierraChartUtil.GetUpperBound(TestFilePath, timestamp);
            upperBound.Should().Be(23408);
            var elapsed = sw.ElapsedMilliseconds;
        }

        [Fact]
        public void GetFirstAndLastTimestamp_Tests()
        {
            var sw = Stopwatch.StartNew();
            var (firstStartDateTimeUtc, lastStartDateTimeUtc) = SierraChartUtil.GetFirstAndLastTimestamp(TestFilePath);
            firstStartDateTimeUtc.Should().BeOnOrBefore(lastStartDateTimeUtc);
        }

        [Fact(Skip = "Manual")]
        //[Fact]
        public void GetFirstAndLastTimestamp_MNQM21_CME_Tests()
        {
            var sw = Stopwatch.StartNew();
            var path = @"D:\SierraChart1T\Data\MNQM21-CME.scid";
            var (firstStartDateTimeUtc, lastStartDateTimeUtc) = SierraChartUtil.GetFirstAndLastTimestamp(path);
            firstStartDateTimeUtc.Should().BeOnOrBefore(lastStartDateTimeUtc);
            var records = SierraChartUtil.ReadScid(path).ToList();
            var first = records[0].StartDateTimeUtc;
            var last = records[records.Count - 1].StartDateTimeUtc;
            first.Should().Be(firstStartDateTimeUtc.TruncateToSecond());
            last.Should().Be(lastStartDateTimeUtc.TruncateToSecond());
        }

        [Fact]
        public void FirstAndLastTimestamp_ShouldMatchReadScidValues_Tests()
        {
            var sw = Stopwatch.StartNew();
            var (firstStartDateTimeUtc, lastStartDateTimeUtc) = SierraChartUtil.GetFirstAndLastTimestamp(TestFilePath);
            firstStartDateTimeUtc.Should().BeOnOrBefore(lastStartDateTimeUtc);
            var records = SierraChartUtil.ReadScid(TestFilePath).ToList();
            var first = records[0].StartDateTimeUtc;
            var last = records[records.Count - 1].StartDateTimeUtc;
            first.Should().Be(firstStartDateTimeUtc.TruncateToSecond());
            last.Should().Be(lastStartDateTimeUtc.TruncateToSecond());
        }
    }
}