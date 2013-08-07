using System.Collections.Generic;

namespace CoffeeScript
{
    class If : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Condition;
                yield return this.Body;
                yield return this.ElseBody;
            }
        }

        public Base Condition { get; set; }
        public Base Body { get; set; }
        public Base ElseBody { get; set; }
    }
}