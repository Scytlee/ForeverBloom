using System.Linq.Expressions;
using ForeverBloom.Api.Models;

namespace ForeverBloom.Api.Extensions;

public static class SortingExtensions
{
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, SortCriterion[] sortColumns, Dictionary<string, Expression<Func<T, object>>>? propertyMapping = null)
    {
        if (!sortColumns.Any())
        {
            return query;
        }

        var isFirst = true;

        foreach (var sortCriterion in sortColumns)
        {
            LambdaExpression lambda;

            if (propertyMapping?.TryGetValue(sortCriterion.PropertyName, out var mappedExpression) == true)
            {
                // Convert the mapped expression to LambdaExpression and rebuild without object boxing
                var parameter = mappedExpression.Parameters[0];
                var body = mappedExpression.Body;

                // Remove the Convert to object if present (unboxing)
                if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary && unary.Type == typeof(object))
                {
                    body = unary.Operand;
                }

                lambda = Expression.Lambda(body, parameter);
            }
            else
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, sortCriterion.PropertyName);
                lambda = Expression.Lambda(property, parameter);
            }

            var methodName = isFirst
                ? sortCriterion.Direction == "asc" ? "OrderBy" : "OrderByDescending"
                : sortCriterion.Direction == "asc" ? "ThenBy" : "ThenByDescending";

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), lambda.ReturnType],
                query.Expression,
                lambda);

            query = query.Provider.CreateQuery<T>(resultExpression);
            isFirst = false;
        }

        return query;
    }
}
