using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Agents;
using DeviceReader.Models;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Security.Authentication;
using System.Linq;

namespace DeviceReader.Devices
{

    public class DeviceAgentWriterFilter
    {
        /// <summary>
        /// Which data tags to include. Regex
        /// </summary>
        public List<string> Include { get; set; } = new List<string>() { ".*" };

        /// <summary>
        /// which data tags to exclude. Regex
        /// </summary>
        public List<string> Exclude { get; set; } = new List<string>();

        /// <summary>
        /// which properties to include
        /// </summary>
        public List<string> Properties { get; set; } = new List<string>() { ".*" };
    }

    /// <summary>
    /// Sends observation payload info to upstream as json string. Upstream medium and format is deviced by IDevice.
    /// Although, if upstream message receiver expects payload data in some other format then new writer is needed
    /// </summary>
    public class DeviceAgentWriter : AgentExecutable
    {
        
        protected readonly IDevice _writer;

        protected readonly string KEY_AGENT_EXECUTABLE_FILTER;
        protected readonly string KEY_AGENT_EXECUTABLE_FILTER_INCLUDE;
        protected readonly string KEY_AGENT_EXECUTABLE_FILTER_EXCLUDE;
        protected readonly string KEY_AGENT_EXECUTABLE_FILTER_PROPERTIES;

        protected readonly string KEY_AGENT_EXECUTABLE_TAGRENAMEURL;

        protected  DeviceAgentWriterFilter _filter;

        protected Dictionary<string, bool> _filterCache;
        protected Dictionary<string, string> _properties;
        protected Dictionary<string, string> _renameMap;

        // Don't overthink it. Just add IDevice to constructor. 
        // TODO: Add tags renaming - loadable from configuration. 
        public DeviceAgentWriter(ILogger logger, IAgent agent, string name, IDevice writer):base(logger,agent, name, writer) {
            
            _writer = writer;
            KEY_AGENT_EXECUTABLE_FILTER = KEY_AGENT_EXECUTABLE_ROOT + ":filter";
            KEY_AGENT_EXECUTABLE_FILTER_INCLUDE = KEY_AGENT_EXECUTABLE_FILTER + ":Include";
            KEY_AGENT_EXECUTABLE_FILTER_EXCLUDE = KEY_AGENT_EXECUTABLE_FILTER + ":Exclude";
            KEY_AGENT_EXECUTABLE_FILTER_PROPERTIES = KEY_AGENT_EXECUTABLE_FILTER + ":Properties";
            KEY_AGENT_EXECUTABLE_TAGRENAMEURL = KEY_AGENT_EXECUTABLE_ROOT + ":renamesource";
            // Try to get filter.

            //https://stackoverflow.com/questions/39169701/how-to-extract-a-list-from-appsettings-json-in-net-core
            _filter = new DeviceAgentWriterFilter()
            {
                Include = new List<string>(),
                Exclude = new List<string>(),
                Properties = new List<string>()

            };

            

            //_filter.            

            // Bind appears not to work when one of items in source is null.
            

            _config.Bind(KEY_AGENT_EXECUTABLE_FILTER_INCLUDE, _filter.Include);
            _config.Bind(KEY_AGENT_EXECUTABLE_FILTER_EXCLUDE, _filter.Exclude);
            _config.Bind(KEY_AGENT_EXECUTABLE_FILTER_PROPERTIES, _filter.Properties);

            
            
            //_config.Bind(KEY_AGENT_EXECUTABLE_FILTER, _filter);

            if (_filter == null) _filter = new DeviceAgentWriterFilter();
        
            _filterCache = new Dictionary<string, bool>();

            // which properties are being sent with data
            this.InitializePropertiesFilter();


        }


