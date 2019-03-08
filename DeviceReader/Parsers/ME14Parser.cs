

namespace DeviceReader.Parsers
{
    using DeviceReader.Data;
    using DeviceReader.Devices;
    using DeviceReader.Diagnostics;
    using DeviceReader.Models;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public enum ME14_Mode
    {
        /// <summary>
        /// Normal polling mode
        /// </summary>
        Normal  = 1,
        /// <summary>
        /// Combined polling mode with DRS511 and DTS12G coming from first and second DRI701 units
        /// What datasets exist, are determined by ID
        /// </summary>
        Combined = 2,
        /// <summary>
        /// Use DSC_DST dataset schema (used in cases where there is no ROSA/RWS200 station, only sensor directly)
        /// </summary>
        DSC_DST = 3
    }

    public class ME14ParserOptions
    {
        public ME14_Mode Mode { get; set; } = ME14_Mode.Normal;
        public string SchemaPath { get; set; } = "";
        public string TagNameTemplate { get; set; } = "{code}.{statname}.{statperiod}.{source}";
    }

    public class ME14ConvertRecord
    {
        [JsonProperty("SOURCE")]
        public string Source { get; set; }
        [JsonProperty("CODE")]
        public string Code { get; set; }
        [JsonProperty("NAME")]
        public string Name { get; set; }
        [JsonProperty("DESCRIPTION")]
        public string Description { get; set; }
        [JsonProperty("PARAMETERNAME")]
        public string ParameterName { get; set; }
        [JsonProperty("STATISTICSNAME")]
        public string StatisticsName { get; set; }
        [JsonProperty("STATISTICSPERIOD")]
        public string StatisticsPeriod { get; set; }
        [JsonProperty("DATATYPE")]
        public string DataType { get; set; }
    }


    public class ME14Parser : AbstractFormatParser<ME14ParserOptions, string, Observation>
    {
        public const string DEFAULT_PARAMETER_TYPEMAP_FILE = "me14_observations.json";
        public const string DCS211_DST111_PARAMETER_TYPEMAP_FILE = "me14_DSC211-DST111_observations.json";

        protected Dictionary<string, ME14ConvertRecord> _conversionTable;

        protected IDictionary<string, ME14ConvertRecord> GetConversionTable(ME14ParserOptions options, string identificator)
        {
            if (options == null) options = default(ME14ParserOptions);
            if (identificator == null || identificator == "") identificator = "01";

            string parametermap = "";

            // load parameter map based on ME14 mode
            if (options.Mode == ME14_Mode.DSC_DST) { parametermap = DCS211_DST111_PARAMETER_TYPEMAP_FILE; }
            else if (options.Mode == ME14_Mode.Combined)
            {
                if (identificator == "01" || identificator == "02")
                {
                    parametermap = DEFAULT_PARAMETER_TYPEMAP_FILE;
                }
                else if ((identificator == "03" || identificator == "04"))
                {
                    parametermap = DCS211_DST111_PARAMETER_TYPEMAP_FILE;
                }
                else
                {
                    _logger.Error($"parametermap = '{parametermap}'", () => { });
                    throw new ArgumentOutOfRangeException($"parametermap = '{parametermap}'");
                }
            } else {
                parametermap = DEFAULT_PARAMETER_TYPEMAP_FILE;
            }
            _logger.Debug($"Using {parametermap} for conversion.", () => { });
            Dictionary<string, ME14ConvertRecord> result = null;
            // try to convert to structure
            try
            {
                var jsonString = StringResources.Resources[parametermap];
                result = JsonConvert.DeserializeObject<Dictionary<string, ME14ConvertRecord>>(jsonString);

            }
            catch (Exception e)
            {
                _logger.Error($"{e}", () => { });
                throw e;
            }
            return result;
            

        }

        public ME14Parser(ILogger logger, string optionspath, IConfiguration configroot) :
            base(logger, optionspath, configroot)
        {
            _conversionTable = new Dictionary<string, ME14ConvertRecord>();
            
            string jsonString = "";

            _logger.Debug($"Schema path '{_options.SchemaPath}'", () => { });

            // if empty path, use built in resource
            if (_options.SchemaPath.Equals(""))
            {

                jsonString = StringResources.Resources[DEFAULT_PARAMETER_TYPEMAP_FILE];
                
            }
            else
            {
                // if file not found, fail
                if (!File.Exists(_options.SchemaPath)) throw new FileNotFoundException(_options.SchemaPath);

                jsonString = File.ReadAllText(_options.SchemaPath);
            }

            // try to convert to structure
            try
            {

                _conversionTable = JsonConvert.DeserializeObject<Dictionary<string, ME14ConvertRecord>>(jsonString);

                
            } catch (Exception e)
            {
                _logger.Error($"{e}", () => { });                
                throw e;
            }
            
        }

        /*
         * 
         * 2018-10-01  11:24,01,M14,amtij
01  11.7;02    82;03   8.8;04     0;05   3.0;06   220;08   0.0;09   0.0;
10   0.0;11  2000;14 13.55;15     1;16     0;21   0.4;23     0;26   6.7;
27   231;30  14.6;31  12.2;32   0.0;33   0.9;34   290;35   0.0;36    21;
38   1.4;39   0.0;40   0.0;41   0.0;42  0.00;43   0.0;44   0.0;90     0;
91     0;92     0;
=
3CD5

    */

