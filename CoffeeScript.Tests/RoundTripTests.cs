using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace CoffeeScript.Tests
{
    [TestFixture, Timeout(5000)]
    public class RoundTripTests
    {
        [Test]
        public void ParseCoffeeFile([ValueSource("GetCoffee")] string filename)
        {
            var expectedResults = Helper.Coffee(@"-n " + filename);
			Assert.NotNull(expectedResults);
            File.WriteAllText(filename + ".expected", expectedResults);

            var parser = new CoffeeParseTreeReader(expectedResults);

            var result = parser.Parse();
            var s = result.ToString();
            if (s.Length > 0)
                s = s.TrimStart('\r', '\n');

            File.WriteAllText(filename + ".actual", s);
            Assert.AreEqual(expectedResults, s);
        }

        [Test]
        public void CompileCoffeeFile([ValueSource("GetCoffee")] string filename)
        {
            var runtime = new CoffeeScript();
            runtime.ExecuteFile(filename);
        }

        public IEnumerable<string> GetCoffee()
        {
			//HACK: i tried specifying "*.coffee" for the searchPattern but it looks like there is some bug with mono...
			foreach (var item in Directory.EnumerateFiles("Data", "*", SearchOption.AllDirectories)) 
			{
				if(Path.GetExtension(item) == ".coffee")
					yield return item;
			}
        }
    }
}
