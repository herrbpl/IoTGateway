using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Abstractions;
using DeviceReader.Parsers;
using DeviceReader.Protocols;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Devices
{
    public static class MultiReaderExtensions
    {
        public static async Task<List<TOutput>> ReadAsyncMerged<TOutput>(this MultiReader<TOutput> multiReader, string primaryKey, MergeOptions mergeOptions,  CancellationToken cancellationToken) where TOutput: IMergeable<TOutput>
        {

            if (mergeOptions == null) mergeOptions = MergeOptions.DefaultMergeOptions;

            var result = await multiReader.ReadAsync(cancellationToken);

            if (result == null) return new List<TOutput>();
            if (result.Count == 0) return new List<TOutput>();

            // if primaryKey is not specified or is empty?
            if (string.IsNullOrEmpty(primaryKey))
            {
                primaryKey = result.Keys.FirstOrDefault();
            }

            if (!result.ContainsKey(primaryKey))
            {
                throw new ArgumentOutOfRangeException($"No key found - '{primaryKey}'");
            }

            List<TOutput> primary = null;
            result.Remove(primaryKey, out primary);

            for (int i = 0; i < primary.Count; i++)
            {
                var first = primary[i];
                
                foreach (var item in result)
                {
                    if (item.Value.Count > i )
                    {
                        mergeOptions.PrefixSecond = item.Key;
                        first = first.Merge(item.Value[i],
                            mergeOptions);
                            //new MergeOptions { MergeConflicAction= MergeConflicAction.KeepFirst, PrefixFirst="", PrefixSecond=item.Key });
                    }
                }
                primary[i] = first;
            }
            return primary;

        }

        public static async Task<List<TOutput>> ReadAsyncMerged<TOutput>(this MultiReader<TOutput> multiReader, string primaryKey, CancellationToken cancellationToken) where TOutput : IMergeable<TOutput>
        {
            return await multiReader.ReadAsyncMerged<TOutput>(primaryKey, null, cancellationToken);
        }

        public static void AddFromConfig<TOutput>(this MultiReader<TOutput> multiReader, 
            IFormatParserFactory<string,TOutput> formatParserFactory,
            IProtocolReaderFactory protocolReaderFactory,
            string configKey, IConfiguration configuration)
        {

            

            if (configuration == null || string.IsNullOrEmpty(configKey)) throw new ArgumentNullException();
            
            IConfiguration section = configuration.GetSection(configKey);


            var readerconfigs = section.GetChildren(); //.ToDictionary(x => x.Key, x => x.Path);

            foreach (var item in readerconfigs)
            {
                string format = configuration.GetValue<string>(item.Path + ":format");
                string key_format_config = item.Path + ":format_config";
                string protocol = configuration.GetValue<string>(item.Path + ":protocol");
                string key_protocol_config = item.Path + ":protocol_config";

                var protocolReader = protocolReaderFactory.GetProtocolReader(protocol, key_protocol_config, configuration);
                var formatParser = formatParserFactory.GetFormatParser(format, key_format_config, configuration);
                multiReader.AddReader(item.Key, protocolReader, formatParser);
            }            
        }
    }
}
