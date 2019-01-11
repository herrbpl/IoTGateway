

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

    public class ME14ParserOptions
    {
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
        private const string DEFAULT_PARAMETER_TYPEMAP_FILE = "me14_observations.json";
        protected Dictionary<string, ME14ConvertRecord> _conversionTable;

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
            var lines = input.Split("\r\n");

            if (lines.Length < 4)
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
                        convertedValue = ObservationData.GetAsTyped(datavalue, _conversionTable[datanumber].DataType, true);
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
