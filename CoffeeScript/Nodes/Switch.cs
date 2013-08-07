using System.Collections.Generic;

namespace CoffeeScript
{
    class Switch : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Subject;

                foreach (var @case in this.Cases)
                    yield return @case;

                yield return this.Otherwise;
            }
        }

        public Base Subject { get; set; }
        public List<Base> Cases { get; set; }
        public Base Otherwise { get; set; }
    }
}