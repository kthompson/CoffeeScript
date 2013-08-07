using System.Collections.Generic;

namespace CoffeeScript
{
    class Obj : Base
    {
        public override IEnumerable<Base> Children
        {
            get 
            {
                return Properties;
            }
        }

        public List<Base> Properties { get; set; }
    }
}