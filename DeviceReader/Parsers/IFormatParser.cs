using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Parsers
{    

    public interface IFormatParser<TInput, TOutput>
    {
        Task<TOutput> ParseAsync(TInput input, CancellationToken cancellationToken);
    }

    public interface IFormatParserFactory<TInput, TOutput>
    {
        IFormatParser<TInput,TOutput> GetFormatParser();        // should we get reader based on class or something else? 
    }
}
