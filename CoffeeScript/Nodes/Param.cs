using System.Collections.Generic;

namespace CoffeeScript
{
    class Param : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Name;
                yield return this.Value;
            }
        }

        public Base Name { get; set; }
        public Base Value { get; set; }
    }
}