using System.Collections.Generic;

namespace CoffeeScript
{
    class Parens : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Body;
            }
        }

        public Base Body { get; set; }
    }
}