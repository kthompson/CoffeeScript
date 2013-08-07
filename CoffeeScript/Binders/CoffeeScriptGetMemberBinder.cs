using System;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace CoffeeScript
{
    /// <summary>
    /// SymplGetMemberBinder is used for general dotted expressions for fetching members.
    /// </summary>
    public class CoffeeScriptGetMemberBinder : GetMemberBinder
    {
        private const BindingFlags DefaultBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;

        public CoffeeScriptGetMemberBinder(string name)
            : base(name, true)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject targetMO, DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!targetMO.HasValue) 
                return Defer(targetMO);

            // Find our own binding.
            MemberInfo[] members = targetMO.LimitType.GetMember(Name, DefaultBindingFlags);
            if (members.Length == 1)
            {
                return new DynamicMetaObject(
                    RuntimeHelpers.EnsureObjectResult(
                        Expression.MakeMemberAccess(
                            Expression.Convert(targetMO.Expression,
                                               members[0].DeclaringType),
                            members[0])),
                    // Don't need restriction test for name since this
                    // rule is only used where binder is used, which is
                    // only used in sites with this binder.Name.
                    BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                                                           targetMO.LimitType));
            }


            return errorSuggestion ??
                   RuntimeHelpers.CreateBindingThrow(
                       targetMO, null,
                       BindingRestrictions.GetTypeRestriction(targetMO.Expression, targetMO.LimitType),
                       typeof(MissingMemberException),
                       "cannot bind member, " + Name + ", on object " + targetMO.Value);
        }
    }
}