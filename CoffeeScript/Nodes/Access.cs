using System.Collections.Generic;

namespace CoffeeScript
{
    class Access : Base
    {
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