using System.Collections.Generic;

namespace CoffeeScript
{
    class Extends : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Child;
                yield return this.Parent;;
            }
        }

        public Base Child { get; set; }
        public Base Parent { get; set; }
    }
}