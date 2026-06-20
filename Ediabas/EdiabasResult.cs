using System;
using System.Collections.Generic;
using System.Globalization;

namespace S54VanosTester.Ediabas
{
    /// <summary>A single named value inside an EDIABAS result set.</summary>
    public sealed class EdiabasResultValue
    {
        public string Name { get; set; }
        public int Format { get; set; }
        public object Value { get; set; }

        public string AsString()
        {
            switch (Value)
            {
                case null:
                    return string.Empty;
                case double d:
                    return d.ToString("0.###", CultureInfo.InvariantCulture);
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return Value.ToString();
            }
        }

        public bool TryGetDouble(out double value)
        {
            switch (Value)
            {
                case double d:
                    value = d;
                    return true;
                case int i:
                    value = i;
                    return true;
                case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                    value = parsed;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }
    }

    /// <summary>A single result set returned by a job (keyed by result name, case-insensitive).</summary>
    public sealed class EdiabasResultSet
    {
        private readonly Dictionary<string, EdiabasResultValue> _values =
            new Dictionary<string, EdiabasResultValue>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, EdiabasResultValue> Values => _values;

        public void Add(EdiabasResultValue value) => _values[value.Name] = value;

        public bool TryGet(string name, out EdiabasResultValue value) => _values.TryGetValue(name, out value);

        /// <summary>Convenience: read a numeric result, returning null when absent or non-numeric.</summary>
        public double? GetDouble(string name)
        {
            if (_values.TryGetValue(name, out var v) && v.TryGetDouble(out var d))
                return d;
            return null;
        }

        public string GetString(string name)
            => _values.TryGetValue(name, out var v) ? v.AsString() : null;
    }
}
