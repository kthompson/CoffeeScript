using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CoffeeScript
{
    class Program
    {

        static void Main(string[] args)
        {
            Parse();
        }

        private static Base Parse()
        {
            var file = "sample.coffee";

            var results = Helper.Coffee("-n " + file);
            File.WriteAllText(file + ".nodes", results);
            var parser = new CoffeeParseTreeReader(results);
            try
            {
                var result = parser.Parse();
                var s = result.ToString();
                return result;
            }
            catch (Exception exception)
            {
                return null;
            }
        }
    }
}
