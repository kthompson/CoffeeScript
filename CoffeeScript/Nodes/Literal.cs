using System;
using System.Text.RegularExpressions;

namespace CoffeeScript
{
    class Literal : Base
    {
        private const string IdentifierStr = "[$A-Za-z_\\x7f-\\uffff][$\\w\\x7f-\\uffff]*";

        private static readonly Regex Identifier = new Regex("^" + IdentifierStr + "$");
        public override bool IsAssignable
        {
            get { return Identifier.IsMatch(this.Value); }
        }

        public override string ToString()
        {
            return " \"" + this.Value + "\"";
        }

        public override string ToString(string indent, string name = null)
        {
            return this.ToString();
        }

        public string Value { get; set; }
    }
}