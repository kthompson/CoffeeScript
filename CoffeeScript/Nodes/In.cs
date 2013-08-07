using System.Collections.Generic;

namespace CoffeeScript
{
    class In : Base
    {
        public override string ToString(string indent, string name = null)
        {
            return base.ToString(indent, this.GetType().Name + (this.Negated ? "!" : ""));
        }

        public bool Negated { get; set; }

        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Object;
                yield return this.Array;
            }
        }

        public Base Object { get; set; }
        public Base Array { get; set; }
    }
}