using System.Collections.Generic;

namespace CoffeeScript
{
    class Splat : Base
    {
        public override bool IsAssignable
        {
            get { return true; }
        }

        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Name;
            }
        }

        public Base Name { get; set; }
    }
}