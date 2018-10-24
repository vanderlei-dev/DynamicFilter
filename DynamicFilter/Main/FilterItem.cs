using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DynamicFilter.Main
{
    public class FilterItem
    {
        public FilterOperator Operator { get; set; } = FilterOperator.And;
        public string Field { get; set; }
        public FilterType Type { get; set; }
        public object Value { get; set; }

        public List<FilterItem> Filters { get; set; } = new List<FilterItem>();

        public FilterItem AndAlso(string fieldName, FilterType type, object value)
        {
            var item = new FilterItem() { Field = fieldName, Type = type, Value = value, Operator = FilterOperator.And };
            Filters.Add(item);
            return item;
        }

        public FilterItem AndAlso<T>(Expression<Func<T, object>> propertySelector, FilterType type, object value)
        {
            string fieldName = propertySelector.GetProperty().Name;
            var item = new FilterItem() { Field = fieldName, Type = type, Value = value, Operator = FilterOperator.And };
            Filters.Add(item);
            return item;
        }

        public FilterItem Or(string fieldName, FilterType type, object value)
        {
            var item = new FilterItem() { Field = fieldName, Type = type, Value = value, Operator = FilterOperator.Or };
            Filters.Add(item);
            return item;
        }
    }
}
