using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoffeeScript
{
    /// <summary>
    /// SymplInvokeMemberBinder is used for general dotted expressions in function calls for invoking members.
    /// </summary>
    public class CoffeeScriptInvokeMemberBinder : InvokeMemberBinder
    {
        public CoffeeScriptInvokeMemberBinder(string name, CallInfo callinfo)
            : base(name, false, callinfo)
        {
            // true = ignoreCase
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject targetMO, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!targetMO.HasValue || args.Any((a) => !a.HasValue))
            {
                var deferArgs = new DynamicMetaObject[args.Length + 1];
                for (int i = 0; i < args.Length; i++)
                {
                    deferArgs[i + 1] = args[i];
                }
                deferArgs[0] = targetMO;
                return Defer(deferArgs);
            }
            // Find our own binding.
            // Could consider allowing invoking static members from an instance.
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            MemberInfo[] members = targetMO.LimitType.GetMember(Name, flags);
            if ((members.Length == 1) && (members[0] is PropertyInfo ||
                                          members[0] is FieldInfo))
            {
                // NEED TO TEST, should check for delegate value too
                MemberInfo mem = members[0];
                throw new NotImplementedException();
                //return new DynamicMetaObject(
                //    Expression.Dynamic(
                //        new SymplInvokeBinder(new CallInfo(args.Length)),
                //        typeof(object),
                //        args.Select(a => a.Expression).AddFirst(
                //               Expression.MakeMemberAccess(this.Expression, mem)));

                // Don't test for eventinfos since we do nothing with them now.
            }
            else
            {
                // Get MethodInfos with right arg counts.
                IEnumerable<MethodInfo> mi_mems = members.
                    Select(m => m as MethodInfo).
                    Where(m => m.GetParameters().Length == args.Length);
                // Get MethodInfos with param types that work for args.  This works
                // except for value args that need to pass to reftype params. 
                // We could detect that to be smarter and then explicitly StrongBox
                // the args.
                var res = mi_mems.Where(mem => RuntimeHelpers.ParametersMatchArguments(mem.GetParameters(), args)).ToList();
                // False below means generate a type restriction on the MO.
                // We are looking at the members targetMO's Type.
                BindingRestrictions restrictions = RuntimeHelpers.GetTargetArgsRestrictions(targetMO, args, false);
                if (res.Count == 0)
                {
                    return errorSuggestion ?? RuntimeHelpers.CreateBindingThrow(targetMO, args, restrictions, typeof(MissingMemberException), "Can't bind member invoke -- " + args);
                }
                // restrictions and conversion must be done consistently.
                Expression[] callArgs = RuntimeHelpers.ConvertArguments(args, res[0].GetParameters());
                return new DynamicMetaObject(
                    RuntimeHelpers.EnsureObjectResult(
                        Expression.Call(
                            Expression.Convert(targetMO.Expression, targetMO.LimitType), 
                            res[0], 
                            callArgs)),
                    restrictions);
                // Could hve tried just letting Expr.Call factory do the work,
                // but if there is more than one applicable method using just
                // assignablefrom, Expr.Call throws.  It does not pick a "most
                // applicable" method or any method.
            }
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject targetMO, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            var argexprs = new Expression[args.Length + 1];
            for (int i = 0; i < args.Length; i++)
            {
                argexprs[i + 1] = args[i].Expression;
            }
            argexprs[0] = targetMO.Expression;
            // Just "defer" since we have code in SymplInvokeBinder that knows
            // what to do, and typically this fallback is from a language like
            // Python that passes a DynamicMetaObject with HasValue == false.
            return new DynamicMetaObject(
                Expression.Dynamic(
                    // This call site doesn't share any L2 caching
                    // since we don't call GetInvokeBinder from Sympl.
                    // We aren't plumbed to get the runtime instance here.
                    new CoffeeScriptInvokeBinder(new CallInfo(args.Length)),
                    typeof(object), // ret type
                    argexprs),
                // No new restrictions since SymplInvokeBinder will handle it.
                targetMO.Restrictions.Merge(BindingRestrictions.Combine(args)));
        }
    }
}