using System;
using System.Collections.Generic;

namespace S54VanosTester.Vanos
{
    /// <summary>One measured/reported line from the VANOS test.</summary>
    public sealed class VanosResultItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; } // e.g. "OK", "FAIL", or blank

        public VanosResultItem() { }

        public VanosResultItem(string name, string value, string unit = "", string status = "")
        {
            Name = name;
            Value = value;
            Unit = unit;
            Status = status;
        }
    }

    /// <summary>Full outcome of a VANOS test run.</summary>
    public sealed class VanosTestReport
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public bool Completed { get; set; }
        public string Summary { get; set; }
        public List<VanosResultItem> Items { get; } = new List<VanosResultItem>();
    }
}
