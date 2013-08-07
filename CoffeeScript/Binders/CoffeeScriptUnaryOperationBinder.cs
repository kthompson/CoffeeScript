using System.Dynamic;
using System.Linq.Expressions;

namespace CoffeeScript
{
    public class CoffeeScriptUnaryOperationBinder : UnaryOperationBinder
    {
        public CoffeeScriptUnaryOperationBinder(ExpressionType operation)
            : base(operation)
        {
        }

        public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue)
                return Defer(target);

            UnaryExpression unaryExpression = Expression.MakeUnary(Operation, Expression.Convert(target.Expression, target.LimitType), target.LimitType);

            return new DynamicMetaObject(RuntimeHelpers.EnsureObjectResult(unaryExpression),
                                         target.Restrictions.Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)));
        }
    }
}