        private void InitializePropertiesFilter()
        {
            // which properties are being sent with data
            _properties = new Dictionary<string, string>();
            foreach (var prop in typeof(ObservationData).GetProperties())
            {

                // get name for property
                string part = "";

                foreach (var a in prop.GetCustomAttributesData())
                {
                    if (a.AttributeType.Name == "JsonPropertyAttribute")
                    {
                        if (a.ConstructorArguments.Count > 0)
                        {
                            part = a.ConstructorArguments[0].Value.ToString();
                        }
                    }
                }
                if (part == "") part = prop.Name;

                bool include = false;

                if (_filter.Properties?.Count == 0)
                {
                    include = true;
                }

                foreach (var includePattern in _filter.Properties)
                {
                    if (Regex.IsMatch(part, includePattern))
                    {
                        //_logger.Debug($"Including '{part}', '{includePattern}'!!!", () => { });
                        include = true;
                        break;
                    }
                }

                // exclude tagname and value
                if (part == "tagname" || part == "value") include = false;

                // add for inclusion with properties
                if (include) _properties.Add(prop.Name, part);
            }
        }

        protected async Task<Dictionary<string, string>> GetRenameMap()
        {

            // cached version
            if (this._renameMap != null) return _renameMap;            

            // quick hack for rename. TODO: make it more elegant later
            var renamesourceuri = _config.GetValue<string>(KEY_AGENT_EXECUTABLE_TAGRENAMEURL);
            
            try
            {
                if (renamesourceuri != "")
                {
                    _logger.Info($"Device [{Agent.Name}]: getting renaming map from '{renamesourceuri}'", () => { });
                    var httphandler = new HttpClientHandler();
                    httphandler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;


                    httphandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        _logger.Debug($"Server SSL CERT: {cert.ToString()}", () => { });
                        return true;
                    };


                    using (var client = new HttpClient(httphandler))
                    {
                        var response = await client.GetAsync(new Uri(renamesourceuri));
                        if (response.IsSuccessStatusCode)
                        {
                            var body = await response.Content.ReadAsStringAsync();

                            Exception jsonException = null;
                            Exception keyvalueException = null;
                            // try to use json. If not working, try to use keyvaluepair
                            try
                            {
                                _renameMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                            } catch (Exception e) {
                                jsonException = e;
                            }

                            if (jsonException != null)
                            {
                                try
                                {
                                    string[] lines = body.Replace("\r\n", "\n").Split("\n");

                                    // Get the position of the = sign within each line
                                    var pairs = lines.
                                        Select(l => new { Line = l, Pos = l.IndexOf("=") });

                                    // Build a dictionary of key/value pairs by splitting the string at the = sign
                                    _renameMap = pairs.ToDictionary(p => p.Line.Substring(0, p.Pos), p => p.Line.Substring(p.Pos + 1));

                                } catch (Exception e)
                                {
                                    keyvalueException = e;
                                }
                            }

                            if (jsonException != null && keyvalueException != null)
                            {
                                throw new InvalidDataException($"Could not parse data retrieved!");
                            }


                        }
                        else
                        {
                            _logger.Warn($"Device [{Agent.Name}]: Unable to load resource: {response.StatusCode}:{response.ReasonPhrase}", () => { });
                        }
                    }
                                      
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Device [{Agent.Name}]:Unable to load renaming map for device {this.Agent.Name}: {e}", () => { });
            }

            if (_renameMap == null)
            {
                _renameMap = new Dictionary<string, string>();
            }

            return _renameMap;

        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {                
            }
            base.Dispose(disposing);

        }

