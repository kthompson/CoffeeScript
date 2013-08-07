using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CoffeeScript
{
    class Scope
    {
        
        public static Scope Root { get; set; }

        public Scope Parent { get; private set; }
        public List<Variable> Variables { get; private set; }
        public Dictionary<string, int> Positions { get; private set; }
        public bool Shared { get; private set; }

        public bool HasAssignments { get; set; }
        public Block Expressions { get; set; }
        public Code Method { get; set; }
        public CoffeeScript Runtime { get; set; }


        public Scope(Scope parent, Block expressions, Code method, CoffeeScript runtime)
        {
            this.Parent = parent;
            this.Expressions = expressions;
            this.Method = method;
            this.Runtime = runtime;
            this.Variables = new List<Variable>
            {
                new Variable
                {
                    Name = "arguments", 
                    Expression = Expression.Parameter(typeof(object), "arguments"),
                    Type = VariableType.Parameter
                }
            };
            this.Positions = new Dictionary<string, int>();

            if (parent == null) 
                Root = this;
        }

        public Variable Add(string name, VariableType type, bool immediate = false)
        {
            if (this.Shared && !immediate)
            {
                return this.Parent.Add(name, type);
            }

            if (this.Positions.ContainsKey(name))
            {
                var index = this.Positions[name];
                return this.Variables[index];
            }
            
            // new variable
            var variable = new Variable
            {
                Name = name,
                Expression = Expression.Parameter(typeof(object), name),
                Type = type
            };

            this.Variables.Add(variable);

            return variable;
        }

        public Code NamedMethod()
        {
            if ((this.Method != null && !string.IsNullOrEmpty(this.Method.Name)) || this.Parent == null)
            {
                return this.Method;
            }

            return this.Parent.NamedMethod();
        }

        public ParameterExpression GetOrCreate(VariableType variableType, string name)
        {
            return (GetVariable(name) ?? this.Add(name, variableType)).Expression;
        }

        public ParameterExpression GetVariableExpression(string name)
        {
            var variable = GetVariable(name);
            if (variable == null)
                return null;

            return variable.Expression;
        }

        public bool Check(string name, bool b = false)
        {
            return GetVariable(name) != null;
        }

        public Variable GetVariable(string name)
        {
            return GetVariables(name).FirstOrDefault();
        }

        public IEnumerable<Variable> GetVariables(string name)
        {
            return GetVariables().Where(v => v.Name == name);
        }

        private IEnumerable<Variable> GetVariables()
        {
            IEnumerable<Variable> list = this.Variables;
            if (this.Parent != null)
                list = list.Concat(this.Parent.GetVariables());
            return list;
        }

        public string Temporary(string name, int index)
        {
            if (name.Length > 1)
                return '_' + name + (index > 1 ? (index - 1).ToString() : string.Empty);

            throw new NotImplementedException();
            //TODO: return '_' + (index + int.Parse(name, 36)).toString(36).replace(@"\d"g, 'a');
        }

        public ParameterExpression FreeVariable(string prefix)
        {
            var index = 0;
            string temp;
            while (this.Check(temp = this.Temporary(prefix, index)))
            {
                index++;
            }
            
            return this.Add(temp, VariableType.Variable).Expression;
        }

        public string FreeVariableName(string prefix)
        {
            return FreeVariable(prefix).Name;
        }

        private void Assign(string name, object value)
        {
            dynamic type = new ExpandoObject();
            type.Value = value;
            type.Assigned = true;
            this.Add(name, type, true);
            this.HasAssignments = true;
        }

        public bool HasDeclarations
        {
            get { return this.DeclaredVariables.Any(); }
        }

        public IEnumerable<ParameterExpression> DeclaredVariables
        {
            get
            {
                var realVars = new List<Variable>();
                var tempVars = new List<Variable>();

                foreach (var variable in this.Variables.Where(v => v.Type == VariableType.Variable))
                {
                    var list = (variable.Name[0] == '_') ? tempVars : realVars;
                    list.Add(variable);
                }

                realVars.Sort();
                tempVars.Sort();

                return realVars.Concat(tempVars).Select(v => v.Expression).ToArray();
            }
        }

        //public IEnumerable<object> AssignedVariables
        //{
        //    get { return GetVariables().Where(v => v.Type.Assigned).Select(v => "" + v.Name + " = " + v.Type.Value); }
        //}

        public CoffeeScript GetRuntime()
        {
            var curScope = this;
            while (curScope.Runtime == null)
            {
                curScope = curScope.Parent;
            }
            return curScope.Runtime;
        }
    }

    class Variable : IComparable<Variable>
    {
        public string Name { get; set; }
        public VariableType Type { get; set; }
        public ParameterExpression Expression { get; set; }

        public int CompareTo(Variable other)
        {
            return this.Name.CompareTo(other.Name);
        }
    }

    enum VariableType
    {
        //public string Value { get; set; }
        //public bool Assigned { get; set; }
        Variable,
        Parameter
    }
}
