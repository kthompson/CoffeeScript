using System.Collections.Generic;

namespace CoffeeScript
{
    class Slice : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Range;
            }
        }

        public Base Range { get; set; }
    }
}