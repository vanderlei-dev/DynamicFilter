using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicFilter.Main
{
    public class Filter
    {
        public List<FilterItem> Filters { get; private set; } = new List<FilterItem>();

        public Filter()
        {

        }

        public Filter(string fieldName, FilterType type, object value)
        {
            this[fieldName, type] = value;            
        }

        public int Count { get { return Filters.Count; } }

        public Filter Add(string fieldName, FilterType type, object value)
        {
            this[fieldName, type] = value;
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
            return Filters.Exists(f => f.Field.Equals(fieldName));
        }

        public void Clear()
        {
            Filters.Clear();
        }

        public Func<T, bool> CreateFilter<T>()
        {
            var properties = typeof(T)
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(p => p.CanRead)
                            .ToArray();

            var expressions = new List<Expression<Func<T, bool>>>();

            foreach (var item in Filters)
            {
                var prop = properties.FirstOrDefault(p => p.Name.Equals(item.Field));
                if (prop != null)
                    expressions.Add(CreateExpression<T>(prop, item.Type, item.Value));
            }

            return Compile(expressions.ToArray());
        }

        public Func<T, bool> Compile<T>(Expression<Func<T, bool>>[] expressions)
        {
            Expression<Func<T, bool>> condicoes = expressions[0];

            for (int i = 1; i < expressions.Count(); i++)
            {
                condicoes = condicoes.And(expressions[i]);
            }

            return condicoes.Compile();            
        }

        public object this[string fieldName, FilterType type = FilterType.Equal]
        {
            get { return Filters.FirstOrDefault(f => f.Field.Equals(fieldName)); }
            set { Filters.Add(new FilterItem() { Field = fieldName, Type = type, Value = value }); }
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
