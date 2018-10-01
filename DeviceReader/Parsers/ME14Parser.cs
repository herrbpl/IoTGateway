

namespace DeviceReader.Parsers
{
    using DeviceReader.Diagnostics;
    using DeviceReader.Models;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class ME14ParserOptions
    {
        public string SchemaPath { get; set; } = "";
    }

    public class ME14ConvertRecord
    {
        public string Source { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ParameterName { get; set; }
        public string StatisticsName { get; set; }
        public string StatisticsPeriod { get; set; }
    }


    public class ME14Parser : AbstractFormatParser<ME14ParserOptions, string, Observation>
    {

        protected Dictionary<string, ME14ConvertRecord> conversionTable;

        public ME14Parser(ILogger logger, string optionspath, IConfigurationRoot configroot) :
            base(logger, optionspath, configroot)
        {
            conversionTable = new Dictionary<string, ME14ConvertRecord>();
            
            string jsonString = "";
            // if empty path, use built in resource
            if (_options.SchemaPath.Equals(""))
            {
                var byteArray = Properties.Resources.me14_observations;
                jsonString = System.Text.Encoding.UTF8.GetString(byteArray);
                
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

            } catch (Exception e)
            {

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

            if (input == null || input.Length == 0) return Task.FromResult(new List<Observation>());
            return null;
        }
    }
}
