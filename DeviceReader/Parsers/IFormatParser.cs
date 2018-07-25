using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Parsers
{    

    public interface IFormatParser<TInput, TOutput>:IDisposable
    {
        Task<List<TOutput>> ParseAsync(TInput input, CancellationToken cancellationToken);
    }

    public class ParserMetadata
    {
        public string FormatName { get; set; }
    }

}
