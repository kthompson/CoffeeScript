using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoffeeScript
{
    public abstract class Base
    {
        public virtual IEnumerable<Base> Children {
            get
            {
                return new Base[]{};
            }
        }

        public virtual bool IsAssignable
        {
            get { return false; }
        }

        public bool Soak { get; set;  }

        public virtual string ToString(string indent, string name = null)
        {
            if(name  == null)
                name = this.GetType().Name;

            var sb = new StringBuilder();
            sb.Append(Environment.NewLine + indent + name);
            if (this.Soak)
                sb.Append("?");

            EachChild(node => sb.Append(node.ToString(indent + "  ")));

            return sb.ToString();
        }

        public Base Cache(string name)
        {
            return new Assign
            {
                Variable = new Literal
                {
                    Value = name
                }, 
                Value = this
            };
        }

        public override string ToString()
        {
            return this.ToString("", this.GetType().Name);
        }

        private void EachChild(Action<Base> action)
        {
            this.EachChild(@base =>
                {
                    action(@base);
                    return true;
                });
        }

        private void EachChild(Func<Base, bool> action)
        {
            foreach (var child in this.Children.Where(n => n != null))
            {
                if (action(child) == false)
                    break;
            }
        }
    }
}