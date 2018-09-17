using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Security.Authentication;
using System.Net.Http.Headers;
using System.Web;
using System.Linq;

namespace DeviceReader.Protocols
{
    

    public class HttpProtocolReaderOptions
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool NoSSLValidation { get; set; } = true;
        public int TimeOut { get; set; } = 30;
    }

    /// <summary>
    /// TODO: Rename this protocol reader to VaisalaXML protocol reader. Mainly because http is more generic and might require to implement different header schemes etc for specific purposes.
    /// See some nice stuff here: https://www.thomaslevesque.com/2018/02/25/better-timeout-handling-with-httpclient/ and https://www.thomaslevesque.com/2016/12/08/fun-with-the-httpclient-pipeline/
    /// </summary>

    public class HttpProtocolReader : AbstractProtocolReader<HttpProtocolReaderOptions>
    {                
        protected HttpClientHandler _handler = null;
        protected HttpClient _client = null;

        public HttpProtocolReader(ILogger logger, string optionspath, IConfigurationRoot configroot) :base(logger, optionspath, configroot)
        {
            if (_options == null)
            {
                throw new ArgumentNullException("Unable to find options for protocolreader from config");
            }
            
            _handler = new HttpClientHandler();
            _handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

            // TODO: implement valid SSL cert checking
            if (_options.NoSSLValidation)
            {
                _handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    _logger.Debug($"Server SSL CERT: {cert.ToString()}", () => { });
                    return true;

                };
            }
        }

        /* need access to config, port, url, etc. Should give access to config? Device Agent? */
        override public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            return await ReadAsync(null, cancellationToken);
        }

        

        override public async Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            // Begin timing.
            stopwatch.Start();

            var response = await ExecuteRequestAsunc(parameters, cancellationToken);
            
            stopwatch.Stop();

            _logger.Debug($"Request took {stopwatch.ElapsedMilliseconds} ms to complete", () => { });

            

            if (response != null)
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warn($"Request completed unsusccessfully: {(int)response.StatusCode}:{response.ReasonPhrase}", () => { });
                    throw new HttpRequestException($"Request completed unsusccessfully: {(int)response.StatusCode}:{response.ReasonPhrase}");
                }
                
                if (response.Content != null)
                {
                    var content =   await response.Content.ReadAsStringAsync();
                    return content;
                }
            }
            return null;
        }

        protected async Task<HttpResponseMessage> ExecuteRequestAsunc(IDictionary<string, string> query, CancellationToken cancellationToken)
        {
           
            if (_client == null)
            {
                _logger.Debug($"Creating HttpClient", () => { });
                _client = new HttpClient(_handler);

                if (_options.TimeOut > 0) _client.Timeout = TimeSpan.FromSeconds(_options.TimeOut);

                var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _options.Username, _options.Password));
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                
            }

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();

            httpRequestMessage.Method = new HttpMethod("GET");
            UriBuilder ub = new UriBuilder(_options.Url);

            if (query != null)
            {
                ub.Query = string.Join("&", query.Keys.Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(query[key]))));
            }

            httpRequestMessage.RequestUri = ub.Uri;

            _logger.Debug($"Request URI: '{httpRequestMessage.RequestUri}'", () => { });

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            return response;
            return null;
        }

        override protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_client != null)
                    {
                        _client.CancelPendingRequests();
                        _client.Dispose();
                        _client = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

    }
}
