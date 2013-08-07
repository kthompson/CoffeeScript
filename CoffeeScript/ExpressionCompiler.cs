using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CoffeeScript
{
    //internal enum CompilerLevel
    //{
    //    Top = 1, // ...;
    //    Paren = 2, // (...)
    //    List = 3, // [...]
    //    Cond = 4, // ... ? x : y
    //    Op = 5, // !...
    //    Access = 6 // ...[0]
    //}

    static class ExpressionCompiler
    {
        private static readonly Type ObjectType = typeof(object);
        private static readonly ConstructorInfo ExpandoConstructor = typeof(ExpandoObject).GetConstructor(new Type[]{});

        //public ExpressionCompiler()
        //{
        //    var an = new AssemblyName("DynamicAssembly");
        //    var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
        //    var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        //    var tb = moduleBuilder.DefineType("MyDynamicType"
        //                        , TypeAttributes.Public |
        //                        TypeAttributes.Class |
        //                        TypeAttributes.AutoClass |
        //                        TypeAttributes.AnsiClass |
        //                        TypeAttributes.BeforeFieldInit |
        //                        TypeAttributes.AutoLayout
        //                        , null);
        //    //return tb;
        //}

        public static Expression Compile(Base node, Scope scope)
        {
            if (node == null)
                return null;

            var block = node as Block;
            if (block != null)
                return CompileBlock(block, scope);

            var assign = node as Assign;
            if (assign != null)
                return CompileAssign(assign, scope);

            var value = node as Value;
            if (value != null)
                return CompileValue(value, scope);

            var literal = node as Literal;
            if (literal != null)
                return CompileLiteral(literal, scope);

            var parens = node as Parens;
            if (parens != null)
                return CompileParens(parens, scope);

            var arr = node as Arr;
            if (arr != null)
                return CompileArr(arr, scope);

            var iff = node as If;
            if (iff != null)
                return CompileIf(iff, scope);

            var existence = node as Existence;
            if (existence != null)
                return CompileExistence(existence, scope);

            var boo = node as Bool;
            if (boo != null)
                return CompileBool(boo, scope);

            var call = node as Call;
            if (call != null)
                return CompileCall(call, scope);

            var code = node as Code;
            if (code != null)
                return CompileCode(code, scope);

            var nil = node as Null;
            if (nil != null)
                return CompileNull(nil, scope);

            var op = node as Op;
            if (op != null)
                return CompileOp(op, scope);

            var @for = node as For;
            if (@for != null)
                return CompileFor(@for, scope);

            var obj = node as Obj;
            if (obj != null)
                return CompileObj(obj, scope);

            if (node is Return)
            {

            }
            else if (node is Extends)
            {

            }
            else if (node is Access)
            {

            }
            else if (node is Index)
            {

            }
            else if (node is Param)
            {

            }

            throw new NotImplementedException("The node type is not implemented: " + node.GetType().Name);
        }

        private static Expression CompileObj(Obj obj, Scope scope)
        {
            var temp = scope.FreeVariable("obj");
            var expressions = new List<Expression>
            {
                Expression.Assign(temp, Expression.New(ExpandoConstructor))
            };

            foreach (var @base in obj.Properties)
            {
                var property = @base as Assign;

                foreach (var name in CompileToNames(property.Variable))
                {
                    var expression = Compile(property.Value, scope);

                    expressions.Add(Expression.Dynamic(scope.GetRuntime().SetMemberBinders.Get(name), typeof(object), temp, expression));
                }
            }

            expressions.Add(temp);

            return Expression.Block(expressions);
        }

        private static IEnumerable<string> CompileToNames(params Base[] objects)
        {
            return CompileToNames((IEnumerable<Base>)objects);
        }

        private static IEnumerable<string> CompileToNames(IEnumerable<Base> objects)
        {
            var stack = new List<Base>(objects);

            while (stack.Count > 0)
            {
                var o = stack[0];
                stack.RemoveAt(0);

                while (true)
                {
                    if (o is Literal)
                    {
                        yield return ((Literal) o).Value;
                        break;
                    }

                    if (o is Obj)
                    {
                        stack.AddRange(((Obj)o).Properties);
                        break;
                    }
                    
                    if (o is Value)
                    {
                        o = ((Value) o).Base;
                    }
                    else if (o is Param)
                    {
                        o = ((Param)o).Name;
                    }
                    else if (o is Access)
                    {
                        o = ((Access)o).Name;
                    }
                    else
                    {
                        Helper.Stop();
                    }
                }
            }
        }

        private static Expression CompileFor(For @for, Scope scope)
        {
            throw new NotImplementedException();
        }

        public static Expression CompileChain(Op op, Scope scope)
        {
            //for a chain we need to convert the second param of the first to a local. then we can do two comparisons and merge them with &&
            var first = op.First as Op;

            var refName = scope.FreeVariableName("ref");

            first.Second = first.Second.Cache(refName);

            var fst = Compile(first, scope);
            var scd = Compile(new Op {First = first.Second, Second = op.Second, Operator = op.Operator}, scope);

            return Expression.Dynamic(
                scope.GetRuntime().BinaryOperationBinders.Get(ExpressionType.And),
                typeof (object),
                fst, scd);
        }

        public static Expression CompileOp(Op op, Scope scope)
        {
            var ischain = op.IsChainable && op.First is Op && ((Op)op.First).IsChainable;

            if (op.Second == null)
                return CompileUnary(op, scope);

            if (ischain)
                return CompileChain(op, scope);

            if (op.Operator == "?")
                return CompileExistence(op, scope);

            var first = Compile(op.First, scope);
            var second = Compile(op.Second, scope);

            var operation = GetExpressionType(op.Operator);
            return Expression.Dynamic(
                scope.GetRuntime().BinaryOperationBinders.Get(operation),
                typeof(object),
                first,
                second
            );
        }

        private static Expression CompileExistence(Op op, Scope scope)
        {
            var p = scope.FreeVariable("ref");

            var @ref = new Literal {Value = p.Name};
            var fst = new Parens {Body = new Assign {Variable = @ref, Value = op.First}};

            return Compile(new If {Condition = new Existence {Expression = fst}, Body = @ref, ElseBody = op.Second}, scope);
        }

        public static Expression CompileUnary(Op op, Scope scope)
        {
            throw new NotImplementedException();
        }

        private static ExpressionType GetExpressionType(string @operator)
        {
            switch (@operator)
            {
                case "<":
                    return ExpressionType.LessThan;

                case "<=":
                    return ExpressionType.LessThanOrEqual;

                case ">":
                    return ExpressionType.GreaterThan;

                case ">=":
                    return ExpressionType.GreaterThanOrEqual;

                case "===":
                    return ExpressionType.Equal;

                case "!==":
                    return ExpressionType.NotEqual;

                case "+":
                    return ExpressionType.Add;

                case "*":
                    return ExpressionType.Multiply;

                case "||":
                    return ExpressionType.Or;

                case "/":
                    return ExpressionType.Divide;

                case "-":
                    return ExpressionType.Subtract;

                case "%":
                    return ExpressionType.Modulo;

                default:
                    throw new NotImplementedException("Op: " + @operator);
            }
        }

        public static Expression CompileNull(Null nil, Scope scope)
        {
            return Expression.Default(ObjectType);
        }

        public static Expression CompileCode(Code code, Scope scope)
        {
            var block = code.Body as Block;
            scope = new Scope(scope, block, code, scope.Runtime);
            
            var parameters = new List<ParameterExpression>();
            var funcArgs = new List<Type> { typeof(object) };
            foreach (var name in CompileToNames(code.Parameters))
            {
                parameters.Add(scope.GetOrCreate(VariableType.Parameter, name));
                funcArgs.Add(typeof(object));
            }

            AddDeclarationsFromAssignments(block, scope);

            var body = Compile(code.Body, scope);
            
            Helper.IsTrue(string.IsNullOrEmpty(code.Name));

            return Expression.Lambda(Expression.GetFuncType(funcArgs.ToArray()), body, code.Name, parameters);
        }

        private static void AddDeclarationsFromAssignments(Block block, Scope scope)
        {
            var variables = block.Expressions.OfType<Assign>().Select(assign => assign.Variable as Value).Where(v => !v.HasProperties);

            foreach (var name in CompileToNames(variables))
                scope.GetOrCreate(VariableType.Variable, name);
        }

        public static Expression CompileCall(Call node, Scope scope)
        {
            var args = new List<Expression>();
            args.AddRange(node.Arguments.Select(arg => Compile(arg, scope)));

            return GetMemberInvocaton(node.Variable, scope,
                (memberObject, name) =>
                {
                    args.Insert(0, memberObject);
                    return Expression.Dynamic(scope.GetRuntime().InvokeMemberBinders.Get(new InvokeMemberBinderKey(name, new CallInfo(node.Arguments.Count))), typeof(object), args);
                },
                (memberObject) =>
                {
                    args.Insert(0, memberObject);
                    return Expression.Dynamic(scope.GetRuntime().InvokeBinders.Get(new CallInfo(node.Arguments.Count)), typeof (object), args);
                });
        }

        public static Expression CompileBool(Bool node, Scope scope)
        {
            return Expression.Convert(Expression.Constant(node.Value == "true"), typeof(object));
        }

        public static Expression CompileExistence(Existence node, Scope scope)
        {
            var expr = Compile(node.Expression, scope);
            Helper.NotImplemented();
            return null;
        }

        public static Expression CompileIf(If node, Scope scope)
        {
            var test = Compile(node.Condition, scope);
            var body = Compile(node.Body, scope);
            var elseBody = Compile(node.ElseBody, scope);
            if (elseBody == null)
                return Expression.IfThen(test, body);

            return Expression.IfThenElse(test, body, elseBody);
        }

        public static Expression CompileArr(Arr node, Scope scope)
        {
            return Expression.NewArrayInit(ObjectType, node.Objects.Select(o => Compile(o, scope)));
        }

        public static Expression CompileParens(Parens node, Scope scope)
        {
            return Compile(node.Body, scope);
        }

        public static Expression CompileLiteral(Literal node, Scope scope)
        {
            //TODO: need to determine if we are doing a value or a variable
            var value = node.Value;

            int i;
            if (int.TryParse(value, out i))
                return Expression.Constant(i, ObjectType);

            long l;
            if (long.TryParse(value, out l))
                return Expression.Constant(l, ObjectType);

            if ((value.StartsWith("'") && value.EndsWith("'")) || (value.StartsWith("\"") && value.EndsWith("\"")))
            {
                //de-quote
                value = value.Substring(1, value.Length - 2);
                return Expression.Constant(value, ObjectType);
            }

            //TODO: we should not be creating variables here. Only upon assignment.
            var p = scope.GetVariableExpression(value);
            if (p == null)
            {
                return RuntimeHelpers.CreateThrowExpression(typeof(MissingMemberException), "'" + value + "' is undefined.");
            }

            Helper.IsNotNull(p);
            return p;
        }

        public static Expression CompileValue(Value node, Scope scope)
        {
            var curExpr = Compile(node.Base, scope);

            foreach (var name in CompileToNames(node.Properties))
            {
                curExpr = Expression.Dynamic(scope.GetRuntime().GetMemberBinders.Get(name),
                    typeof (object),
                    curExpr
                );
            }

            Helper.IsNotNull(curExpr);

            return curExpr;
        }

        public static Expression CompileAssign(Assign node, Scope scope)
        {
            var variable = node.Variable as Value;
            var value = node.Value;
            
            

            if (variable != null)
            {
                if (variable.IsObject)
                {
                    var obj = variable.Base as Obj;
                    return CompileObjectAssignment(obj, value, scope);
                }

                if (!node.Variable.IsAssignable)
                    throw new InvalidOperationException("Variable is not assignable");
                if (!variable.HasProperties)
                {
                    foreach(var name in CompileToNames(variable))
                        scope.Add(name, VariableType.Variable);
                }
            }

            var right = Compile(value, scope);

            return GetMemberInvocaton(node.Variable, scope,
                        (memberObject, name) => Expression.Dynamic(scope.GetRuntime().SetMemberBinders.Get(name), typeof (object), memberObject, right),
                        (memberObject) => Expression.Assign(memberObject, right));
        }

        private static Expression CompileObjectAssignment(Obj variable, Base value, Scope scope)
        {
            var tempName = scope.FreeVariableName("ref");

            var expressions1 = new List<Base>
            {
                new Assign {Variable = NewValue(tempName), Value = value}
            };

            var propNames = CompileToNames(variable);

            var assignments = propNames.Select(prop => new Assign {Variable = NewValue(prop), Value = NewValue(tempName, prop)});

            expressions1.AddRange(assignments);
            var expressions = expressions1.Select(expr => Compile(expr, scope));

            return Expression.Block(expressions);
        }

        private static Value NewValue(string value, string access = null)
        {
            var newValue = new Value {Base = new Literal {Value = value}};
            if (access != null)
                newValue.Properties.Add(new Access {Name = new Literal {Value = access}});
            return newValue;
        }

        private delegate Expression MemberBinder(Expression memberObject, string name);
        private delegate Expression SimpleBinder(Expression memberObject);
        private static Expression GetMemberInvocaton(Base v, Scope scope, MemberBinder binder, SimpleBinder simple)
        {
            var value = v as Value;

            Expression memberObject;
            if (value != null && value.Properties.Count > 0)
            {
                var last = value.Properties.Last() as Access;
                Helper.IsNotNull(last);
                var nameLit = last.Name as Literal;
                Helper.IsNotNull(nameLit);

                value.Properties.Remove(last);

                memberObject = Compile(value, scope);

                return binder(memberObject, nameLit.Value);
            }

            memberObject = Compile(v, scope);

            return simple(memberObject);
        }

        public static Expression CompileBlock(Block node, Scope scope)
        {
            var expressions = node.Expressions.Select(expression => Compile(expression, scope)).ToArray();
            var variables = scope.DeclaredVariables;
            return Expression.Block(variables, expressions);
        }
    }

    class CodeFragment
    {
        public Base Parent { get; private set; }
        public object Code { get; private set; }
        public Type Type { get; private set; }

        public CodeFragment(Base parent, object code)
        {
            this.Parent = parent;
            this.Code = code;
            if (this.Parent != null)
                this.Type = this.Parent.GetType();
        }
    }
}
