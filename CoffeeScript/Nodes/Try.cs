using System.Collections.Generic;

namespace CoffeeScript
{
    class Try : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Attempt;
                yield return this.Recovery;
                yield return this.Ensure;
            }
        }

        public Base Attempt { get; set; }
        public Base Recovery { get; set; }
        public Base Ensure { get; set; }
    }
}