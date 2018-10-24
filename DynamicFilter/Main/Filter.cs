using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicFilter.Main
{
    public class Filter
    {        
        public string FilterText { get; private set; }

        public List<FilterItem> Filters { get; set; } = new List<FilterItem>();

        public Filter()
        {

        }        

        public FilterItem Add(string fieldName, FilterType type, object value)
        {
            var item = new FilterItem() { Field = fieldName, Type = type, Value = value, Operator = FilterOperator.And };
            Filters.Add(item);
            return item;
        }

        public FilterItem Add<T>(Expression<Func<T, object>> propertySelector, FilterType type, object value)
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
                {
                    switch (item.Operator)
                    {
                        case FilterOperator.And:
                            expressions.Add(CreateExpression<T>(prop, item.Type, item.Value));
                            break;
                        case FilterOperator.Or:
                            var joinExpression = Join(expressions.ToArray());
                            joinExpression = joinExpression.Or(CreateExpression<T>(prop, item.Type, item.Value));
                            expressions.Clear();
                            expressions.Add(joinExpression);
                            break;
                        default:
                            break;
                    }                    
                }

                foreach (var subItem in item.Filters)
                {
                    var subProp = properties.FirstOrDefault(p => p.Name.Equals(subItem.Field));
                    if (subProp != null)
                    {
                        switch (subItem.Operator)
                        {
                            case FilterOperator.And:
                                expressions.Add(CreateExpression<T>(subProp, subItem.Type, subItem.Value));
                                break;
                            case FilterOperator.Or:
                                var joinExpression = Join(expressions.ToArray());
                                joinExpression = joinExpression.Or(CreateExpression<T>(subProp, subItem.Type, subItem.Value));
                                expressions.Clear();
                                expressions.Add(joinExpression);
                                break;
                            default:
                                break;
                        }                        
                    }
                }                
            }

            var mainExpression = Join(expressions.ToArray());                        
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
