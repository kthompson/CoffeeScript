using System.Collections.Generic;

namespace CoffeeScript
{
    class Assign : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Variable;
                yield return this.Value;
            }
        }

        public Base Variable { get; set; }
        public Base Value { get; set; }
    }
}