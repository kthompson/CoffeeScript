using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CoffeeScript
{
    /// <summary>
    /// The CoffeeScript Runtime
    /// </summary>
    public class CoffeeScript
    {
        public CoffeeScript()
        {
        }

        public ExpandoObject Require(string id)
        {
            //TODO: sanitize id
            if (id.StartsWith("./"))
                id = id.Substring(2);

            var cached = CoffeeScriptModule.GetCached(id);
            if (cached != null)
                return cached.Exports;

            if (!CoffeeScriptModule.Exists(id))
                throw new Exception("No such native module " + id);


            var module = new CoffeeScriptModule(id, this);

            module.Cache();
            module.Compile();
            module.Invoke();

            return module.Exports;
        }

        public ExpandoObject Execute(string coffee)
        {
            var filename = Path.GetTempFileName();
            try
            {
                File.WriteAllText(filename, coffee);

                return Require(filename);
            }
            finally
            {
                try
                {
                    File.Delete(filename);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        public ExpandoObject ExecuteFile(string filename)
        {
            return Require(filename);
        }

        #region Binders
        /////////////////////////
        // Canonicalizing Binders
        /////////////////////////

        // We need to canonicalize binders so that we can share L2 dynamic
        // dispatch caching across common call sites.  Every call site with the
        // same operation and same metadata on their binders should return the
        // same rules whenever presented with the same kinds of inputs.  The
        // DLR saves the L2 cache on the binder instance.  If one site somewhere
        // produces a rule, another call site performing the same operation with
        // the same metadata could get the L2 cached rule rather than computing
        // it again.  For this to work, we need to place the same binder instance
        // on those functionally equivalent call sites.
        
        public readonly BinderCache<InvokeMemberBinderKey, CoffeeScriptInvokeMemberBinder> InvokeMemberBinders = new BinderCache<InvokeMemberBinderKey, CoffeeScriptInvokeMemberBinder>(info => new CoffeeScriptInvokeMemberBinder(info.Name, info.Info));
        public readonly BinderCache<CallInfo, CoffeeScriptInvokeBinder> InvokeBinders = new BinderCache<CallInfo, CoffeeScriptInvokeBinder>(info => new CoffeeScriptInvokeBinder(info));
        public readonly BinderCache<string, CoffeeScriptGetMemberBinder> GetMemberBinders = new BinderCache<string, CoffeeScriptGetMemberBinder>(name => new CoffeeScriptGetMemberBinder(name));
        public readonly BinderCache<string, CoffeeScriptSetMemberBinder> SetMemberBinders = new BinderCache<string, CoffeeScriptSetMemberBinder>(name => new CoffeeScriptSetMemberBinder(name));
        public readonly BinderCache<ExpressionType, CoffeeScriptBinaryOperationBinder> BinaryOperationBinders = new BinderCache<ExpressionType, CoffeeScriptBinaryOperationBinder>(op => new CoffeeScriptBinaryOperationBinder(op));
        public readonly BinderCache<ExpressionType, CoffeeScriptUnaryOperationBinder> UnaryOperationBinders = new BinderCache<ExpressionType, CoffeeScriptUnaryOperationBinder>(op => new CoffeeScriptUnaryOperationBinder(op));
        public readonly BinderCache<CallInfo, CoffeeScriptCreateInstanceBinder> CreateInstanceBinders = new BinderCache<CallInfo, CoffeeScriptCreateInstanceBinder>(info => new CoffeeScriptCreateInstanceBinder(info));

        

        #endregion
    }

    public class BinderCache<TArg, TBinder>
        where TBinder : CallSiteBinder
    {
        private readonly Func<TArg, TBinder> _builder;
        private readonly Dictionary<TArg, TBinder> _binders = new Dictionary<TArg, TBinder>();

        public BinderCache(Func<TArg, TBinder> builder)
        {
            _builder = builder;
        }

        public TBinder Get(TArg argument)
        {
            lock (_binders)
            {
                if (_binders.ContainsKey(argument))
                    return _binders[argument];
                var b = _builder(argument);
                _binders[argument] = b;
                return b;
            }
        }
    }
}
