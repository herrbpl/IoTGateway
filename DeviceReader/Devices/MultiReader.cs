using DeviceReader.Diagnostics;
using DeviceReader.Parsers;
using DeviceReader.Protocols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Devices
{

    public class MultiReaderRow<TOutput>
    {
        public Boolean CanFail { get; set; } = false;
        public IProtocolReader ProtocolReader { get; set; }
        public IFormatParser<string,TOutput> FormatParser { get; set; }         
    }

    // Don't know at moment how to write good generic code for case, where multiple types of input might exist.

    public class MultiReader<TOutput>
    {        
        private ConcurrentDictionary<string, MultiReaderRow<TOutput>> readers;
        private readonly ILogger logger;
        public MultiReader()
        {
            readers = new ConcurrentDictionary<string, MultiReaderRow<TOutput>>();
            this.logger = Logger.GetLogger();
        }

        public MultiReader(ILogger logger)
        {
            readers = new ConcurrentDictionary<string, MultiReaderRow<TOutput>>();
            this.logger = logger;
            
        }

        
        public void AddReader( string name, IProtocolReader protocolReader, IFormatParser<string, TOutput> formatParser, Boolean canFail)
        {
            AddReader<TOutput>(name, new MultiReaderRow<TOutput>()
            {
                CanFail = canFail,
                FormatParser = formatParser,
                ProtocolReader = protocolReader
            }); 
        }

        public void AddReader(string name, IProtocolReader protocolReader, IFormatParser<string, TOutput> formatParser)
        {
            AddReader(name, protocolReader, formatParser, false);
        }

        public void AddReader<TFormat>(string name, MultiReaderRow<TOutput> reader)
        {
            MultiReaderRow<TOutput> r;
            if (readers.ContainsKey(name))
            {
                if (!readers.TryRemove(name, out r))
                {
                    throw new ConcurrencyException($"Unable to remove item {name}");
                }
            }

            if (!readers.TryAdd(name, reader))
            {
                throw new ConcurrencyException($"Unable to add item {name}");
            }
        }
        
        public static implicit operator MultiReaderRow<TOutput>[] (MultiReader<TOutput> pipeline)
        {
            return pipeline.readers.Values.ToArray();
        }
        
        internal async Task<List<TOutput>> ReadAsyncOne(MultiReaderRow<TOutput> item, CancellationToken cancellationToken)
        {
            string readerResult = null;
            List<TOutput> result = null;

            var reader = item.ProtocolReader;
            var parser = item.FormatParser;
            try
            {
                if (reader != null)
                {
                    readerResult = await reader.ReadAsync(cancellationToken);
                }
                if (parser != null && readerResult != null)
                {
                    result = await parser.ParseAsync(readerResult, cancellationToken);
                }
                
            } catch (Exception e)
            {
                if (item.CanFail)
                {
                    logger.Warn($"Failed to read {reader.ToString()}, exception: {e.Message}", ()=>{ });
                } else
                {
                    throw e;
                }
            }
            return result;
        }

        public async Task<Dictionary<string,List<TOutput>>> ReadAsync(CancellationToken cancellationToken)
        {
            var _tasks = new Dictionary<string,Task<List<TOutput>>>();
            foreach (var item in readers)
            {
                _tasks.Add(item.Key,Task.Factory.StartNew<List<TOutput>>(
                    () => ReadAsyncOne(item.Value,cancellationToken).Result              
                ) );                
            }
            
            await Task.WhenAll(_tasks.Values.ToArray());
            var result = new Dictionary<string, List<TOutput>>();

            List<Exception> exceptions = new List<Exception>();

            foreach (var item in _tasks)
            {
                //result.Add(item.Key, item.Value.Result);
                
                if (!item.Value.IsFaulted && item.Value.Exception == null)
                {
                    if (item.Value.Result != null) result.Add(item.Key, item.Value.Result);
                } else
                {
                    
                    if (item.Value.Exception != null)
                    {
                        exceptions.Add(item.Value.Exception.Flatten());
                    } else
                    {
                        exceptions.Add(new Exception($"Multireader key {item.Key} faulted unexpectedly!"));
                    }
                }
                
            }
            
            if (exceptions.Count > 0)
            {
                throw new AggregateException("Error while executing multireader!",exceptions.ToArray());
            }
            
            return result;
        }
    }
}
