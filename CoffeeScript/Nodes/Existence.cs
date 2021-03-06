﻿using System.Collections.Generic;

namespace CoffeeScript
{
    class Existence : Base
    {
        public override IEnumerable<Base> Children
        {
            get
            {
                yield return this.Expression;
            }
        }

        public Base Expression { get; set; }
    }
}