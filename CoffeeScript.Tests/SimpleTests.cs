using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace CoffeeScript.Tests
{
    [TestFixture]
    class SimpleTests
    {
        class Test
        {
            public int field;
        }

        [Test]
        public void SetExportsField()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"exports.value = 1
exports.value2 = 2
exports.value3 = 3
exports.value4 = 4");

            Assert.AreEqual(1, exports.value);
            Assert.AreEqual(2, exports.value2);
            Assert.AreEqual(3, exports.value3);
            Assert.AreEqual(4, exports.value4);
        }

        [Test]
        public void ShouldCreateLocalVariables()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"value = 1
exports.value = value");

            Assert.AreEqual(1, exports.value);
        }

        [Test]
        public void ShouldCreateArrays()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"exports.value = [1,2,3,4,5]");

            Assert.AreEqual(5, exports.value.Length);
            Assert.AreEqual(1, exports.value[0]);
            Assert.AreEqual(2, exports.value[1]);
            Assert.AreEqual(3, exports.value[2]);
            Assert.AreEqual(4, exports.value[3]);
            Assert.AreEqual(5, exports.value[4]);
            
        }

        [Test]
        public void ShouldExecuteFunctions()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(
@"func = ->
  exports.value = 1

func()");

            Assert.AreEqual(1, exports.value);
        }

        [Test]
        public void FunctionsCanPassVariables()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(
@"func = (s)->
  s.value = 1

func(exports)");

            Assert.AreEqual(1, exports.value);
        }

        [Test]
        public void SupportsComparisons()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"exports.value = 200 > 127");

            Assert.AreEqual(true, exports.value);
        }

        [Test]
        public void SupportsChains()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"cholesterol = 127

healthy = 200 > cholesterol > 60

exports.value = healthy");

            Assert.AreEqual(true, exports.value);
        }

        [Test]
        public void SupportsFunctions()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
# Functions:
exports.square = (x) -> x * x
");

            Assert.AreEqual(1, exports.square(1));
            Assert.AreEqual(4, exports.square(2));
            Assert.AreEqual(9, exports.square(3));
            Assert.AreEqual(16, exports.square(4));
            Assert.AreEqual(25, exports.square(5));
        }

        [Test]
        public void SupportsPreIncrement()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
exports.value = 1
exports.inc = () -> ++exports.value
");

            Assert.AreEqual(1, exports.value);
            Assert.AreEqual(1, exports.inc());
            Assert.AreEqual(2, exports.value);
            Assert.AreEqual(2, exports.inc());
            Assert.AreEqual(3, exports.value);
        }

        [Test]
        public void SupportsPostIncrement()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
exports.value = 1
exports.inc = () -> exports.value++
");

            Assert.AreEqual(1, exports.value);
            Assert.AreEqual(2, exports.inc());
            Assert.AreEqual(2, exports.value);
            Assert.AreEqual(3, exports.inc());
            Assert.AreEqual(3, exports.value);
        }

        [Test]
        public void SupportsObjects()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
# Objects:
square = (x) -> x * x

exports.math =
  cube:   (x) -> x * square x
");
            Assert.AreEqual(1, exports.math.cube(1));
            Assert.AreEqual(8, exports.math.cube(2));
            Assert.AreEqual(27, exports.math.cube(3));
            Assert.AreEqual(64, exports.math.cube(4));
            Assert.AreEqual(125, exports.math.cube(5));
        }

        [Test]
        public void SupportsNestedObjects()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
# Objects:
square = (x) -> x * x

exports.math =
  cube:   (x) -> x * square x
  nested: 
    one: 1
    two: 2
");


            Assert.AreEqual(1, exports.math.nested.one);
            Assert.AreEqual(2, exports.math.nested.two);

            Assert.AreEqual(1, exports.math.cube(1));
            Assert.AreEqual(8, exports.math.cube(2));
            Assert.AreEqual(27, exports.math.cube(3));
            Assert.AreEqual(64, exports.math.cube(4));
            Assert.AreEqual(125, exports.math.cube(5));
        }

        [Test]
        public void SupportsObjects2()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
# Objects:
exports.math = {}
exports.math.root   = 1
");

            dynamic math = exports.math;
            Assert.AreEqual(1, math.root);
        }


        [Test]
        public void SupportsObjectAssignments()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
testObject = 
    value1: 1
    value15: 15
    value2: 2

{value1, value2} = testObject

exports.value1 = value1
exports.value2 = value2
");
            
            Assert.AreEqual(1, exports.value1);
            Assert.AreEqual(2, exports.value2);
        }

        [Test]
        public void RequireAssemblyTest()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"exports.mscorlib = require('mscorlib')");
            var mscorlib = exports.mscorlib;
            mscorlib.System.Console.WriteLine("hello");
        }

        [Test]
        public void HelloWorldTest()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(
@"Console = require('mscorlib').System.Console
Console.WriteLine ""Hello World""");

        }


        [Test, ExpectedException(typeof(MissingMemberException))]
        public void ShouldThrowReferenceError()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
# Objects:
exports.math =
  cube:   (x) -> x * square x
");
            //should throw reference error since square is not defined
            var result = exports.math.cube(1);
        }

        [Test]
        public void ShouldNotThrowReferenceError()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
exports.math =
  cube:   (x) -> x * square x

square = (x) -> x * x
");
            //should throw reference error since square is not defined
            //TODO: "square" is defined in the wrong lambda
            Assert.AreEqual(1, exports.math.cube(1));
            Assert.AreEqual(8, exports.math.cube(2));
            Assert.AreEqual(27, exports.math.cube(3));
            Assert.AreEqual(64, exports.math.cube(4));
            Assert.AreEqual(125, exports.math.cube(5));
        }

        [Test]
        public void ShouldNotThrowReferenceErrorOnLateBoundObjects()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
exports.math =
  cube:   (x) -> x * square x

defineSquare = () ->
    this.square = (x) -> x * x

defineSquare()
");
            //should throw reference error since square is not defined
            //TODO: "square" is defined in the wrong lambda
            Assert.AreEqual(1, exports.math.cube(1));
            Assert.AreEqual(8, exports.math.cube(2));
            Assert.AreEqual(27, exports.math.cube(3));
            Assert.AreEqual(64, exports.math.cube(4));
            Assert.AreEqual(125, exports.math.cube(5));
        }

        [Test]
        public void VariableReferenceShouldFallbackToThisContext()
        {
            var runtime = new CoffeeScript();
            dynamic exports = runtime.Execute(@"
this.value = 5
exports.getValue = () -> value
");
            //should throw reference error since square is not defined
            //TODO: "square" is defined in the wrong lambda
            Assert.AreEqual(5, exports.getValue());
        }
    }
}
