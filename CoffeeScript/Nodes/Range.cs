using System.Collections.Generic;

namespace CoffeeScript
{
    class Range : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.From;
                yield return this.To;
            }
        }

        public Base From { get; set; }
        public Base To { get; set; }
    }
}