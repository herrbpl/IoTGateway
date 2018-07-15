using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Parsers
{
    class FormatParserFactory<TOutput> : IFormatParserFactory<object, TOutput>
    {
        public IFormatParser<object, TOutput> GetFormatParser()
        {
            throw new NotImplementedException();
        }

        
    }
}
