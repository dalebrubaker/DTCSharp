using System;
using System.Collections.Generic;
using System.IO;
using DTCPB;
using Serilog;

namespace DTCCommon
{
    /// <summary>
    /// See https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
    /// </summary>
    public static class SierraChartUtil
    {
        private static readonly ILogger s_logger = Log.ForContext(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType!);

        private const string TimeFormatStringSortsWithMsecs = "yyyyMMdd.HHmmss.fffffff";
        private static readonly Dictionary<string, SymbolSettings> s_symbolSettingsBySierraChartDirectoryAndSettingsFilename = new Dictionary<string, SymbolSettings>();
        private static readonly Dictionary<string, string> s_futuresSymbolPatternByPrefix = new Dictionary<string, string>();

        private static readonly object s_lock = new object();

        private static readonly object s_lockFile = new object();

        /// <summary>
        /// Get the number of records in the .dly file
        /// </summary>
        /// <param name="path">The fully-qualified path to the file</param>
        /// <returns></returns>
        public static long GetCountDly(string path)
        {
            DebugDTC.Assert(path.EndsWith(".dly"));
            if (!File.Exists(path))
            {
                return 0;
            }
            try
            {
                var lines = File.ReadAllLines(path);
                return lines.Length - 1; // don't include the header
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Read the .dly file into HistoricalPriceDataRecordResponse records
        /// </summary>
        /// <param name="path">The fully-qualified path to the file</param>
        /// <returns></returns>
        public static IEnumerable<HistoricalPriceDataRecordResponse> ReadDly(string path)
        {
            DebugDTC.Assert(path.EndsWith(".dly"));
            var lines = File.ReadAllLines(path);

            // Skip the header
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Replace(" ", "");
                var splits = line.Split(',');
                DateTime.TryParse(splits[0], out var date);
                var unixSeconds = date.UtcToDtcDateTime();
                double.TryParse(splits[1], out var openPrice);
                double.TryParse(splits[2], out var highPrice);
                double.TryParse(splits[3], out var lowPrice);
                double.TryParse(splits[4], out var lastPrice);
                long.TryParse(splits[5], out var volume);
                var responseRecord = new HistoricalPriceDataRecordResponse
                {
                    StartDateTime = unixSeconds,
                    OpenPrice = openPrice,
                    HighPrice = highPrice,
                    LowPrice = lowPrice,
                    LastPrice = lastPrice,
                    Volume = volume
                };
                yield return responseRecord;
            }
        }

        // /// <summary>
        // /// Write HistoricalPriceDataRecordResponse records to a .Dly file
        // /// </summary>
        // /// <param name="records"></param>
        // /// <param name="path">The fully-qualified path to the file</param>
        // /// <param name="priceFormatString"></param>
        // /// <returns></returns>
        // public static bool WriteDly(IEnumerable<HistoricalPriceDataRecordResponse> records, string path, string priceFormatString)
        // {
        //     DebugDTC.Assert(path.EndsWith(".dly"));
        //     if (File.Exists(path))
        //     {
        //         File.Delete(path);
        //     }
        //     using var sw = File.CreateText(path);
        //     const string Header = "Date,  Open,  High,  Low,  Close,  Volume,  OpenInterest";
        //     sw.WriteLine(Header);
        //     foreach (var record in records)
        //     {
        //         var openStr = record.OpenPrice.ToString(priceFormatString);
        //         var highStr = record.HighPrice.ToString(priceFormatString);
        //         var lowStr = record.LowPrice.ToString(priceFormatString);
        //         var closeStr = record.LastPrice.ToString(priceFormatString);
        //         var line = $"{record.StartDateTimeUtc.ToShortDateString()}, {openStr}, {highStr}, {lowStr}, {closeStr}, {record.Volume}, 0";
        //         sw.WriteLine(line);
        //     }
        //     return true;
        // }

        // /// <summary>
        // /// Append or create an existing .scid file with HistoricalPriceDataRecordResponse records
        // /// Note that this causes timestamps to be written in seconds, per the DTC record, rather than the normal microseconds granularity in the .scid file
        // /// </summary>
        // /// <param name="records">HistoricalPriceDataRecordResponse records</param>
        // /// <param name="path">The fully-qualified path to the file</param>
        // /// <param name="append">false means create a new file</param>
        // /// <returns></returns>
        // public static bool WriteScid(IEnumerable<HistoricalPriceDataRecordResponse> records, string path, bool append)
        // {
        //     DebugDTC.Assert(path.EndsWith(".scid"));
        //     if (!append && File.Exists(path))
        //     {
        //         File.Delete(path);
        //     }
        //     using var fs = File.OpenWrite(path);
        //     if (fs.Length == 0)
        //     {
        //         var header = new s_IntradayHeader
        //         {
        //             FileTypeUniqueHeaderID = new[]
        //             {
        //                 'S',
        //                 'C',
        //                 'I',
        //                 'D'
        //             },
        //             HeaderSize = (uint)s_IntradayHeader.Size,
        //             RecordSize = (uint)s_IntradayRecord.Size,
        //             Version = 1,
        //             UTCStartIndex = 0
        //         };
        //         fs.Write(header.GetBytes(), 0, s_IntradayHeader.Size);
        //     }
        //     else
        //     {
        //         fs.Seek(0, SeekOrigin.End);
        //     }
        //     foreach (var priceDataRecordResponse in records)
        //     {
        //         var record = new s_IntradayRecord(priceDataRecordResponse);
        //         fs.Write(record.GetBytes(), 0, s_IntradayRecord.Size);
        //     }
        //     return true;
        // }

        public static s_IntradayHeader GetHeader(string path)
        {
            DebugDTC.Assert(path.EndsWith(".scid"));
            if (!File.Exists(path))
            {
                throw new ArgumentException("path");
            }
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var header = GetHeader(fs);
                return header;
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get the number of records in the .scid file
        /// </summary>
        /// <param name="path">The fully-qualified path to the file</param>
        /// <returns></returns>
        public static long GetCountScid(string path)
        {
            DebugDTC.Assert(path.EndsWith(".scid"));
            if (!File.Exists(path))
            {
                return 0;
            }
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var header = GetHeader(fs);
                var count = (fs.Length - header.HeaderSize) / header.RecordSize;
                return count;
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Get the number of records in the .scid file
        /// </summary>
        /// <param name="path">The fully-qualified path to the file</param>
        /// <returns></returns>
        public static DateTime GetFirstTimestampUtcInFile(string path)
        {
            DebugDTC.Assert(path.EndsWith(".scid"));
            if (!File.Exists(path))
            {
                return DateTime.MinValue;
            }
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var header = GetHeader(fs);
                using var br = new BinaryReader(fs);
                var firstStartDateTime = br.ReadInt64();
                var firstStartDateTimeUtc = firstStartDateTime.FromScMicroSecondsToDateTime();
                return firstStartDateTimeUtc;
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }
        }

        public static (DateTime firstStartDateTimeUtc, DateTime lastStartDateTimeUtc) GetFirstAndLastTimestamp(string path)
        {
            DebugDTC.Assert(path.EndsWith(".scid"));
            if (!File.Exists(path))
            {
                return (DateTime.MinValue, DateTime.MinValue);
            }
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var br = new BinaryReader(fs);
                var header = GetHeader(fs);
                var count = (fs.Length - header.HeaderSize) / header.RecordSize;
                var firstStartDateTime = br.ReadInt64();
                var firstStartDateTimeUtc = firstStartDateTime.FromScMicroSecondsToDateTime();
                br.BaseStream.Seek(-header.RecordSize, SeekOrigin.End);
                var lastStartDateTime = br.ReadInt64();
                var lastStartDateTimeUtc = lastStartDateTime.FromScMicroSecondsToDateTime();
                return (firstStartDateTimeUtc, lastStartDateTimeUtc);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        ///  Get both the count and the startIndex for startTimestampUtc
        /// </summary>
        /// <param name="path"></param>
        /// <param name="startTimestampUtc"></param>
        /// <returns></returns>
        public static (long count, long startIndex) GetCountAndStartIndex(string path, DateTime startTimestampUtc)
        {
            DebugDTC.Assert(path.EndsWith(".scid"));
            if (!File.Exists(path))
            {
                return (0, 0);
            }
            try
            {
                // var sw = Stopwatch.StartNew();
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var br = new BinaryReader(fs);
                var header = GetHeader(fs);
                var count = (fs.Length - header.HeaderSize) / header.RecordSize;
                if (count == 0)
                {
                    return (0, 0);
                }
                var startIndex = 0L;
                var requiredStartDateTime = startTimestampUtc.ToScMicroSeconds();
                var firstStartDateTime = br.ReadInt64();
                var firstStartDateTimeUtc = firstStartDateTime.FromScMicroSecondsToDateTime();
                // if (path.Contains("ESZ1.CME"))
                // {
                //     var prevDateTime = 0L;
                //     for (var i = 0; i < count; i++)
                //     {
                //         br.BaseStream.Seek(header.HeaderSize + i * header.RecordSize, SeekOrigin.Begin);
                //         var startDateTime = br.ReadInt64();
                //         if (startDateTime < prevDateTime)
                //         {
                //             throw new Exception();
                //         }
                //         prevDateTime = startDateTime;
                //     }
                // }

                if (firstStartDateTime >= requiredStartDateTime)
                {
                    // var elapsed = sw.Elapsed;
                    return (count, startIndex);
                }
                br.BaseStream.Seek(-header.RecordSize, SeekOrigin.End);
                var lastStartDateTime = br.ReadInt64();
                // var lastStartDateTimeUtc = lastStartDateTime.FromScMicroSecondsToDateTime();
                //var lastStartDateTimeLocal = lastStartDateTimeUtc.ToLocalTime();
                if (lastStartDateTime <= requiredStartDateTime)
                {
                    // var elapsed = sw.Elapsed;
                    return (count, count - 1);
                }

                // We can't do a binary search because of timestamps out of order in .scid files,
                //  and we almost always are looking at the beginning or very close to the end.
                if (requiredStartDateTime - firstStartDateTime <= (lastStartDateTime - requiredStartDateTime))
                {
                    // We are closer to the beginning, so start looking from there.
                    for (var i = 0; i < count; i++)
                    {
                        br.BaseStream.Seek(header.HeaderSize + i * header.RecordSize, SeekOrigin.Begin);
                        var startDateTime = br.ReadInt64();
                        if (startDateTime >= requiredStartDateTime)
                        {
                            // var startDateTimeUtc = startDateTime.FromScMicroSecondsToDateTime();
                            // var startDateTimeLocal = startTimestampUtc.ToLocalTime();
                            // var elapsed = sw.Elapsed;
                            return (count, i);
                        }
                    }
                }

                // We are closer to the end, so start looking from there.
                for (var i = count - 2; i >= 0; i--)
                {
                    br.BaseStream.Seek(header.HeaderSize + i * header.RecordSize, SeekOrigin.Begin);
                    var startDateTime = br.ReadInt64();
                    if (startDateTime <= requiredStartDateTime)
                    {
                        // var elapsed = sw.Elapsed;
                        // var startDateTimeUtc = startDateTime.FromScMicroSecondsToDateTime();
                        // var startDateTimeLocal = startTimestampUtc.ToLocalTime();
                        return (count, i + 1);
                    }
                }

                // var elapsed2 = sw.Elapsed;
                return (count, startIndex);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Read the .scid file into HistoricalPriceDataRecordResponse records
        /// Note that this causes timestamps to come in unix seconds, per DTC, rather than the SC microseconds in the .scid file
        /// Skips out-of-order records
        /// </summary>
        /// <param name="path">The fully-qualified path to the file</param>
        /// <param name="startIndex">use GetCountAndStartIndex() to determine this. Default is 0</param>
        /// <returns></returns>
        public static IEnumerable<HistoricalPriceDataRecordResponse> ReadScid(string path, long startIndex = 0)
        {
            DebugDTC.Assert(path.EndsWith(".scid"));
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); // FileShare.ReadWrite because SC is writing it
            using var br = new BinaryReader(fs);
            var header = GetHeader(fs);
            if (startIndex > 0)
            {
                br.BaseStream.Seek(startIndex * header.RecordSize + header.HeaderSize, SeekOrigin.Begin);
            }

            // Allow reading to the end of the file as it grows during this method
            var prevDateTime = 0L;
            var _ = (fs.Length - header.HeaderSize) / header.RecordSize;
            for (var i = startIndex; i < (fs.Length - header.HeaderSize) / header.RecordSize; i++)
            {
                var startDateTime = br.ReadInt64();
                var openPrice = br.ReadSingle();
                var highPrice = br.ReadSingle();
                var lowPrice = br.ReadSingle();
                var lastPrice = br.ReadSingle();
                var numTrades = br.ReadUInt32();
                var volume = br.ReadUInt32();
                var bidVolume = br.ReadUInt32();
                var askVolume = br.ReadUInt32();
                if (numTrades == 1)
                {
                    // Ticks are supposed to have 0 openPrice, but sometimes have huge negative values
                    openPrice = 0;
                }
                if (startDateTime < prevDateTime)
                {
                    // Skip this out-of-order record
                    continue;
                }
                if (lastPrice == 0)
                {
                    // Skip this bad record. Seems rare but it does happen
                    continue;
                }
                prevDateTime = startDateTime;

                // Convert the SCID StartDateTime (microseconds since 12/30/1899) to HistoricalPriceDataRecordResponse StartDateTime (unix seconds)
                // https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#s_IntradayRecord__DateTime
                // https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime
                var startDateTimeScidUtc = startDateTime.FromScMicroSecondsToDateTime();
                var startDateTimeUnixSeconds = startDateTimeScidUtc.ToUnixSeconds();
                var responseRecord = new HistoricalPriceDataRecordResponse
                {
                    StartDateTime = startDateTimeUnixSeconds,
                    OpenPrice = openPrice,
                    HighPrice = highPrice,
                    LowPrice = lowPrice,
                    LastPrice = lastPrice,
                    Volume = volume,
                    BidVolume = bidVolume,
                    AskVolume = askVolume,
                    NumTrades = numTrades
                };
                var countNow = (fs.Length - header.HeaderSize) / header.RecordSize;
                if (i == countNow - 1)
                {
                    responseRecord.IsFinalRecordBool = true;
                    yield return responseRecord;
                    s_logger.Verbose("Returned final record {ResponseRecord} from {Path}", responseRecord, path);
                    yield break;
                }
                yield return responseRecord;
            }
        }

        public static (long countTotal, long countOutOfOrder, List<string> rangeStrs) GetRecordsOutOfOrder(string path)
        {
            var rangeStrs = new List<string>();
            var countTotal = GetCountScid(path);
            var countOutOfOrder = 0L;
            DebugDTC.Assert(path.EndsWith(".scid"));
            if (!File.Exists(path))
            {
                return (countTotal, countOutOfOrder, rangeStrs);
            }
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var br = new BinaryReader(fs);
                var header = GetHeader(fs);
                var countOutOfOrderSequential = 0;
                var firstStartDateTimeOutOfOrder = 0L;
                var prevScMicroSecondsStartDateTime = 0L;
                for (var i = 0L; i < countTotal; i++)
                {
                    var startDateTime = br.ReadInt64();
                    br.BaseStream.Seek(header.RecordSize - 8, SeekOrigin.Current);
                    if (startDateTime < prevScMicroSecondsStartDateTime)
                    {
                        if (countOutOfOrderSequential == 0)
                        {
                            firstStartDateTimeOutOfOrder = prevScMicroSecondsStartDateTime;
                        }
                        countOutOfOrderSequential++;
                        countOutOfOrder++;
                    }
                    else if (countOutOfOrderSequential > 0)
                    {
                        // Not out of order. End the range
                        EndSequentialOutOfOrder(countOutOfOrderSequential, firstStartDateTimeOutOfOrder, prevScMicroSecondsStartDateTime, rangeStrs);
                        countOutOfOrderSequential = 0;
                    }
                    prevScMicroSecondsStartDateTime = startDateTime;
                }
                EndSequentialOutOfOrder(countOutOfOrderSequential, firstStartDateTimeOutOfOrder, prevScMicroSecondsStartDateTime, rangeStrs);
                return (countTotal, countOutOfOrder, rangeStrs);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }

            void EndSequentialOutOfOrder(long countOutOfOrderSequential, long firstStartDateTimeOutOfOrder, long prevScMicroSecondsStartDateTime, List<string> list)
            {
                if (countOutOfOrderSequential == 0)
                {
                    return;
                }
                var startTimestamp = firstStartDateTimeOutOfOrder.FromScMicroSecondsToDateTime();
                var endTimestamp = prevScMicroSecondsStartDateTime.FromScMicroSecondsToDateTime();
                var rangeStr = $"{countOutOfOrderSequential:N0}: {startTimestamp.ToString(TimeFormatStringSortsWithMsecs)}-{endTimestamp.ToString(TimeFormatStringSortsWithMsecs)}";
                list.Add(rangeStr);
            }
        }

        private static s_IntradayHeader GetHeader(FileStream fs)
        {
            var br = new BinaryReader(fs);
            br.BaseStream.Seek(4, SeekOrigin.Begin);
            var header = new s_IntradayHeader
            {
                HeaderSize = br.ReadUInt32(),
                RecordSize = br.ReadUInt32(),
                Version = br.ReadUInt16(),
            };
            br.BaseStream.Seek(header.HeaderSize, SeekOrigin.Begin);
            var _ = br.BaseStream.Position;
            return header;
        }

        /// <summary>
        /// Truncate to the end of the previous second.
        /// Thanks to http://stackoverflow.com/questions/1004698/how-to-truncate-milliseconds-off-of-a-net-datetime
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime TruncateToSecond(this DateTime dateTime)
        {
            var extraTicks = dateTime.Ticks % TimeSpan.TicksPerSecond;
            if (extraTicks > 0)
            {
                var result = dateTime.AddTicks(-extraTicks);
                return result;
            }
            return dateTime;
        }

        /// <summary>
        /// Return <c>true</c> if there is a .Dly file in dataDirectory for symbol, using the historical data symbol as an alternate name
        /// </summary>
        /// <param name="sierraChartDirectory"></param>
        /// <param name="settingsFilename"></param>
        /// <param name="symbol"></param>
        /// <returns><c>null</c> if there is no file with either symbol or the historical chart symbol</returns>
        public static string GetDailyPath(string sierraChartDirectory, string settingsFilename, string symbol)
        {
            var dataDirectory = Path.Combine(sierraChartDirectory, "Data");
            var path = Path.Combine(dataDirectory, symbol + ".dly");
            if (File.Exists(path))
            {
                return path;
            }
            var symbolSettings = RequireSymbolSettings(sierraChartDirectory, settingsFilename);
            var historicalSymbolPattern = symbolSettings.GetInnerText(symbol, "historical-chart-symbol");
            if (string.IsNullOrEmpty(historicalSymbolPattern))
            {
                return null;
            }
            var last3CharsPattern = historicalSymbolPattern.Substring(historicalSymbolPattern.Length - 3, 3);
            var splits = symbol.Split('-');
            var symbolWithoutExchange = splits[0];
            var last3CharsSymbol = symbolWithoutExchange.Substring(symbolWithoutExchange.Length - 3, 3);
            var historicalSymbol = historicalSymbolPattern.Replace(last3CharsPattern, last3CharsSymbol);
            var historicalPath = Path.Combine(dataDirectory, historicalSymbol + ".dly");
            if (File.Exists(historicalPath))
            {
                return historicalPath;
            }
            return null;
        }

        public static double GetTickSize(string symbol, string sierraChartDirectory, string settingsFilename)
        {
            var symbolSettings = RequireSymbolSettings(sierraChartDirectory, settingsFilename);
            if (symbolSettings == null)
            {
                throw new ArgumentNullException(nameof(symbolSettings));
            }
            lock (s_lock)
            {
                // Lock or throws because duplicate adds to an inner dictionary
                //s_logger.Debug($"Getting tickSizeStr from symbolSettings for {symbol}");
                var tickSizeStr = symbolSettings.GetInnerText(symbol, "tick-size");
                double.TryParse(tickSizeStr, out var tickSize);
                return tickSize;
            }
        }

        public static double GetRealtimeMultiplier(string sierraChartDirectory, string settingsFilename, string symbol)
        {
            var symbolSettings = RequireSymbolSettings(sierraChartDirectory, settingsFilename);
            if (symbolSettings == null)
            {
                throw new ArgumentNullException(nameof(symbolSettings));
            }
            lock (s_lock)
            {
                // Lock or throws because duplicate adds to an inner dictionary
                //s_logger.Debug($"Getting real-time-multiplier from symbolSettings for {symbol}");
                var str = symbolSettings.GetInnerText(symbol, "real-time-multiplier");
                if (string.IsNullOrEmpty(str))
                {
                    // Default to 1
                    return 1;
                }
                double.TryParse(str, out var realTimeMultiplier);
                return realTimeMultiplier;
            }
        }

        private static SymbolSettings RequireSymbolSettings(string sierraChartDirectory, string settingsFilename)
        {
            if (!Directory.Exists(sierraChartDirectory))
            {
                return null;
            }
            try
            {
                var key = $"{sierraChartDirectory},{settingsFilename}";
                lock (s_symbolSettingsBySierraChartDirectoryAndSettingsFilename)
                {
                    if (!s_symbolSettingsBySierraChartDirectoryAndSettingsFilename.TryGetValue(key, out var symbolSettings))
                    {
                        var xmlPath = Path.Combine(sierraChartDirectory, "SymbolSettings", settingsFilename);
                        if (!File.Exists(xmlPath))
                        {
                            throw new FileNotFoundException(xmlPath);
                        }
                        symbolSettings = new SymbolSettings(xmlPath);
                        //s_logger.Debug($"Adding symbolSettings to dictionary for {SierraChartDirectory}");
                        s_symbolSettingsBySierraChartDirectoryAndSettingsFilename.Add(key, symbolSettings);
                        //s_logger.Debug($"Added symbolSettings to dictionary for {SierraChartDirectory}");
                    }
                    return symbolSettings;
                }
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "{Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get the symbolPattern from SC symbol settings for a futures contract. 
        /// We can't get it from DTC via the SecurityDefinition, because we need the exchange on the symbolPrefix to request it.
        /// We probably could use DTC search for it, but we use SC files anyway for futures due to CME restrictions
        /// </summary>
        /// <param name="settingsFileName"></param>
        /// <param name="symbolPrefix"></param>
        /// <param name="sierraChartDirectory"></param>
        /// <returns></returns>
        public static string GetFuturesSymbolPattern(string sierraChartDirectory, string settingsFileName, string symbolPrefix)
        {
            lock (s_futuresSymbolPatternByPrefix)
            {
                if (s_futuresSymbolPatternByPrefix.TryGetValue(symbolPrefix, out var symbolPattern))
                {
                    return symbolPattern;
                }
                try
                {
                    lock (s_lock) // Lock or throws because duplicate adds to an inner dictionary
                    {
                        var symbolSettings = RequireSymbolSettings(sierraChartDirectory, settingsFileName);
                        if (symbolSettings == null)
                        {
                            throw new ArgumentNullException(nameof(symbolSettings));
                        }
                        //s_logger.Debug($"Got symbolSettings={symbolSettings} for symbolPrefix= {symbolPrefix}");
                        symbolPattern = symbolSettings.GetFuturesSymbolPattern(symbolPrefix);
                        //s_logger.Debug($"Got symbolPattern={symbolPattern} for symbolSettings={symbolSettings} and symbolPrefix= {symbolPrefix}");
                        s_futuresSymbolPatternByPrefix.Add(symbolPrefix, symbolPattern);
                        return symbolPattern;
                    }
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, "{Message}", ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Search for a specific element.
        /// You can apply the bitwise complement operator (~ in C#) to the negative result to produce an index.
        /// If this index is equal to the size of this time series, there are no items larger than value in this time series.
        /// Otherwise, it is the index of the first element that is larger than value.
        /// Duplicate time series items are allowed.
        ///     If this time series contains more than one item equal to value, the method returns the index of only one of the occurrences,
        ///     and not necessarily the first one.
        /// This method is an O(log n) operation, where n is the length of the section to search.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="startTimestampUtc">The value to search for.</param>
        /// <param name="index">The starting index for the search.</param>
        /// <param name="length">The length of this array or the length of the section to search. The default long.MaxValue means to use Count</param>
        /// <returns>the index of startTimestampUtc in the file at path</returns>
        public static long BinarySearch(string path, DateTime startTimestampUtc, long index = 0, long length = long.MaxValue)
        {
            lock (s_lockFile)
            {
                DebugDTC.Assert(path.EndsWith(".scid"));
                if (!File.Exists(path))
                {
                    return 0;
                }
                try
                {
                    var givenStartDateTime = startTimestampUtc.ToScMicroSeconds();
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var br = new BinaryReader(fs);
                    var header = GetHeader(fs);
                    var count = (fs.Length - header.HeaderSize) / header.RecordSize;
                    if (length == long.MaxValue)
                    {
                        length = count - index;
                    }
                    var lo = index;
                    var hi = index + length - 1;
                    while (lo <= hi)
                    {
                        var i = lo + ((hi - lo) >> 1);
                        br.BaseStream.Seek(header.HeaderSize + i * header.RecordSize, SeekOrigin.Begin);
                        var arrayValue = br.ReadInt64();
#if DEBUG
                        var startDateTimeUtc = arrayValue.FromScMicroSecondsToDateTime();
#endif
                        var order = arrayValue.CompareTo(givenStartDateTime);
                        if (order == 0)
                        {
                            return i;
                        }
                        if (order < 0)
                        {
                            lo = i + 1;
                        }
                        else
                        {
                            hi = i - 1;
                        }
                    }
                    return ~lo;
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, "{Message}", ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Like std::upper_bound. Returns the index of the first startTimestampUtc in the file at path which compares greater than value.
        ///     In other words, this is the index of the first item with a value higher than value
        /// If higher than any item, returns Count.
        /// Duplicate items are allowed.
        ///     If this array contains more than one item equal to value, the method returns the index one higher than the last duplicate.
        /// This method is an O(log n) operation, where n is the length of the section to search.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="startTimestampUtc">The value to search for.</param>
        /// <param name="index">The starting index for the search.</param>
        /// <param name="length">The length of this array or the length of the section to search. The default long.MaxValue means to use Count</param>
        /// <returns>The index of the first element in this array which compares greater than value.</returns>
        public static long GetUpperBound(string path, DateTime startTimestampUtc, long index = 0, long length = long.MaxValue)
        {
            lock (s_lockFile)
            {
                try
                {
                    if (length == long.MaxValue)
                    {
                        var count = GetCountScid(path);
                        length = count - index;
                    }
                    var result = BinarySearch(path, startTimestampUtc, index, length);
                    if (result >= 0)
                    {
                        // BinarySearch found value. We want the index of the first arrayValue greater than value
                        var givenStartDateTime = startTimestampUtc.ToScMicroSeconds();
                        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var br = new BinaryReader(fs);
                        var header = GetHeader(fs);
                        br.BaseStream.Seek(header.HeaderSize + result * header.RecordSize, SeekOrigin.Begin);
                        while (++result < length)
                        {
                            var arrayValue = br.ReadInt64();
#if DEBUG
                            var startDateTimeUtc = arrayValue.FromScMicroSecondsToDateTime();
#endif
                            if (!arrayValue.Equals(givenStartDateTime))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        // BinarySearch returns the complement of the index to the first element higher than value
                        result = ~result;
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, "{Message}", ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Like std::lower_bound. Returns the index of the first startTimestampUtc in the file at path which does not compare less than value.
        ///     In other words, this is the index of the first item with a value greater than or equal to value
        /// If higher than any item, returns Count.
        /// If lower than any item, returns 0.
        /// Duplicate elements are allowed.
        ///     If this array contains more than one item equal to value, the method returns the index of the first duplicate.
        /// This method is an O(log n) operation, where n is the length of the section to search.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="startTimestampUtc">The value to search for.</param>
        /// <param name="index">The starting index for the search.</param>
        /// <param name="length">The length of this array or the length of the section to search. The default long.MaxValue means to use Count</param>
        /// <returns>The index of the first element in this array which compares greater than value.</returns>
        public static long GetLowerBound(string path, DateTime startTimestampUtc, long index = 0, long length = long.MaxValue)
        {
            lock (s_lockFile)
            {
                if (length == long.MaxValue)
                {
                    var count = GetCountScid(path);
                    length = count - index;
                }
                var result = BinarySearch(path, startTimestampUtc, index, length);
                if (result >= 0)
                {
                    // BinarySearch found value. We want the index of the lowest value equal to val
                    if (result == 0)
                    {
                        // We found a result at 0. Don't look lower.
                        return 0;
                    }

                    result--;
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var br = new BinaryReader(fs);
                    var header = GetHeader(fs);
                    br.BaseStream.Seek(header.HeaderSize + result * header.RecordSize, SeekOrigin.Begin);
                    var givenStartDateTime = startTimestampUtc.ToScMicroSeconds();
                    var arrayValue = br.ReadInt64();
#if DEBUG
                    var startDateTimeUtc = arrayValue.FromScMicroSecondsToDateTime();
#endif
                    while (result > 0 && arrayValue.Equals(givenStartDateTime))
                    {
                        result--;
                        br.BaseStream.Seek(-header.RecordSize, SeekOrigin.Current);
                        arrayValue =  br.ReadInt64();
                    }

                    // we went one too far
                    result++;
                }
                else
                {
                    // BinarySearch returns the complement of the index to the first element higher than value
                    // The lower bound is one lower (it doesn't fit in the 0 slot)
                    result = ~result - 1;
                }
                return result;
            }
        }
    }
}