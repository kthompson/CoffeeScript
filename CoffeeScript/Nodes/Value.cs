using System.Collections.Generic;

namespace CoffeeScript
{
    class Value : Base
    {
        public Value()
        {
            this.Properties = new List<Base>();
        }

        public bool IsObject
        {
            get
            {
                if (HasProperties)
                    return false;

                return this.Base is Obj;
            }
        }

        public override bool IsAssignable
        {
            get { return HasProperties || this.Base.IsAssignable; }
        }

        public bool HasProperties
        {
            get { return this.Properties.Count > 0; }
        }


        public override IEnumerable<Base> Children
        {
            get 
            {
                yield return this.Base; 
                foreach (var property in Properties)
                    yield return property;
            }
        }

        public Base Base { get; set; }
        public List<Base> Properties { get; set; }

    }
}