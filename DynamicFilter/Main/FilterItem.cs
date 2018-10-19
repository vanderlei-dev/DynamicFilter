using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicFilter.Main
{
    public class FilterItem
    {
        public string Field { get; set; }
        public FilterType Type { get; set; }
        public object Value { get; set; }
    }
}
