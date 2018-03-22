using System;
using System.Collections.Generic;
using HandlebarsDotNet;
using Xunit;

namespace PipServices.Commons.Config
{
    public class ConfigReaderTest
    {
        [Fact]
        public void TestParameterize()
        {
            var config = "{{#if A}}{{B}}{{/if}}";
            var parameters = new Dictionary<string, string>() { { "A", "true" }, { "B", "XYZ" } };

            var template = Handlebars.Compile(config);

            var result = template(parameters);       
            Assert.Equal("XYZ", result);
        }
    }
}
