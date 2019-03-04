using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using DeviceReader.Models;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.IO;

namespace DeviceReader.Tests.Models
{
    public class ObservationExtensionsTests    
    {
        private readonly ITestOutputHelper _output;
        public ObservationExtensionsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private Observation GetObservation()
        {
            return new Observation
            {
                DeviceId = "test",
                Data = new List<ObservationData>
                {
                   new ObservationData {
                      TagName = "first",
                      Value = 1
                   },
                   new ObservationData {
                      TagName = "second",
                      Value = 2
                   },
                   new ObservationData {
                      TagName = "third",
                      Value = "c"
                   }
                }
            };
        }

        private Observation GetObservation2()
        {
            return new Observation
            {
                DeviceId = "test",
                Data = new List<ObservationData>
                {
                   new ObservationData {
                      TagName = "TA.MEAN.PT1M.HMP155_1",
                      Value = 1.0
                   },
                   new ObservationData {
                      TagName = "VIS.MEAN.PT10M.PWD12_1",
                      Value = 2000.0
                   }
                }
            };
        }


        [Fact]
        public void ObservationExtensions_TestCommentsRemoval()
        {
            var input = "line a=line a\nline b=line b\n# line with comment\nline c=line c";
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1+"="+p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            Assert.DoesNotContain("#", output);
        }

        [Fact]
        public void ObservationExtensions_TestCommentsRemovalCommentIsFirst()
        {
            var input = @"#rws0031 (Kernu)
PRF.VALUE.PT30S.PWD12_1 = precipitation_intensity
PW.MEAN.PT15M.PWD12_1 = present_weather
RS.VALUE.PT30S.PWD12_1 = precipitation_type
SST.VALUE.PT1M.DRS511_1 = surface_state_lane2
TA.MEAN.PT1M.HMP155_1 = air_temp
TD.MEAN.PT1M.SYSSTATUS_1 = dewpoint
TSURF.VALUE.PT1M.DRS511_1 = road_temp_lane2
WD.MEAN.PT10M.WMT700_1 = wind_dir
VIS.MEAN.PT10M.PWD12_1 = visibility
WLT.VALUE.PT1M.DRS511_1 = water_layer_thickness_lane2
WS.MAXIMUM.PT10M.WMT700_1 = wind_speed_max
WS.MEAN.PT10M.WMT700_1 = wind_speed
";


            string[] lines = input.Replace("\r\n", "\n").Split("\n");
            
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            Assert.DoesNotContain("#", output);
            
        }

        [Fact]
        public void ObservationExtensions_TestEmptyRowsRemoval()
        {
            var input = "line a=line a\nline b\n# line with comment\nline c=line c";
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            Assert.DoesNotContain("line b", output);
        }


        [Fact]
        public void ObservationExtensions_TestIgnore()
        {
            var o = GetObservation();
            var input = "line a=line a\nline b\n# line with comment\nline c=line c\na=";
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            o.RenameTags(x);
            Assert.Equal(3, o.Data.Count);

        }

        [Fact]
        public void ObservationExtensions_TestRemove()
        {
            var o = GetObservation();
            var input = "#removing first observation\nfirst=";
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            o.RenameTags(x);
            Assert.Equal(2, o.Data.Count);

            var str = String.Join(",", o.Data.Select(t => t.TagName).ToArray());
            _output.WriteLine(str);
            Assert.Equal("second,third", str);
        }

        [Fact]
        public void ObservationExtensions_TestRename()
        {
            var o = GetObservation();
            var input = "#rename first observation\nfirst=esimene";
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            o.RenameTags(x);
            Assert.Equal(3, o.Data.Count);

            var str = String.Join(",", o.Data.Select(t => t.TagName).ToArray());
            _output.WriteLine(str);
            Assert.Equal("esimene,second,third", str);
        }

