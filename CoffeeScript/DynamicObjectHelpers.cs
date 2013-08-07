using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CoffeeScript
{
    /// <summary>
    /// Dynamic Helpers for HasMember, GetMember, SetMember
    /// 
    /// DynamicObjectHelpers provides access to IDynObj members given names as
    /// data at runtime.  When the names are known at compile time (o.foo), then
    /// they get baked into specific sites with specific binders that encapsulate
    /// the name.  We need this in python because hasattr et al are case-sensitive.
    /// 
    /// Currently Sympl only uses this on ExpandoObjects, but it works generally on
    /// IDOs.
    /// </summary>
    internal class DynamicObjectHelpers
    {
        private static readonly Dictionary<string, CallSite<Func<CallSite, object, object>>> GetSites = new Dictionary<string, CallSite<Func<CallSite, object, object>>>();

        private static readonly Dictionary<string, CallSite<Action<CallSite, object, object>>> SetSites = new Dictionary<string, CallSite<Action<CallSite, object, object>>>();

        static DynamicObjectHelpers()
        {
            Sentinel = new object();
        }

        internal static object Sentinel { get; private set; }

        internal static bool HasMember(IDynamicMetaObjectProvider o, string name)
        {
            return (GetMember(o, name) != Sentinel);
            //Alternative impl used when EOs had bug and didn't call fallback ...
            //var mo = o.GetMetaObject(Expression.Parameter(typeof(object), null));
            //foreach (string member in mo.GetDynamicMemberNames()) {
            //    if (string.Equals(member, name, StringComparison.OrdinalIgnoreCase)) {
            //        return true;
            //    }
            //}
            //return false;
        }

        internal static object GetMember(IDynamicMetaObjectProvider o, string name)
        {
            CallSite<Func<CallSite, object, object>> site;
            if (!GetSites.TryGetValue(name, out site))
            {
                site = CallSite<Func<CallSite, object, object>>.Create(new DoHelpersGetMemberBinder(name));
                GetSites[name] = site;
            }
            return site.Target(site, o);
        }

        internal static void SetMember(IDynamicMetaObjectProvider o, string name, object value)
        {
            CallSite<Action<CallSite, object, object>> site;
            if (!SetSites.TryGetValue(name, out site))
            {
                site = CallSite<Action<CallSite, object, object>>.Create(new DoHelpersSetMemberBinder(name));
                SetSites[name] = site;
            }
            site.Target(site, o, value);
        }
    }

    internal class DoHelpersGetMemberBinder : GetMemberBinder
    {
        internal DoHelpersGetMemberBinder(string name)
            : base(name, true)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (errorSuggestion != null)
                return errorSuggestion;

            var sentinalExpr = Expression.Constant(DynamicObjectHelpers.Sentinel);
            var restrictions = target.Restrictions.Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            return new DynamicMetaObject(sentinalExpr, restrictions);
        }
    }

    internal class DoHelpersSetMemberBinder : SetMemberBinder
    {
        internal DoHelpersSetMemberBinder(string name)
            : base(name, true)
        {
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            return errorSuggestion ??
                   RuntimeHelpers.CreateBindingThrow(
                       target, null, BindingRestrictions.Empty,
                       typeof(MissingMemberException),
                       "If IDynObj doesn't support setting members, " +
                       "DOHelpers can't do it for the IDO.");
        }
    }

    // RuntimeHelpers is a collection of functions that perform operations at
    // runtime of Sympl code, such as performing an import or eq.
    //
    public static class RuntimeHelpers
    {
        //// SymplImport takes the runtime and module as context for the import.
        //// It takes a list of names, what, that either identify a (possibly dotted
        //// sequence) of names to fetch from Globals or a file name to load.  Names
        //// is a list of names to fetch from the final object that what indicates
        //// and then set each name in module.  Renames is a list of names to add to
        //// module instead of names.  If names is empty, then the name set in
        //// module is the last name in what.  If renames is not empty, it must have
        //// the same cardinality as names.
        ////
        //public static object SymplImport(CoffeeScript runtime, ExpandoObject module, string[] what, string[] names, string[] renames)
        //{
        //    // Get object or file scope.
        //    object value = null;
        //    if (what.Length == 1)
        //    {
        //        string name = what[0];
        //        if (DynamicObjectHelpers.HasMember(runtime.Globals, name))
        //        {
        //            value = DynamicObjectHelpers.GetMember(runtime.Globals, name);
        //        }
        //        else
        //        {
        //            var f = (string)(DynamicObjectHelpers.GetMember(module, "__file__"));
        //            f = Path.Combine(Path.GetDirectoryName(f), name + ".sympl");
        //            if (File.Exists(f))
        //            {
        //                value = runtime.ExecuteFile(f);
        //            }
        //            else
        //            {
        //                throw new ArgumentException(
        //                    "Import: can't find name in globals " +
        //                    "or as file to load -- " + name + " " + f);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // What has more than one name, must be Globals access.
        //        value = runtime.Globals;
        //        // For more correctness and generality, shouldn't assume all
        //        // globals are dynamic objects, or that a look up like foo.bar.baz
        //        // cascades through all dynamic objects.
        //        // Would need to manually create a CallSite here with Sympl's
        //        // GetMemberBinder, and think about a caching strategy per name.
        //        foreach (string name in what)
        //        {
        //            value = DynamicObjectHelpers.GetMember((IDynamicMetaObjectProvider)value, name);
        //        }
        //    }
        //    // Assign variables in module.
        //    if (names.Length == 0)
        //    {
        //        DynamicObjectHelpers.SetMember(module, what[what.Length - 1], value);
        //    }
        //    else
        //    {
        //        if (renames.Length == 0) renames = names;
        //        for (int i = 0; i < names.Length; i++)
        //        {
        //            string name = names[i];
        //            string rename = renames[i];
        //            DynamicObjectHelpers.SetMember(module, rename, DynamicObjectHelpers.GetMember((IDynamicMetaObjectProvider)value, name));
        //        }
        //    }
        //    return null;
        //}

        // SymplImport

        // Uses of the 'eq' keyword form in Sympl compile to a call to this
        // helper function.
        //
        public static bool SymplEq(object x, object y)
        {
            if (x == null)
                return y == null;

            if (y == null)
                return false;

            Type xtype = x.GetType();
            Type ytype = y.GetType();

            if (xtype.IsPrimitive && xtype != typeof(string) &&
                ytype.IsPrimitive && ytype != typeof(string))
                return x.Equals(y);

            return ReferenceEquals(x, y);
        }


        //////////////////////////////////////////////////
        // Array Utilities (slicing) and some LINQ helpers
        //////////////////////////////////////////////////

        public static T[] RemoveFirstElt<T>(IList<T> list)
        {
            // Make array ...
            if (list.Count == 0)
            {
                return new T[0];
            }
            var res = new T[list.Count];
            list.CopyTo(res, 0);
            // Shift result
            return ShiftLeft(res, 1);
        }

        public static T[] RemoveFirstElt<T>(T[] array)
        {
            return ShiftLeft(array, 1);
        }

        private static T[] ShiftLeft<T>(T[] array, int count)
        {
            //ContractUtils.RequiresNotNull(array, "array");
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            var result = new T[array.Length - count];
            Array.Copy(array, count, result, 0, result.Length);
            return result;
        }

        public static T[] RemoveLast<T>(T[] array)
        {
            //ContractUtils.RequiresNotNull(array, "array");
            Array.Resize(ref array, array.Length - 1);
            return array;
        }

        ///////////////////////////////////////
        // Utilities used by binders at runtime
        ///////////////////////////////////////

        // ParamsMatchArgs returns whether the args are assignable to the parameters.
        // We specially check for our TypeModel that wraps .NET's RuntimeType, and
        // elsewhere we detect the same situation to convert the TypeModel for calls.
        //
        // Consider checking p.IsByRef and returning false since that's not CLS.
        //
        // Could check for a.HasValue and a.Value is None and
        // ((paramtype is class or interface) or (paramtype is generic and
        // nullable<t>)) to support passing nil anywhere.
        //
        public static bool ParametersMatchArguments(ParameterInfo[] parameters,
                                                    DynamicMetaObject[] args)
        {
            // We only call this after filtering members by this constraint.
            Debug.Assert(args.Length == parameters.Length,
                         "Internal: args are not same len as params?!");
            for (int i = 0; i < args.Length; i++)
            {
                Type paramType = parameters[i].ParameterType;
                // We consider arg of TypeModel and param of Type to be compatible.
                if (paramType == typeof(Type) &&
                    (args[i].LimitType == typeof(TypeModel)))
                {
                    continue;
                }
                if (!paramType
                    // Could check for HasValue and Value==null AND
                    // (paramtype is class or interface) or (is generic
                    // and nullable<T>) ... to bind nullables and null.
                         .IsAssignableFrom(args[i].LimitType))
                {
                    return false;
                }
            }
            return true;
        }

        // Returns a DynamicMetaObject with an expression that fishes the .NET
        // RuntimeType object from the TypeModel MO.
        //
        public static DynamicMetaObject GetRuntimeTypeMoFromModel(DynamicMetaObject typeModelMO)
        {
            Debug.Assert((typeModelMO.LimitType == typeof(TypeModel)),
                         "Internal: MO is not a TypeModel?!");
            // Get tm.ReflType
            PropertyInfo pi = typeof(TypeModel).GetProperty("ReflType");
            Debug.Assert(pi != null);
            return new DynamicMetaObject(
                Expression.Property(
                    Expression.Convert(typeModelMO.Expression, typeof(TypeModel)),
                    pi),
                typeModelMO.Restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        typeModelMO.Expression, typeof(TypeModel))) //,
                // Must supply a value to prevent binder FallbackXXX methods
                // from infinitely looping if they do not check this MO for
                // HasValue == false and call Defer.  After Sympl added Defer
                // checks, we could verify, say, FallbackInvokeMember by no
                // longer passing a value here.
                //((TypeModel)typeModelMO.Value).ReflType
                );
        }

        // Returns list of Convert exprs converting args to param types.  If an arg
        // is a TypeModel, then we treat it special to perform the binding.  We need
        // to map from our runtime model to .NET's RuntimeType object to match.
        //
        // To call this function, args and pinfos must be the same length, and param
        // types must be assignable from args.
        //
        // NOTE, if using this function, then need to use GetTargetArgsRestrictions
        // and make sure you're performing the same conversions as restrictions.
        //
        public static Expression[] ConvertArguments(DynamicMetaObject[] args, ParameterInfo[] ps)
        {
            Debug.Assert(args.Length == ps.Length,
                         "Internal: args are not same len as params?!");
            var callArgs = new Expression[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                Expression argExpr = args[i].Expression;
                if (args[i].LimitType == typeof(TypeModel) &&
                    ps[i].ParameterType == typeof(Type))
                {
                    // Get arg.ReflType
                    argExpr = GetRuntimeTypeMoFromModel(args[i]).Expression;
                }
                argExpr = Expression.Convert(argExpr, ps[i].ParameterType);
                callArgs[i] = argExpr;
            }
            return callArgs;
        }

        // GetTargetArgsRestrictions generates the restrictions needed for the
        // MO resulting from binding an operation.  This combines all existing
        // restrictions and adds some for arg conversions.  targetInst indicates
        // whether to restrict the target to an instance (for operations on type
        // objects) or to a type (for operations on an instance of that type).
        //
        // NOTE, this function should only be used when the caller is converting
        // arguments to the same types as these restrictions.
        //
        public static BindingRestrictions GetTargetArgsRestrictions(
            DynamicMetaObject target, DynamicMetaObject[] args,
            bool instanceRestrictionOnTarget)
        {
            // Important to add existing restriction first because the
            // DynamicMetaObjects (and possibly values) we're looking at depend
            // on the pre-existing restrictions holding true.
            BindingRestrictions restrictions = target.Restrictions.Merge(BindingRestrictions
                                                                             .Combine(args));
            if (instanceRestrictionOnTarget)
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(
                        target.Expression,
                        target.Value
                        ));
            }
            else
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        target.Expression,
                        target.LimitType
                        ));
            }
            for (int i = 0; i < args.Length; i++)
            {
                BindingRestrictions r;
                if (args[i].HasValue && args[i].Value == null)
                {
                    r = BindingRestrictions.GetInstanceRestriction(
                        args[i].Expression, null);
                }
                else
                {
                    r = BindingRestrictions.GetTypeRestriction(
                        args[i].Expression, args[i].LimitType);
                }
                restrictions = restrictions.Merge(r);
            }
            return restrictions;
        }

        // Return the expression for getting target[indexes]
        //
        // Note, callers must ensure consistent restrictions are added for
        // the conversions on args and target.
        //
        public static Expression GetIndexingExpression(
            DynamicMetaObject target,
            DynamicMetaObject[] indexes)
        {
            Debug.Assert(target.HasValue && target.LimitType != typeof(Array));

            UnaryExpression[] indexExpressions = indexes.Select(
                i => Expression.Convert(i.Expression, i.LimitType))
                                                        .ToArray();

            //// CONS
            //if (target.LimitType == typeof(Cons))
            //{
            //    // Call RuntimeHelper.GetConsElt
            //    var args = new List<Expression>();
            //    // The first argument is the list
            //    args.Add(
            //        Expression.Convert(
            //            target.Expression,
            //            target.LimitType)
            //        );
            //    args.AddRange(indexExpressions);
            //    return Expression.Call(
            //        typeof(RuntimeHelpers),
            //        "GetConsElt",
            //        null,
            //        args.ToArray());
            //    // ARRAY
            //}
            //else
                if (target.LimitType.IsArray)
            {
                return Expression.ArrayAccess(
                    Expression.Convert(target.Expression,
                                       target.LimitType),
                    indexExpressions
                    );
                // INDEXER
            }
            else
            {
                PropertyInfo[] props = target.LimitType.GetProperties();
                PropertyInfo[] indexers = props.
                    Where(p => p.GetIndexParameters().Length > 0).ToArray();
                indexers = indexers.
                    Where(idx => idx.GetIndexParameters().Length ==
                                 indexes.Length).ToArray();

                var res = new List<PropertyInfo>();
                foreach (PropertyInfo idxer in indexers)
                {
                    if (ParametersMatchArguments(
                        idxer.GetIndexParameters(), indexes))
                    {
                        // all parameter types match
                        res.Add(idxer);
                    }
                }
                if (res.Count == 0)
                {
                    return Expression.Throw(
                        Expression.New(
                            typeof(MissingMemberException).GetConstructor(new[] { typeof(string) }),
                            Expression.Constant("Can't bind because there is no matching indexer.")
                            )
                        );
                }
                return Expression.MakeIndex(
                    Expression.Convert(target.Expression, target.LimitType),
                    res[0], indexExpressions);
            }
        }

        // CreateThrow is a convenience function for when binders cannot bind.
        // They need to return a DynamicMetaObject with appropriate restrictions
        // that throws.  Binders never just throw due to the protocol since
        // a binder or MO down the line may provide an implementation.
        //
        // It returns a DynamicMetaObject whose expr throws the exception, and 
        // ensures the expr's type is object to satisfy the CallSite return type
        // constraint.
        //
        // A couple of calls to CreateThrow already have the args and target
        // restrictions merged in, but BindingRestrictions.Merge doesn't add 
        // duplicates.
        //
        public static DynamicMetaObject CreateBindingThrow(DynamicMetaObject target, DynamicMetaObject[] args, BindingRestrictions moreTests, Type exception, params object[] exceptionArgs)
        {
            var throwExpression = CreateThrowExpression(exception, exceptionArgs);
            return new DynamicMetaObject(
                throwExpression,
                target.Restrictions.Merge(BindingRestrictions.Combine(args))
                      .Merge(moreTests));
        }

        public static Expression CreateThrowExpression(Type exception, params object[] exceptionArgs)
        {
            Expression[] argExprs = null;
            var argTypes = Type.EmptyTypes;
            if (exceptionArgs != null)
            {
                argExprs = new Expression[exceptionArgs.Length];
                argTypes = new Type[exceptionArgs.Length];

                var i = 0;
                foreach (var e in exceptionArgs.Select(Expression.Constant))
                {
                    argExprs[i] = e;
                    argTypes[i] = e.Type;
                    i += 1;
                }
            }
            
            var constructor = exception.GetConstructor(argTypes);
            if (constructor == null)
                throw new ArgumentException("Type doesn't have constructor with a given signature");

            return Expression.Throw(Expression.New(constructor, argExprs), // Force expression to be type object so that DLR CallSite
                                    // code things only type object flows out of the CallSite.
                                    typeof (object));
        }

        // EnsureObjectResult wraps expr if necessary so that any binder or
        // DynamicMetaObject result expression returns object.  This is required
        // by CallSites.
        //
        public static Expression EnsureObjectResult(Expression expr)
        {
            if (!expr.Type.IsValueType)
                return expr;
            if (expr.Type == typeof(void))
                return Expression.Block(expr, Expression.Default(typeof(object)));
            else
                return Expression.Convert(expr, typeof(object));
        }
    }



}