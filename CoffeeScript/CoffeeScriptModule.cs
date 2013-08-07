using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Linq.Expressions;

namespace CoffeeScript
{
    class CoffeeScriptModule
    {
        public string Id { get; private set; }
        public bool Compiled { get; private set; }
        public FileInfo FileInfo { get; private set; }

        /// <summary>
        /// This is the exports that the module exposes.
        /// </summary>
        public ExpandoObject Exports { get; private set; }

        protected CoffeeScript Runtime { get; private set; }

        public CoffeeScriptModule(string id, CoffeeScript runtime)
        {
            this.Id = id;
            this.Runtime = runtime;
            this.Exports = new ExpandoObject();

            this.FileInfo = new FileInfo(this.Id);
        }

        public static bool Exists(string id)
        {
            //TODO: probably could improve this
            return IsNativeModule(id) || File.Exists(id);
        }
        
        public void Compile()
        {
            if (Compiled)
                return;

            this.Compiled = true;

            if (IsNativeModule(this.Id))
            {
                var asm = Assembly.Load(new AssemblyName(this.Id));
                LoadAssembly(asm);
                return;
            }

            var nodes = Helper.Coffee(@"-n " + this.FileInfo.FullName);
            var parser = new CoffeeParseTreeReader(nodes);
            var parseTree = parser.Parse();
            var wrappedParseTree = Wrap(parseTree);
            Trace.WriteLine("Parse Tree", "CoffeeScriptModule");
            Trace.WriteLine(wrappedParseTree.ToString(), "CoffeeScriptModule");

            var expressionTree = ExpressionCompiler.Compile(wrappedParseTree, new Scope(null, null, null, Runtime));

            Trace.WriteLine("Expression Tree", "CoffeeScriptModule");
            Trace.WriteLine(expressionTree.ToCSharpCode());

            Trace.WriteLine(expressionTree.GetDebugView(), "CoffeeScriptModule");
            var lambda = ToLambda(expressionTree);
            _method = lambda.Compile();

            //DumpLambda(_method);
        }

        private static void DumpLambda(Func<object, object, object, object, object, object> compiledLambda)
        {
            var da = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("dyn"), AssemblyBuilderAccess.Save);

            var dm = da.DefineDynamicModule("dyn_mod", "dyn.dll");
            var dt = dm.DefineType("dyn_type");
            var method = dt.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static,typeof(object),new[]{typeof(object),typeof(object),typeof(object),typeof(object),typeof(object)});

            var lambdaMethod = compiledLambda.Method;

            var il = lambdaMethod.GetMethodBody().GetILAsByteArray();
            method.CreateMethodBody(il, il.Length);
            dt.CreateType();

            da.Save("dyn.dll");
        }

        private Base Wrap(Base root)
        {
            return new Code
            {
                Body = root,
                Parameters = new List<Base>
                {
                        new Param{Name = new Literal{Value = "exports"}},
                        new Param{Name = new Literal{Value = "require"}},
                        new Param{Name = new Literal{Value = "module"}},
                        new Param{Name = new Literal{Value = "__filename"}},
                        new Param{Name = new Literal{Value = "__dirname"}},
                }
            };
        }

        public void Invoke()
        {
            //Ensure we are compiled already
            Compile();

            if (_method == null)
                return;
            
            object require = (Func<string,ExpandoObject>)(s => this.Runtime.Require(s));
            MethodDumper.Dump(_method.Method);

            _method(this.Exports, require, this, this.FileInfo.FullName, this.FileInfo.Directory.FullName);
            
        }

        private delegate void ModuleLoaderDelegate(object exports, Func<string, ExpandoObject> require, object module, object __filename, object __dirname);

        private static Expression<Func<object, object, object, object, object, object>> ToLambda(Expression expression)
        {
            return (Expression<Func<object, object, object, object, object, object>>)(expression);
        } 

        private void LoadAssembly(Assembly assm)
        {
            foreach (var typ in assm.GetExportedTypes())
                AddTypeToExports(typ);
        }

        private void AddTypeToExports(Type typ)
        {
            var table = this.Exports;
            string[] names = typ.FullName.Split('.');

            table = BuildNamespace(names, table);

            DynamicObjectHelpers.SetMember(table, names[names.Length - 1], new TypeModel(typ));
        }

        private static ExpandoObject BuildNamespace(string[] names, ExpandoObject table)
        {
            for (int i = 0; i < names.Length - 1; i++)
            {
                var name = names[i];
                if (DynamicObjectHelpers.HasMember(table, name))
                {
                    // Must be Expando since only we have put objs in
                    // the tables so far.
                    table = (ExpandoObject)(DynamicObjectHelpers.GetMember(table, name));
                }
                else
                {
                    var tmp = new ExpandoObject();
                    DynamicObjectHelpers.SetMember(table, name, tmp);
                    table = tmp;
                }
            }

            return table;
        }

        private static bool IsNativeModule(string id)
        {
            return (id == "mscorlib" || id == "corlib");
        }


        static readonly Dictionary<string, CoffeeScriptModule> _cache = new Dictionary<string, CoffeeScriptModule>();
        private Func<object, object, object, object, object, object> _method;

        public static CoffeeScriptModule GetCached(string id)
        {
            if (_cache.ContainsKey(id))
                return _cache[id];

            return null;
        }

        public void Cache()
        {
            _cache[this.Id] = this;
        }

        void Load()
        {

            
        }
    }

    static class ExpressionExtentions
    {
        private static readonly MethodInfo DebugViewField = typeof(Expression).GetProperties(BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(p => p.Name == "DebugView").GetGetMethod(true);
        //static readonly MethodInfo _getDebugViewMethod = .GetGetMethod();

        public static string GetDebugView(this Expression expression)
        {
            var value = DebugViewField.Invoke(expression, new object[] { });
            return value as string;
        }
    }
}