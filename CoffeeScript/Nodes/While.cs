using System.Collections.Generic;

namespace CoffeeScript
{
    class While : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Condition;
                yield return this.Guard;
                yield return this.Body;
            }
        }

        public Base Condition { get; set; }
        public Base Guard { get; set; }
        public Base Body { get; set; }
    }
}