        [Fact]
        public void ObservationExtensions_TestClone()
        {
            var o = GetObservation();
            var input = "#clone first observation to new \nfourth=first";
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            o.RenameTags(x);
            Assert.Equal(4, o.Data.Count);

            var str = String.Join(",", o.Data.Select(t => t.TagName).ToArray());
            _output.WriteLine(str);
            Assert.Equal("first,second,third,fourth", str);

            var str2 = String.Join(",", o.Data.Select(t => $"{t.Value}").ToArray());
            _output.WriteLine(str2);
            Assert.Equal("1,2,c,1", str2);
        }

        [Fact]
        public void ObservationExtensions_TestReplace()
        {
            var o = GetObservation();
            var input = "#replace first observation with third\nfirst=third";
            var x = ObservationExtensions.GetRenameList(input);
            var keys = x.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);
            o.RenameTags(x);
            Assert.Equal(3, o.Data.Count);

            var str = String.Join(",", o.Data.Select(t => t.TagName).ToArray());
            _output.WriteLine(str);
            Assert.Equal("third,second,third", str);

            var str2 = String.Join(",", o.Data.Select(t => $"{t.Value}").ToArray());
            _output.WriteLine(str2);
            Assert.Equal("c,2,c", str2);
        }

        [Fact]
        public void ObservationExtensions_TestGetRenameList()
        {
            var renamesourceuri = "https://mntiotroadweather4config.file.core.windows.net/config/rws0031.txt?sv=2018-03-28&si=public&sr=f&sig=KLmmz5fm5z8YusOJ%2F79SCjv3GZh7IpjTGmVT83TBbgs%3D";
            IList<Tuple<string, string>> _renameList = new List<Tuple<string,string>>();
            try
            {
                if (renamesourceuri != "")
                {
                    
                    var httphandler = new HttpClientHandler();
                    httphandler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;


                    httphandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        
                        return true;
                    };


                    using (var client = new HttpClient(httphandler))
                    {
                        var response = client.GetAsync(new Uri(renamesourceuri)).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            var body = response.Content.ReadAsStringAsync().Result;

                            _output.WriteLine(body);
                            Exception keyvalueException = null;
                            // try to use json. If not working, try to use keyvaluepair

                            try
                            {
                                _renameList = ObservationExtensions.GetRenameList(body);
                                /*
                                string[] lines = body.Replace("\r\n", "\n").Split("\n");



                                // Get the position of the = sign within each line
                                _renameList = lines.
                                    Where(l => l.Trim().First() != '#'). // exclude comments
                                    Select(l =>
                                    {
                                        var p = l.Split("=", 2);
                                        var key = p[0].Trim();
                                        string value = "";
                                        if (p.Length > 1) { value = p[1].Trim(); }
                                        return new Tuple<string, string>(key, value);
                                    }).Where(t => (
                                        t.Item1 != "" && t.Item2 != ""  // exclude empty rows
                                        )
                                    ).ToList();                                    
                                */
                            }
                            catch (Exception e)
                            {
                                keyvalueException = e;
                            }

                            if (keyvalueException != null)
                            {
                                throw new InvalidDataException($"Could not parse data retrieved! Message: ${keyvalueException}");
                            }


                        }
                        else
                        {
                            _output.WriteLine($"Device [Agent.Name]: Unable to load resource: {response.StatusCode}:{response.ReasonPhrase}");
                        }
                    }

                }
            }
            catch (Exception e)
            {
                _output.WriteLine($"Device [Agent.Name]:Unable to load renaming map for device this.Agent.Name: {e}");
            }

            foreach (var item in _renameList)
            {
                _output.WriteLine($"{item.Item1}={item.Item2}");
            }

            var o = GetObservation2();
            
            var keys = _renameList.Select(p => p.Item1 + "=" + p.Item2);
            var output = String.Join("\n", keys);
            _output.WriteLine(output);

            o.RenameTags(_renameList);
            Assert.Equal(2, o.Data.Count);

            var str = String.Join(",", o.Data.Select(t => t.TagName).ToArray());
            _output.WriteLine(str);
            Assert.Equal("air_temp,visibility", str);

        }
    }
}