        public override Task<List<Observation>> ParseAsync(string input, CancellationToken cancellationToken)
        {

            input = input.Trim();

            if (input == null || input.Length == 0) return Task.FromResult(new List<Observation>());
            _logger.Debug($"parsing input '{input}'", () => { });
            // Message should have 4 parts: 1 line header, lines with data, = on empty line and checksum on last line.
            // 2019/03/07/Siim Aus/Amendment - simple devices sometimes do not add checksum. So, we ensure in protocol reader that only header and data lines are passed
            var lines = input.Split("\r\n");

            if (lines.Length < 2)
            {
                _logger.Warn("input too short, probably invalid message format", () => { });
                throw new ArgumentException("input too short, probably invalid message format");
            }

            // ROSA stations writes MES 14 at beginning of message while RWS200 does not

            int headerstartsat = 0;
            if (lines[0] == "MES 14")
            {
                headerstartsat++;
            }
            // header
            var header = lines[headerstartsat];

            var headers = header.Split(',');
            if (headers.Length != 4)
            {
                _logger.Warn("header missing or with invalid structure", () => { });
                throw new ArgumentException("header missing or with invalid structure");
            }


            const DateTimeStyles style = DateTimeStyles.AllowWhiteSpaces;

            DateTime timestamp;

            if (!DateTime.TryParseExact(headers[0].Replace("  ", " "), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, style, out timestamp))
            {
                _logger.Warn("header datetime stamp invalid", () => { });
                throw new ArgumentException("header datetime stamp invalid");
            }

            var deviceId = headers[3];
            var identifier = headers[1];

            // Try to get conversation table.
            var _conversionTable = GetConversionTable(_options, identifier);

            // observations
            List<ObservationData> observations = new List<ObservationData>();

            

            for (var i =headerstartsat+1; i<lines.Length;i++)
            {
                var line = lines[i];
                //_logger.Debug($"Line: '{line}'", () => { });
                if (line.Equals("="))
                {
                    break;
                }

                var measures = line.Split(';');
                foreach (var measure in measures) 
                {

                    if (measure.Equals("")) continue;

                    // check if length is correct
                    if (measure.Length != 8)
                    {
                        _logger.Warn($"measurement '{measure}' is of invalid length, skipping", () => { });
                        continue;
                    }

                    // check if data data number exists
                    string datanumber = measure.Substring(0, 2);
                    string datavalue = measure.Substring(2, 6).Trim();

                    //_logger.Debug($"{deviceId}:{timestamp}:{datanumber}:{datavalue}", () => { });

                    if (!_conversionTable.ContainsKey(datanumber))
                    {
                        _logger.Warn($"measurement '{measure}' is not found in conversion table, skipping", () => { });
                        continue;
                    }

                    // check if value is not errorneus
                    if (datavalue == "/////")
                    {
                        _logger.Warn($"measurement '{measure}' has invalid value, skipping", () => { });
                        continue;
                    }

                    dynamic convertedValue = null;
                    // data value type conversion                    
                    try
                    {
                        convertedValue = ObservationData.GetAsTyped(datavalue, _conversionTable[datanumber].DataType, false, true);
                    } catch (ArgumentException e)
                    {
                        _logger.Warn($"Unable to convert datanumber {datanumber} ({_conversionTable[datanumber].Code}) value '{datavalue}' to '{_conversionTable[datanumber].DataType}'", () => { });
                    }
                    
                    // ObservationData
                    var od = new ObservationData()
                    {
                        Value = convertedValue,
                        Code = _conversionTable[datanumber].Code,
                        Timestamp = timestamp,
                        Source = _conversionTable[datanumber].Source,
                        StatName = _conversionTable[datanumber].StatisticsName,
                        StatPeriod = _conversionTable[datanumber].StatisticsPeriod,
                        Height = 0,
                        Unit = _conversionTable[datanumber].Description,
                        Measure = _conversionTable[datanumber].ParameterName,
                        QualityLevel = 0,
                        QualityValue = 8500,
                        TagName = _conversionTable[datanumber].ParameterName + "." + deviceId.Replace(".", "_") + "." +
                        _conversionTable[datanumber].Source + "." +
                        _conversionTable[datanumber].Code + "." +
                        _conversionTable[datanumber].StatisticsName + "." +
                        _conversionTable[datanumber].StatisticsPeriod
                    };

                    // based on what number is specified in ID, decide what source module info comes from
                    if (_options.Mode == ME14_Mode.Combined)
                    {
                        if (identifier == "02")
                        {
                            if (od.Source == "DRS511_1") od.Source = "DRS511_3";
                            if (od.Source == "DRS511_2") od.Source = "DRS511_4";
                        }

                        if (identifier == "04")
                        {
                            if (od.Source == "DSC211_1") od.Source = "DSC211_2";                            
                        }
                    }                    

                    od.TagName = ObservationData.GetTagName(_options.TagNameTemplate, od);

                    observations.Add(od);

                }
            }

            // create observation and add 
            var observation = new Observation()
            {
                DeviceId = deviceId
                ,
                Timestamp = timestamp
                ,
                GeoPositionPoint = null
                ,
                Data = observations

            };

            var result = new List<Observation>();
            result.Add(observation);
            return Task.FromResult(result);
        }
    }
}
