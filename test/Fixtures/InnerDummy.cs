using System.Collections.Generic;

namespace PipServices.Oss.Fixtures
{
    public class InnerDummy
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<InnerDummy> InnerInnerDummies { get; set; } = new List<InnerDummy>();
    }
}