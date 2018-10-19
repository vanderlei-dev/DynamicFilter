using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicFilter.Main
{
    public class Filter
    {
        private int _filterCount = 0;

        public string FilterText { get; private set; }

        public FilterGroup MainFilter { get; set; } = new FilterGroup();
        public List<FilterGroup> AdditionalFilters { get; private set; } = new List<FilterGroup>();

        public Filter()
        {

        }

        public Filter(string fieldName, FilterType type, object value)
        {
            this[fieldName, type] = value;            
        }

        public int Count { get { return MainFilter.Filters.Count; } }

        public Filter Add(string fieldName, FilterType type, object value)
        {
            this[fieldName, type] = value;
            return this;
        }

        public Filter Or(string fieldName, FilterType type, object value)
        {
            _filterCount++;
            var group = new FilterGroup() { Order = _filterCount };
            group.Filters.Add(
                new FilterItem()
                {
                    Field = fieldName,
                    Type = type,
                    Value = value
                });

            AdditionalFilters.Add(group);            
            
            return this;
        }

        public Filter Add<T>(Expression<Func<T, object>> propertySelector, FilterType type, object value)
        {
            string propertyName = propertySelector.GetProperty().Name;
            this[propertyName, type] = value;
            return this;
        }

        public bool Exists(string fieldName)
        {
            return MainFilter.Filters.Exists(f => f.Field.Equals(fieldName));
        }

        public void Clear()
        {
            MainFilter.Filters.Clear();
            AdditionalFilters.Clear();
        }

        public Func<T, bool> CreateFilter<T>()
        {
            var properties = typeof(T)
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(p => p.CanRead)
                            .ToArray();

            var expressions = new List<Expression<Func<T, bool>>>();

            foreach (var item in MainFilter.Filters)
            {
                var prop = properties.FirstOrDefault(p => p.Name.Equals(item.Field));
                if (prop != null)
                    expressions.Add(CreateExpression<T>(prop, item.Type, item.Value));
            }

            var mainExpression = Join(expressions.ToArray());            

            foreach (var item in AdditionalFilters)
            {
                var subExpressions = new List<Expression<Func<T, bool>>>();

                foreach (var filter in item.Filters)
                {
                    var prop = properties.FirstOrDefault(p => p.Name.Equals(filter.Field));
                    if (prop != null)
                        subExpressions.Add(CreateExpression<T>(prop, filter.Type, filter.Value));
                }

                var subExpression = Join(subExpressions.ToArray());
                mainExpression = mainExpression.Or(subExpression);
            }

            FilterText = mainExpression.ToString();

            return mainExpression.Compile();
        }

        private static Expression<Func<T, bool>> Join<T>(Expression<Func<T, bool>>[] expressions)
        {
            Expression<Func<T, bool>> condicoes = expressions[0];

            for (int i = 1; i < expressions.Count(); i++)
            {
                condicoes = condicoes.And(expressions[i]);
            }

            return condicoes;
        }

        public object this[string fieldName, FilterType type = FilterType.Equal]
        {
            get { return MainFilter.Filters.FirstOrDefault(f => f.Field.Equals(fieldName)); }
            set { MainFilter.Filters.Add(new FilterItem() { Field = fieldName, Type = type, Value = value }); }
        }

        private static Expression<Func<T, bool>> CreateExpression<T>(PropertyInfo prop, FilterType filterType, object value)
        {
            var type = typeof(T);
            ParameterExpression parameter = Expression.Parameter(typeof(T), "obj");
            Expression left = Expression.Property(parameter, prop);
            Expression right = Expression.Constant(Convert.ChangeType(value, prop.PropertyType));

            var expression = ApplyFilter(filterType, left, right);

            return Expression.Lambda<Func<T, bool>>(expression, parameter);
        }

        private static Expression ApplyFilter(FilterType type, Expression left, Expression right)
        {
            Expression InnerLambda = null;

            switch (type)
            {
                case FilterType.Contains:
                    MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    InnerLambda = Expression.Call(left, method, right);
                    break;
                case FilterType.Equal:
                    InnerLambda = Expression.Equal(left, right);
                    break;
                case FilterType.LessThan:
                    InnerLambda = Expression.LessThan(left, right);                    
                    break;
                case FilterType.GreaterThan:
                    InnerLambda = Expression.GreaterThan(left, right);
                    break;
                case FilterType.GreaterThanOrEqual:
                    InnerLambda = Expression.GreaterThanOrEqual(left, right);
                    break;
                case FilterType.LessThanOrEqual:
                    InnerLambda = Expression.LessThanOrEqual(left, right);
                    break;
                case FilterType.NotEqual:
                    InnerLambda = Expression.NotEqual(left, right);
                    break;                                    
            }

            return InnerLambda;
        }
    }
}
