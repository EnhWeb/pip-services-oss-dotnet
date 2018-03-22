using PipServices.Commons.Config;

using Xunit;

namespace PipServices.Commons.Test.Config
{
    //[TestClass]
    public class JsonConfigReaderTest
    {
        [Fact]
        public void TestReadConfig()
        {
            var parameters = ConfigParams.FromTuples(
                "param1", "Test Param 1",
                "param2", "Test Param 2");

            ConfigParams config = JsonConfigReader.ReadConfig(null, "../../../../data/config.json", parameters);

            Assert.Equal(9, config.Count);
            Assert.Equal(123, config.GetAsInteger("field1.field11"));
            Assert.Equal("ABC", config.GetAsString("field1.field12"));
            Assert.Equal(123, config.GetAsInteger("field2.0"));
            Assert.Equal("ABC", config.GetAsString("field2.1"));
            Assert.Equal(543, config.GetAsInteger("field2.2.field21"));
            Assert.Equal("XYZ", config.GetAsString("field2.2.field22"));
            Assert.Equal(true, config.GetAsBoolean("field3"));
        }
    }
}
