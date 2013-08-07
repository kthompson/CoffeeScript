using System.Collections.Generic;

namespace CoffeeScript
{
    class Return : Base
    {
        public Return()
        {
        }

        public Base Expression { get; set; }

        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Expression;
            }
        }
    }
}