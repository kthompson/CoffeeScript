namespace CoffeeScript
{
    class Undefined : Base
    {
        public override bool IsAssignable
        {
            get { return false; }
        }
    }
}