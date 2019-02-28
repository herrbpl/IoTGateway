using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using DeviceReader.Models;
using System.Linq;

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
    }
}
