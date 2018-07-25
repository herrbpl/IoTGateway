using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceReader.Extensions
{
    public class ConfigurationJsonStringSource : IConfigurationSource
    {
        private readonly string sourceJson;
        public ConfigurationJsonStringSource(string json)
        {
            this.sourceJson = json;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ConfigurationJsonStringProvider(sourceJson);
        }
    }

    public class ConfigurationJsonStringProvider : ConfigurationProvider
    {
        private readonly string sourceJson;

        public ConfigurationJsonStringProvider(string sourceJson)
        {
            this.sourceJson = sourceJson;
        }
        public override void Load()
        {
            Data = JsonConfigurationFileParser.Parse(sourceJson);
        }
    }

    public static class ConfigurationJsonExtension
    {
        public static IConfigurationBuilder AddJsonString(
            this IConfigurationBuilder builder, string json)
        {            
            //builder.Add( (s) => { return null; });
            return builder.Add(new ConfigurationJsonStringSource(json));
        }
    }

    /// <summary>
    /// https://github.com/aspnet/Configuration/blob/master/src/Config.Json/JsonConfigurationFileParser.cs
    /// </summary>
    internal class JsonConfigurationFileParser
    {
        private JsonConfigurationFileParser() { }

        private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _context = new Stack<string>();
        private string _currentPath;

        private JsonTextReader _reader;

        public static IDictionary<string, string> Parse(Stream input)
            => new JsonConfigurationFileParser().ParseStream(input);

        public static IDictionary<string, string> Parse(string input)
            => new JsonConfigurationFileParser().ParseString(input);

        private IDictionary<string, string> ParseString(string input)
        {
            _data.Clear();


            var jsonConfig = JObject.Parse(input);

            VisitJObject(jsonConfig);

            return _data;
        }

        private IDictionary<string, string> ParseStream(Stream input)
        {
            _data.Clear();
            _reader = new JsonTextReader(new StreamReader(input));
            _reader.DateParseHandling = DateParseHandling.None;

            var jsonConfig = JObject.Load(_reader);

            VisitJObject(jsonConfig);

            return _data;
        }

        private void VisitJObject(JObject jObject)
        {
            foreach (var property in jObject.Properties())
            {
                EnterContext(property.Name);
                VisitProperty(property);
                ExitContext();
            }
        }

        private void VisitProperty(JProperty property)
        {
            VisitToken(property.Value);
        }

        private void VisitToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    VisitJObject(token.Value<JObject>());
                    break;

                case JTokenType.Array:
                    VisitArray(token.Value<JArray>());
                    break;

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Bytes:
                case JTokenType.Raw:
                case JTokenType.Null:
                    VisitPrimitive(token.Value<JValue>());
                    break;

                default:
                    throw new FormatException(string.Format("Unsupported JSON token '{0}' was found. Path '{1}', line {2} position {3}.",
                        _reader.TokenType,
                        _reader.Path,
                        _reader.LineNumber,
                        _reader.LinePosition));
            }
        }

        private void VisitArray(JArray array)
        {
            for (int index = 0; index < array.Count; index++)
            {
                EnterContext(index.ToString());
                VisitToken(array[index]);
                ExitContext();
            }
        }

        private void VisitPrimitive(JValue data)
        {
            var key = _currentPath;

            if (_data.ContainsKey(key))
            {
                throw new FormatException(string.Format("A duplicate key '{0}' was found.", key));
            }
            _data[key] = data.ToString(CultureInfo.InvariantCulture);
        }

        private void EnterContext(string context)
        {
            _context.Push(context);
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private void ExitContext()
        {
            _context.Pop();
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }
    }

}
