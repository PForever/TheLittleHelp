using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace TheLittleHelp.System.ExpressionHelp
{
    public static class TheLittleExpressionHelper
    {
        class ParameterVisitor : ExpressionVisitor
        {
            //private static Expression SymbolConditionsOrDefaultr<TEntity, TValue>(Expression<Func<TEntity, TValue>> property, IList<TValue> node, int elementIndex, int nodeIndex, int propertyCount)
            //{
            //    if (nodeIndex >= node.Count || elementIndex > propertyCount) return null;
            //    var charter = Expression.Constant(node[nodeIndex]);

            //    var item = Expression.ArrayIndex(property, Expression.Constant(elementIndex));
            //    var eq = Expression.Equal(item, charter);

            //    Expression subBranches = null;
            //    int nextIndex = ++nodeIndex;
            //    do
            //    {
            //        var innerExpression = SymbolConditionsOrDefaultr(property, node, ++elementIndex, nextIndex, propertyCount);
            //        if (innerExpression == null) break;

            //        var subBranch = Expression.AndAlso(eq, innerExpression);

            //        subBranches = subBranches == null ? subBranch : Expression.OrElse(subBranches, subBranch);
            //    } while (elementIndex < propertyCount);

            //    return subBranches;
            //}

            private readonly ReadOnlyCollection<ParameterExpression> from, to;
            public ParameterVisitor(ReadOnlyCollection<ParameterExpression> from, ReadOnlyCollection<ParameterExpression> to)
            {
                if (from == null) throw new ArgumentNullException("from");
                if (to == null) throw new ArgumentNullException("to");
                if (from.Count != to.Count) throw new InvalidOperationException(
                    "Parameter lengths must match");
                this.from = from;
                this.to = to;
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                for (int i = 0; i < from.Count; i++)
                {
                    if (node == from[i]) return to[i];
                }
                return node;
            }
        }
        public static Expression<Func<T, bool>> AndAlso<T>(
            Expression<Func<T, bool>> x, Expression<Func<T, bool>> y)
        {
            var newY = new ParameterVisitor(y.Parameters, x.Parameters)
                .VisitAndConvert(y.Body, "AndAlso");
            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(x.Body, newY),
                x.Parameters);
        }
        public static LambdaExpression AndAlso(
            LambdaExpression x, LambdaExpression y)
        {
            var newY = new ParameterVisitor(y.Parameters, x.Parameters)
                .VisitAndConvert(y.Body, "AndAlso");
            return Expression.Lambda(
                Expression.AndAlso(x.Body, newY),
                x.Parameters);
        }
        public static Expression<Func<T, bool>> OrElse<T>(
            Expression<Func<T, bool>> x, Expression<Func<T, bool>> y)
        {
            var newY = new ParameterVisitor(y.Parameters, x.Parameters)
                .VisitAndConvert(y.Body, "OrElse");
            return Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(x.Body, newY),
                x.Parameters);
        }
        public static LambdaExpression OrElse(
            LambdaExpression x, LambdaExpression y)
        {
            var newY = new ParameterVisitor(y.Parameters, x.Parameters)
                .VisitAndConvert(y.Body, "OrElse");
            return Expression.Lambda(
                Expression.OrElse(x.Body, newY),
                x.Parameters);
        }
        public static Expression<Func<T, bool>> Or<T>(
            Expression<Func<T, bool>> x, Expression<Func<T, bool>> y)
        {
            var newY = new ParameterVisitor(y.Parameters, x.Parameters)
                .VisitAndConvert(y.Body, "Or");
            return Expression.Lambda<Func<T, bool>>(
                Expression.Or(x.Body, newY),
                x.Parameters);
        }
        public static LambdaExpression Or(
            LambdaExpression x, LambdaExpression y)
        {
            var newY = new ParameterVisitor(y.Parameters, x.Parameters)
                .VisitAndConvert(y.Body, "Or");
            return Expression.Lambda(
                Expression.Or(x.Body, newY),
                x.Parameters);
        }
        internal static Expression<Action<object, T>> CreateSetProperty<T>(object defaultObject, string propertyName)
        {
            var varriable = Expression.Variable(typeof(object));
            var varriableConverted = Expression.Convert(varriable, defaultObject.GetType());
            var value = Expression.Variable(typeof(T));
            var property = Expression.PropertyOrField(varriableConverted, propertyName);
            var act = Expression.Assign(property, value);
            return Expression.Lambda<Action<object, T>>(act, varriable, value);
        }
        internal static Expression<Func<object, T>> CreateGetProperty<T>(object defaultObject, string propertyName)
        {
            var varriable = Expression.Variable(typeof(object));
            var varriableConverted = Expression.Convert(varriable, defaultObject.GetType());
            Expression property = Expression.PropertyOrField(varriableConverted, propertyName);
            if (property.Type != typeof(T)) property = Expression.Convert(property, typeof(T));
            return Expression.Lambda<Func<object, T>>(property, varriable);
        }

        internal static Expression CheckNull(this Expression condition, Expression property)
        {
            return property.Type.CanBeNull() ? property.IsNotNull().And(condition) : condition;
        }
        internal static Expression IsNotNull(this Expression property)
        {
            return Expression.NotEqual(property, Expression.Constant(null, property.Type));
        }

        internal static Expression Equal(this Expression left, Expression right)
        {
            var type = left.Type;
            if (!type.IsClass) return Expression.Equal(left, right);
            if (type.GetInterface(typeof(IEquatable<>).MakeGenericType(type).Name) is null)
                return left.IsNotNull().And(Expression.Call(left, type.GetMethod(nameof(object.Equals), new[] { typeof(object) }), right));
            return left.IsNotNull().And(Expression.Call(left, type.GetMethod(nameof(IEquatable<int>.Equals), new[] { type }), right));
        }

        internal static Expression And(this Expression leftBoolean, Expression rightBoolean)
        {
            return Expression.AndAlso(leftBoolean, rightBoolean);
        }
        internal static Expression ConvertTo(this Expression expression, Type type) => Expression.Convert(expression, type);
        internal static bool CanBeNull(this Type type) => type.IsClass || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        internal static Type GetNullubleType(this object value) => value.GetType() is var type && type.CanBeNull() ? type : typeof(Nullable<>).MakeGenericType(type);
    }
}
