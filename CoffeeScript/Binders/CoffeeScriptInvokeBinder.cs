using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoffeeScript
{
    public class CoffeeScriptInvokeBinder : InvokeBinder
    {
        public CoffeeScriptInvokeBinder(CallInfo callinfo)
            : base(callinfo)
        {
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject targetMO, DynamicMetaObject[] argMOs, DynamicMetaObject errorSuggestion)
        {

            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!targetMO.HasValue || argMOs.Any(a => !a.HasValue))
            {
                var deferArgs = new DynamicMetaObject[argMOs.Length + 1];
                for (int i = 0; i < argMOs.Length; i++)
                {
                    deferArgs[i + 1] = argMOs[i];
                }
                deferArgs[0] = targetMO;
                return Defer(deferArgs);
            }

            // Find our own binding.
            if (targetMO.LimitType.IsSubclassOf(typeof(Delegate)))
            {
                var parms = targetMO.LimitType.GetMethod("Invoke").GetParameters();
                if (parms.Length == argMOs.Length)
                {
                    // Don't need to check if argument types match parameters.
                    // If they don't, users get an argument conversion error.
                    Expression[] callArgs = RuntimeHelpers.ConvertArguments(argMOs, parms);
                    InvocationExpression expression = Expression.Invoke(Expression.Convert(targetMO.Expression, targetMO.LimitType),callArgs);
                    return new DynamicMetaObject(
                        RuntimeHelpers.EnsureObjectResult(expression),
                        BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                                                               targetMO.LimitType));
                }
            }

            return errorSuggestion ??
                   RuntimeHelpers.CreateBindingThrow(
                       targetMO, argMOs,
                       BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                                                              targetMO.LimitType),
                       typeof(InvalidOperationException),
                       "Wrong number of arguments for function -- " +
                       targetMO.LimitType + " got " + argMOs);
        }
    }
}