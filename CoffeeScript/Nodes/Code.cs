using System.Collections.Generic;

namespace CoffeeScript
{
    class Code : Base
    {
        public string Name { get; set; }

        public override IEnumerable<Base> Children
        {
            get
            {
                foreach (var parameter in this.Parameters)
                {
                    yield return parameter;
                }

                yield return Body;
            }
        }

        public List<Base> Parameters { get; set; }
        public Base Body { get; set; }
    }
}