using System;
using System.Collections.Generic;
using PipServices.Commons.Count;
using Xunit;

namespace PipServices.Oss.Prometheus
{
    public sealed class PrometheusCounterConverterTest
    {
        [Fact]
        public void EmptyCounters()
        {
            var counters = new List<Counter>();
            var body = PrometheusCounterConverter.ToString(counters, string.Empty, string.Empty);
            Assert.Equal(string.Empty, body);
        }

        [Fact]
        public void NullValues()
        {
            var body = PrometheusCounterConverter.ToString(null, null, null);
            Assert.Equal(string.Empty, body);
        }

        [Fact]
        public void SingleIncrement_NoLabels()
        {
            var counters = new List<Counter>
            {
                new Counter("MyCounter", CounterType.Increment)
                {
                    Average = 2,
                    Min = 1,
                    Max = 3,
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, null, null);
            const string expected = "# TYPE mycounter gauge\nmycounter 2\n";
            Assert.Equal(expected, body);
        }

        [Fact]
        public void SingleIncrement_SourceInstance()
        {
            var counters = new List<Counter>
            {
                new Counter("MyCounter", CounterType.Increment)
                {
                    Average = 2,
                    Min = 1,
                    Max = 3,
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, "MyApp", "MyInstance");
            const string expected = "# TYPE mycounter gauge\nmycounter{source=\"MyApp\",instance=\"MyInstance\"} 2\n";
            Assert.Equal(expected, body);
        }

        [Fact]
        public void MultiIncrement_SourceInstance()
        {
            var counters = new List<Counter>
            {
                new Counter("MyCounter1", CounterType.Increment)
                {
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                },
                new Counter("MyCounter2", CounterType.Increment)
                {
                    Count = 5,
                    Last = 10,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, "MyApp", "MyInstance");
            const string expected = "# TYPE mycounter1 gauge\nmycounter1{source=\"MyApp\",instance=\"MyInstance\"} 2\n"
                                  + "# TYPE mycounter2 gauge\nmycounter2{source=\"MyApp\",instance=\"MyInstance\"} 5\n";
            Assert.Equal(expected, body);
        }

        [Fact]
        public void MultiIncrement_ExecWithOnlyTwo_SourceInstance()
        {
            var counters = new List<Counter>
            {
                new Counter("MyCounter1.exec_time", CounterType.Increment)
                {
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                },
                new Counter("MyCounter2.exec_time", CounterType.Increment)
                {
                    Count = 5,
                    Last = 10,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, "MyApp", "MyInstance");
            const string expected = "# TYPE mycounter1_exec_time gauge\nmycounter1_exec_time{source=\"MyApp\",instance=\"MyInstance\"} 2\n"
                                    + "# TYPE mycounter2_exec_time gauge\nmycounter2_exec_time{source=\"MyApp\",instance=\"MyInstance\"} 5\n";
            Assert.Equal(expected, body);
        }

        [Fact]
        public void MultiIncrement_Exec_SourceInstance()
        {
            var counters = new List<Counter>
            {
                new Counter("MyService1.MyCommand1.exec_time", CounterType.Increment)
                {
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                },
                new Counter("MyService2.MyCommand2.exec_time", CounterType.Increment)
                {
                    Count = 5,
                    Last = 10,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, "MyApp", "MyInstance");
            const string expected = "# TYPE exec_time gauge\nexec_time{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 2\n"
                                  + "# TYPE exec_time gauge\nexec_time{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 5\n";
            Assert.Equal(expected, body);
        }

        [Fact]
        public void MultiInterval_Exec_SourceInstance()
        {
            var counters = new List<Counter>
            {
                new Counter("MyService1.MyCommand1.exec_time", CounterType.Interval)
                {
                    Min = 1,
                    Max = 3,
                    Average = 2,
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                },
                new Counter("MyService2.MyCommand2.exec_time", CounterType.Interval)
                {
                    Min = 2,
                    Max = 4,
                    Average = 3,
                    Count = 5,
                    Last = 10,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, "MyApp", "MyInstance");

            const string expected =
                "# TYPE exec_time_max gauge\nexec_time_max{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 3\n"
                + "# TYPE exec_time_min gauge\nexec_time_min{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 1\n"
                + "# TYPE exec_time_average gauge\nexec_time_average{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 2\n"
                + "# TYPE exec_time_count gauge\nexec_time_count{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 2\n"
                + "# TYPE exec_time_max gauge\nexec_time_max{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 4\n"
                + "# TYPE exec_time_min gauge\nexec_time_min{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 2\n"
                + "# TYPE exec_time_average gauge\nexec_time_average{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 3\n"
                + "# TYPE exec_time_count gauge\nexec_time_count{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 5\n";
            Assert.Equal(expected, body);
        }

        [Fact]
        public void MultiStatistics_Exec_SourceInstance()
        {
            var counters = new List<Counter>
            {
                new Counter("MyService1.MyCommand1.exec_time", CounterType.Statistics)
                {
                    Min = 1,
                    Max = 3,
                    Average = 2,
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                },
                new Counter("MyService2.MyCommand2.exec_time", CounterType.Statistics)
                {
                    Min = 2,
                    Max = 4,
                    Average = 3,
                    Count = 5,
                    Last = 10,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, "MyApp", "MyInstance");

            const string expected =
                "# TYPE exec_time_max gauge\nexec_time_max{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 3\n"
                + "# TYPE exec_time_min gauge\nexec_time_min{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 1\n"
                + "# TYPE exec_time_average gauge\nexec_time_average{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 2\n"
                + "# TYPE exec_time_count gauge\nexec_time_count{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 2\n"
                + "# TYPE exec_time_max gauge\nexec_time_max{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 4\n"
                + "# TYPE exec_time_min gauge\nexec_time_min{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 2\n"
                + "# TYPE exec_time_average gauge\nexec_time_average{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 3\n"
                + "# TYPE exec_time_count gauge\nexec_time_count{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 5\n";
            Assert.Equal(expected, body);
        }

        [Fact]
        public void MultiLastValue_Exec_SourceInstance()
        {
            var counters = new List<Counter>
            {
                new Counter("MyService1.MyCommand1.exec_time", CounterType.LastValue)
                {
                    Count = 2,
                    Last = 3,
                    Time = DateTime.MinValue
                },
                new Counter("MyService2.MyCommand2.exec_time", CounterType.LastValue)
                {
                    Count = 5,
                    Last = 10,
                    Time = DateTime.MinValue
                }
            };
            var body = PrometheusCounterConverter.ToString(counters, "MyApp", "MyInstance");
            const string expected = "# TYPE exec_time gauge\nexec_time{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService1\",command=\"MyCommand1\"} 3\n"
                                    + "# TYPE exec_time gauge\nexec_time{source=\"MyApp\",instance=\"MyInstance\",service=\"MyService2\",command=\"MyCommand2\"} 10\n";
            Assert.Equal(expected, body);
        }
    }
}
