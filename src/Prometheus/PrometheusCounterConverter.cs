using System;
using System.Collections.Generic;
using System.Text;
using PipServices.Commons.Convert;
using PipServices.Commons.Count;

namespace PipServices.Oss.Prometheus
{
    public static class PrometheusCounterConverter
    {
        public static string ToString(IEnumerable<Counter> counters)
        {
            if (counters == null) return string.Empty;

            var builder = new StringBuilder();

            foreach (var counter in counters)
            {
                var counterName = counter.Name.ToLowerInvariant().Replace(".", "_");

                switch (counter.Type)
                {
                    case CounterType.Increment:                        
                        builder.Append("# TYPE ").Append(counterName).Append(" gauge\n");
                        builder.Append(counterName).Append(" ").Append(StringConverter.ToString(counter.Count)).Append("\n");
                        break;
                    case CounterType.Interval:
                        builder.Append("# TYPE ").Append(counterName).Append("_max gauge\n");
                        builder.Append(counterName).Append("_max ").Append(StringConverter.ToString(counter.Max)).Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_min gauge\n");
                        builder.Append(counterName).Append("_min ").Append(StringConverter.ToString(counter.Max)).Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_average gauge\n");
                        builder.Append(counterName).Append("_average ").Append(StringConverter.ToString(counter.Average)).Append("\n");
                        break;
                    case CounterType.LastValue:
                        builder.Append("# TYPE ").Append(counterName).Append(" gauge\n");
                        builder.Append(counterName).Append(" ").Append(StringConverter.ToString(counter.Last)).Append("\n");
                        break;
                    case CounterType.Statistics:
                        builder.Append("# TYPE ").Append(counterName).Append("_max gauge\n");
                        builder.Append(counterName).Append("_max ").Append(StringConverter.ToString(counter.Max)).Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_min gauge\n");
                        builder.Append(counterName).Append("_min ").Append(StringConverter.ToString(counter.Max)).Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_average gauge\n");
                        builder.Append(counterName).Append("_average ").Append(StringConverter.ToString(counter.Average)).Append("\n");
                        break;
                    //case CounterType.Timestamp: // Prometheus doesn't support non-numeric metrics
                        //builder.Append("# TYPE ").Append(counterName).Append(" untyped\n");
                        //builder.Append(counterName).Append(" ").Append(StringConverter.ToString(counter.Time)).Append("\n");
                        //break;
                }
            }

            return builder.ToString();
        }
    }
}
