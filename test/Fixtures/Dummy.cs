using System;
using PipServices.Commons.Data;

namespace PipServices.Oss.Fixtures
{
    public class Dummy : IStringIdentifiable
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Content { get; set; }
    }
}
