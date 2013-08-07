using System.Dynamic;
using System.Linq.Expressions;

namespace CoffeeScript
{
    public class CoffeeScriptBinaryOperationBinder : BinaryOperationBinder
    {
        public CoffeeScriptBinaryOperationBinder(ExpressionType operation)
            : base(operation)
        {
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue || !arg.HasValue)
            {
                return Defer(target, arg);
            }
            BindingRestrictions restrictions = target.Restrictions.Merge(arg.Restrictions)
                                                     .Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType))
                                                     .Merge(BindingRestrictions.GetTypeRestriction(arg.Expression, arg.LimitType));
            return new DynamicMetaObject(
                RuntimeHelpers.EnsureObjectResult(
                    Expression.MakeBinary(
                        Operation,
                        Expression.Convert(target.Expression, target.LimitType),
                        Expression.Convert(arg.Expression, arg.LimitType))),
                restrictions
                );
        }
    }
}