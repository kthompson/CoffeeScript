using System.Collections.Generic;

namespace CoffeeScript
{
    class Arr : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                return Objects;
            }
        }

        public List<Base> Objects { get; set; }
    }
}