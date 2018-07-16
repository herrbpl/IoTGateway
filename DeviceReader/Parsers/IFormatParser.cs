using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Devices;

namespace DeviceReader.Parsers
{    

    public interface IFormatParser<TInput, TOutput>:IDisposable
    {
        Task<TOutput> ParseAsync(TInput input, CancellationToken cancellationToken);
    }

    public class ParserMetadata
    {
        public string FormatName { get; set; }
    }

}
