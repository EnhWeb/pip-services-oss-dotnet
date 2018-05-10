using System;
using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using PipServices.Commons.Data;

namespace PipServices.Oss.Fixtures
{
    public enum DummyType : int
    {
        Dummy = 0,
        NotDummy
    }

    public class Dummy : IStringIdentifiable
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Content { get; set; }
        public DateTime CreateTimeUtc { get; set; }
        public InnerDummy InnerDummy { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DummyType DummyType { get; set; }

        public List<InnerDummy> InnerDummies { get; set; } = new List<InnerDummy>();
    }
}
