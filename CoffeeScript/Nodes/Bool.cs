namespace CoffeeScript
{
    class Bool : Base
    {
        public string Value { get; set; }

        public override bool IsAssignable
        {
            get { return false; }
        }

        public override string ToString()
        {
            return this.ToString("", this.GetType().Name + " " + this.Value);
        }

        public override string ToString(string indent, string name = null)
        {
            return base.ToString(indent, (name ?? this.GetType().Name) + " " + this.Value);
        }
    }
}