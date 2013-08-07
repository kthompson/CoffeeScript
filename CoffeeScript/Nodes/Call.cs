using System.Collections.Generic;

namespace CoffeeScript
{
    class Call : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Variable;

                foreach (var property in Arguments)
                    yield return property;
            }
        }

        public Base Variable { get; set; }
        public List<Base> Arguments { get; set; }
    }
}
