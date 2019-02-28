using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using DeviceReader.Services;
using System.Threading.Tasks;

namespace DeviceReader.Tests.Services
{
    public class rString
    {
        public string Value { get; set; }
    }

    public class TransformTests
    {
        private readonly ITestOutputHelper _output;
        public TransformTests(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public void TransformBuilder_TestBuilder()
        {
            var tfb = new TransformBuilder<rString>();

            tfb.Use<rString>(async (context, next) => {
                _output.WriteLine("transform A starts");
                context.Value = context.Value + "[A]";
                if (next != null) await next();
                _output.WriteLine("transform A ends");
                return;
            }
            );

            var i = tfb.Build();
            rString x = new rString { Value = "input" };
            i.Invoke(x);
            Assert.Equal("input[A]", x.Value);

            tfb.Use<rString>(async (context, next) =>
            {
                _output.WriteLine("transform B starts");
                context.Value = context.Value + "[B]";
                if (next != null) await next();
                _output.WriteLine("transform B ends");
                return;
            }
            );

            var j = tfb.Build();
            rString y = new rString { Value = "input" };
            j.Invoke(y);
            Assert.Equal("input[A][B]", y.Value);

        }

    }
}
