using System.Collections.Generic;

namespace CoffeeScript
{
    class Index : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.IndexValue;
            }
        }

        public Base IndexValue { get; set; }
    }
}