        public override async Task Runtime(CancellationToken ct)
        {            

            var queue = this._agent.Router.GetQueue(this.Name);
            if (queue != null)
            {                
                // process queue
                while (!queue.IsEmpty)
                {
                    if (ct.IsCancellationRequested) break;
                    
                    // try to process

                    var o = queue.Peek();

                    // Expect message to contain observations..
                    if (o.Type == typeof(Observation))
                    {
                        var observation = (Observation)o.Message;

                        // Here, do filtration. And message transformation. To save data, send only filtered data tags
                        // TODO: extract generic filter logic and put into separate testable unit, write tests for it.

                        var olist = new List<ObservationData>();

                        // Get rename map...                            
                        await GetRenameMap();

                        // First include all that match, then apply exclude.
                        foreach (var record in observation.Data)
                        {

                            var tagname = record.TagName;
                            if (_renameMap.ContainsKey(record.TagName)) { record.TagName = _renameMap[record.TagName]; }
                           


                            // check if this tag name has already been matched?
                            if (!_filterCache.ContainsKey(record.TagName))
                            {
                                var taginclusion = false;

                                // search inclusion patterns
                                foreach (var includePattern in _filter.Include)
                                {
                                    if (Regex.IsMatch(record.TagName, includePattern))
                                    {
                                        taginclusion = true;
                                        break;
                                    }
                                }

                                // search exclusion patterns
                                foreach (var excludePattern in _filter.Exclude)
                                {
                                    if (Regex.IsMatch(record.TagName, excludePattern))
                                    {
                                        taginclusion = false;
                                        break;
                                    }
                                }

                                _filterCache.Add(record.TagName, taginclusion);
                            }

                            // if tag is to be included, then add it.
                            if (_filterCache[record.TagName])
                            {                                                                                                     
                                olist.Add(record);

                                

                            }
                        }

                        observation.Data = olist;

                        // only send data if at least one observation exist
                        if (observation.Data.Count > 0)
                        {

                            // how to send properties ? As part of payload or part of message ?
                            var datadict = new Dictionary<string, dynamic>();
                            var propertydict = new Dictionary<string, Dictionary<string, dynamic>>();

                            

                            foreach (var item in observation.Data)
                            {
                                try
                                {
                                    /*
                                    var tagname = item.TagName;
                                    if (_renameMap.ContainsKey(item.TagName)) { tagname = _renameMap[item.TagName];  }
                                    {
                                        if (tagname != item.TagName)
                                            _logger.Debug($"Device [{Agent.Name}]: Renaming tag {item.TagName} to {tagname} in output.", () => { });

                                        if (datadict.ContainsKey(tagname))
                                        {
                                            datadict[tagname] = item.Value;
                                            _logger.Warn($"Overwriting existing target tag {tagname} with value from {item.TagName}.", () => { });
                                        } else
                                        {
                                            datadict.Add(tagname, item.Value);
                                        }
                                    }
                                    */
                                    // What do do in case of duplicates? And how to format tagname, it must conform to json key requirements.

                                    if (datadict.ContainsKey(item.TagName))
                                    {
                                        datadict[item.TagName] = item.Value;
                                        _logger.Warn($"Duplicate tagnames, overwriting.. {item.TagName}.", () => { });
                                    }
                                    else
                                    {
                                        datadict.Add(item.TagName, item.Value);
                                    }

                                    //datadict.Add(item.TagName, item.Value);

                                    var addprops = new Dictionary<string, dynamic>();

                                    foreach (var prop in item.GetType().GetProperties())
                                    {
                                        if (_properties.ContainsKey(prop.Name)) addprops.Add(_properties[prop.Name], prop.GetValue(item));
                                    }

                                    if (addprops.Count > 0) propertydict.Add(item.TagName, addprops);

                                } catch (ArgumentException e)
                                {
                                    _logger.Warn($"Cannot add tagname '{item.TagName}' multiple times. Skipping '{item.TagName}' with value '{item.Value}'", () => { });
                                }
                            }

                            var obj = new
                            {
                                deviceid = observation.DeviceId,
                                timestamp = observation.Timestamp,
                                location = observation.GeoPositionPoint,
                                data = datadict,
                                properties = propertydict

                            };

                            //observation.Data = olist;
                            //var js = JsonConvert.SerializeObject(observation);
                            var js = JsonConvert.SerializeObject(obj);
                            _logger.Debug($"Sending: {js}", () => { });
                            var data = Encoding.UTF8.GetBytes(js);
                            await _writer.SendOutboundAsync(data, "application/json", "utf-8", null);

                        } else
                        {
                            _logger.Warn($"All observation data filtered out, dropping message.", () => { });
                        }
                       
                    } else
                    {
                        _logger.Warn($"Received message with type '{o.Type.Name}', don't know how to handle, dropping message", () => { });
                    }

                    queue.Dequeue();
                }         
            }
            
        }

    }
}
