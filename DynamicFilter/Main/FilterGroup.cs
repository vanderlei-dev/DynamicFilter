using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicFilter.Main
{
    public class FilterGroup
    {
        public int Order { get; set; }
        public List<FilterItem> Filters { get; set; } = new List<FilterItem>();
    }
}
