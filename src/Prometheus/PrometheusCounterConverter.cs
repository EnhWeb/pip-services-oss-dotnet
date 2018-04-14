using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PipServices.Commons.Convert;
using PipServices.Commons.Count;

namespace PipServices.Oss.Prometheus
{
    public static class PrometheusCounterConverter
    {
        public static string ToString(IEnumerable<Counter> counters, string source, string instance)
        {
            if (counters == null) return string.Empty;

            var builder = new StringBuilder();

            foreach (var counter in counters)
            {
                var counterName = ParseCounterName(counter);
                var labels = GenerateCounterLabel(counter, source, instance);

                switch (counter.Type)
                {
                    case CounterType.Increment:
                        builder.Append("# TYPE ").Append(counterName).Append(" gauge\n");
                        builder.Append(counterName).Append(labels).Append(" ").Append(StringConverter.ToString(counter.Count)).Append("\n");
                        break;
                    case CounterType.Interval:
                        builder.Append("# TYPE ").Append(counterName).Append("_max gauge\n");
                        builder.Append(counterName).Append("_max").Append(labels).Append(" ").Append(StringConverter.ToString(counter.Max))
                            .Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_min gauge\n");
                        builder.Append(counterName).Append("_min").Append(labels).Append(" ").Append(StringConverter.ToString(counter.Min))
                            .Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_average gauge\n");
                        builder.Append(counterName).Append("_average").Append(labels).Append(" ")
                            .Append(StringConverter.ToString(counter.Average)).Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_count gauge\n");
                        builder.Append(counterName).Append("_count").Append(labels).Append(" ").Append(StringConverter.ToString(counter.Count))
                            .Append("\n");
                        break;
                    case CounterType.LastValue:
                        builder.Append("# TYPE ").Append(counterName).Append(" gauge\n");
                        builder.Append(counterName).Append(labels).Append(" ").Append(StringConverter.ToString(counter.Last))
                            .Append("\n");
                        break;
                    case CounterType.Statistics:
                        builder.Append("# TYPE ").Append(counterName).Append("_max gauge\n");
                        builder.Append(counterName).Append("_max").Append(labels).Append(" ").Append(StringConverter.ToString(counter.Max))
                            .Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_min gauge\n");
                        builder.Append(counterName).Append("_min").Append(labels).Append(" ").Append(StringConverter.ToString(counter.Min))
                            .Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_average gauge\n");
                        builder.Append(counterName).Append("_average").Append(labels).Append(" ")
                            .Append(StringConverter.ToString(counter.Average)).Append("\n");
                        builder.Append("# TYPE ").Append(counterName).Append("_count gauge\n");
                        builder.Append(counterName).Append("_count").Append(labels).Append(" ").Append(StringConverter.ToString(counter.Count))
                            .Append("\n");
                        break;
                        //case CounterType.Timestamp: // Prometheus doesn't support non-numeric metrics
                        //builder.Append("# TYPE ").Append(counterName).Append(" untyped\n");
                        //builder.Append(counterName).Append(" ").Append(StringConverter.ToString(counter.Time)).Append("\n");
                        //break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts a collection of labels into something we can insert into the output to prometheus
        /// </summary>
        /// <param name="counter">The counter for which we are generating labels</param>
        /// <param name="source">The source that generated the counter (usually app)</param>
        /// <param name="instance">Typically the machine that generated the counter</param>
        /// <returns>A string presentation of the labels</returns>
        private static string GenerateCounterLabel(Counter counter, string source, string instance)
        {
            var labels = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(source)) labels["source"] = source;
            if (!string.IsNullOrEmpty(instance)) labels["instance"] = instance;

            var nameParts = counter.Name.Split('.');

            // If there are other predictable names from which we can parse labels, we can add them below
            if (nameParts.Length >= 3 && nameParts[2] == "exec_time")
            {
                labels["service"] = nameParts[0];
                labels["command"] = nameParts[1];
            }

            if (!labels.Any()) return string.Empty;
            var builder = new StringBuilder();

            builder.Append("{");
            foreach (var label in labels)
            {
                builder.Append($"{label.Key}=\"{label.Value}\",");
            }

            builder.Remove(builder.Length - 1, 1);
            builder.Append("}");
            return builder.ToString();
        }

        /// <summary>
        /// Evaluates the counter name and returns the name to use and any potential labels
        /// </summary>
        /// <param name="counter">The counter to evaluate</param>
        /// <returns>The name of the counter to publish</returns>
        // TODO: split into two methods rather than use out param
        private static string ParseCounterName(Counter counter)
        {
            if (string.IsNullOrEmpty(counter.Name)) return string.Empty;

            var nameParts = counter.Name.Split('.');

            // If there are other predictable names from which we can parse labels, we can add them below
            if (nameParts.Length >= 3 && nameParts[2] == "exec_time")
            {
                return nameParts[2];
            }

            // TODO: are there other assumptions we can make?
            // Or just return as a single, valid name
            return counter.Name.ToLowerInvariant()
                .Replace(".", "_").Replace("/", "_");
        }

        /// <summary>
        /// Returns any potential labels
        /// </summary>
        /// <param name="counter">The counter to evaluate</param>
        /// <param name="instance">Typically the machine this is running on</param>
        /// <param name="source">Typically the application</param>
        /// <returns>An IDictionary populated with any labels to use with the counter</returns>
        // TODO: split into two methods rather than use out param
        private static IDictionary<string, string> ParseCounterLabels(Counter counter, string source, string instance)
        {
            var labels = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(source)) labels["source"] = source;
            if (!string.IsNullOrEmpty(instance)) labels["instance"] = instance;

            var nameParts = counter.Name.Split('.');

            // If there are other predictable names from which we can parse labels, we can add them below
            if (nameParts.Length >= 3 && nameParts[2] == "exec_time")
            {
                labels["service"] = nameParts[0];
                labels["command"] = nameParts[1];
            }

            return labels;
        }
    }
}
