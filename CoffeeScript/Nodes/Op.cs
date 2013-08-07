using System.Collections.Generic;

namespace CoffeeScript
{
    class Op : Base
    {
        public string Operator { get; set; }
        
        public override string ToString(string indent, string name = null)
        {
            return base.ToString(indent, this.GetType().Name + " " + this.Operator);
        }

        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.First;
                yield return this.Second;
            }
        }

        public Base First { get; set; }
        public Base Second { get; set; }

        public bool IsChainable
        {
            get
            {
                switch (this.Operator)
                {
                    case "<":
                    case ">":
                    case ">=":
                    case "<=":
                    case "===":
                    case "!==":
                        return true;

                    default:
                        return false;
                } 

            }
        }
    }
}