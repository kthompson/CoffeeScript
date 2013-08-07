using System.Collections.Generic;

namespace CoffeeScript
{
    class Class : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Variable;
                yield return this.Parent;
                yield return this.Body;
            }
        }

        public Base Variable { get; set; }
        public Base Parent { get; set; }
        public Base Body { get; set; }
    }
}