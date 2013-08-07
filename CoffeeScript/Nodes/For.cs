using System.Collections.Generic;

namespace CoffeeScript
{
    class For : While
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Body;
                yield return this.Source;
                yield return this.Guard;
                yield return this.Step;
            }
        }

        public Base Source { get; set; }
        public Base Step { get; set; }
    }
}