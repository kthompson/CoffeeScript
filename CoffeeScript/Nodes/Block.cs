using System.Collections.Generic;

namespace CoffeeScript
{
    class Block : Base
    {
        public List<Base> Expressions { get; set; }

        public Block()
        {
            this.Expressions = new List<Base>();
        }

        public override IEnumerable<Base> Children
        {
            get 
            {
                return Expressions;
            }
        }
    }
}