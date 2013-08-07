using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CoffeeScript
{
    class Marker
    {
        private readonly Action _resetAction;

        public Marker(Action resetAction)
        {
            _resetAction = resetAction;
        }

        public void Reset()
        {
            _resetAction();
        }
    }

    public class CoffeeParseTreeReader
    {
        public int Indent { get; private set; }
        public int LineNumber { get; private set; }
        public string Type { get; private set; }
        public string Arguments { get; private set; }

        private readonly string _source;
        private int _position = 0;

        public CoffeeParseTreeReader(string source)
        {
            _source = source;
        }

        private Marker GetMarker()
        {
            var indent = Indent;
            var lineNumber  = this.LineNumber;
            var type = this.Type;
            var arguments = this.Arguments;
            var position = this._position;

            return new Marker(() =>
            {
                this.Indent = indent;
                this.LineNumber = lineNumber;
                this.Type = type;
                this.Arguments = arguments;
                this._position = position;
            });
        }

        private int ReadChar()
        {
            if (_position == _source.Length)
                return -1;

            return _source[_position++];
        }

        private int PeekChar(int offset = 0)
        {
            if (_position + offset >= _source.Length)
                return -1;

            return _source[_position + offset];
        }

        public Base Parse()
        {
            return ReadIf(() => true);
        }

        public Base ReadIf(Func<bool> condition)
        {
            var marker = GetMarker();

            this.Indent = SkipWhile(c => c == ' ');
            this.Type = ReadString(c => c != ' ' && c != '\n' && c != '\r');
            
            if (PeekChar() == ' ')
            {
                ReadChar();
                
                this.Arguments = ReadQuotedString().Unquote();
            }
            else
            {
                this.Arguments = string.Empty;
            }

            ConsumeNewLine();

            if (condition())
                return GetBase();

            marker.Reset();
            return null;
        }

        private string ReadQuotedString()
        {
            Func<char, bool> func = c => c != '\n' && c != '\r';

            var sb = new StringBuilder();

            while (true)
            {
                var i = PeekChar();
                if (i == -1)
                    return sb.ToString();

                if (i == '\\')
                {
                    var nl = PeekChar(1);
                    if (nl == '\n' || nl == '\r')
                    {
                        sb.Append((char)ReadChar()); //Consume '\'
                        sb.Append(While(c => c == '\n' || c == '\r').ToArray());
                        continue;
                    }
                }

                if (i == '\n' || i == '\r')
                {
                    return sb.ToString();
                }
                
                sb.Append((char) ReadChar());
            }
        }

        private void ConsumeNewLine()
        {
            if(SkipWhile(c => c == '\n' || c == '\r') > 0)
                this.LineNumber++;
        }

        public IEnumerable<Base> ReadCurrentIndentationItems(int? indent = null)
        {
            var idt = indent ?? this.Indent + 2;

            while (true)
            {
                var node = ReadIf(() => idt == this.Indent);
                if (node == null)
                    break;

                yield return node;
            }
        }

        private int SkipWhile(Func<char, bool> func)
        {
            var start = _position;

            foreach (var source in While(func))
            {
            }

            return _position - start;
        }

        private IEnumerable<char> While(Func<char, bool> func)
        {
            while (true)
            {
                var i = PeekChar();
                if (i == -1)
                    break;

                if (!func((char) i))
                    break;

                yield return (char)ReadChar();
            }
        }

        private string ReadString(Func<char, bool> @while)
        {
            return new string(While(@while).ToArray());
        }
        
        public string Rest
        {
            get { return _source.Substring(_position); }
        }

        private Base GetBase()
        {
            //save our values before we look at children
            var arguments = this.Arguments;
            var type = this.Type;
            var soak = false;
            if (type.EndsWith("?"))
            {
                type = type.Substring(0, type.Length - 1);
                soak = true;
            }

            var children = ReadCurrentIndentationItems().ToList();

            if (!string.IsNullOrEmpty(arguments))
            {
                children.Insert(0, new Literal { Value = arguments });
            }

            var first = children.FirstOrDefault();
            var second = children.Skip(1).FirstOrDefault();
            var third = children.Skip(2).FirstOrDefault();

            switch (type)
            {
                case "Access":
                    return new Access {Name = first, Soak = soak};
                case "Arr":
                    return new Arr {Objects = children, Soak = soak};
                case "Assign":
                    return new Assign {Variable = first, Value = second, Soak = soak};
                case "Block":
                    return new Block {Expressions = children, Soak = soak};
                case "Call":
                    return new Call {Variable = first, Arguments = children.Skip(1).ToList(), Soak = soak};
                case "Class":
                    switch (children.Count)
                    {
                        case 1:
                            if (first is Block)
                                return new Class {Body = first, Soak = soak};

                            Helper.IsTrue(first is Literal);
                            return new Class {Variable = first};
                        case 2:
                            return new Class {Variable = first, Body = second, Soak = soak};

                        default:
                            Helper.IsTrue(children.Count == 3);
                            return new Class {Variable = first, Parent = second, Body = third, Soak = soak};
                    }


                case "Code":
                    return new Code
                        {
                            Parameters = children.OfType<Param>().Cast<Base>().ToList(),
                            Body = children.OfType<Block>().Cast<Base>().FirstOrDefault(),
                            Soak = soak
                        };
                case "Comment":
                    return new Comment {Soak = soak};
                case "Existence":
                    return new Existence {Expression = first, Soak = soak};
                case "Extends":
                    return new Extends {Child = first, Parent = second, Soak = soak};
                case "For":
                    Helper.IsTrue(children.Count == 2 || children.Count == 3);
                    return new For {Body = first, Source = second, Guard = third, Soak = soak};
                case "If":
                    {
                        var body = children.OfType<Block>().FirstOrDefault();
                        var elseBody = children.OfType<Block>().Skip(1).FirstOrDefault();

                        return new If {Condition = first, Body = body, ElseBody = elseBody, Soak = soak};
                    }
                case "In":
                    return new In {Object = first, Array = second, Soak = soak};
                case "In!":
                    return new In {Object = first, Array = second, Negated = true, Soak = soak};
                case "Index":
                    return new Index {IndexValue = first, Soak = soak};
                case "Literal":
                    return new Literal {Value = arguments, Soak = soak};
                case "Obj":
                    return new Obj {Properties = children, Soak = soak};
                case "Op":
                    return new Op {First = second, Second = third, Operator = ((Literal) first).Value, Soak = soak};
                case "Param":
                    return new Param {Name = first, Value = second, Soak = soak};
                case "Parens":
                    return new Parens {Body = first, Soak = soak};
                case "Range":
                    return new Range {From = first, To = second, Soak = soak};
                case "Return":
                    return new Return {Expression = first, Soak = soak};
                case "Slice":
                    return new Slice {Range = first, Soak = soak};
                case "Splat":
                    return new Splat {Name = first, Soak = soak};
                case "Switch":
                    return new Switch {Soak = soak, Subject = first, Cases = children.Skip(1).ToList()};
                case "Throw":
                    return new Throw {Expression = first, Soak = soak};
                case "Try":
                    Helper.IsTrue(children.Count == 3 || children.Count == 2 || children.Count == 1);
                    return new Try {Attempt = first, Recovery = second, Ensure = third, Soak = soak};
                case "Value":
                    return new Value {Base = first, Properties = children.Skip(1).ToList(), Soak = soak};

                case "While":
                    if (children.Count == 2)
                        return new While {Condition = first, Body = second, Soak = soak};

                    Helper.IsTrue(children.Count == 3);
                    return new While {Condition = first, Guard = second, Body = third, Soak = soak};

                case "Undefined":
                    return new Undefined();
                case "Null":
                    return new Null();
                case "Bool":
                    return new Bool {Value = arguments};
                default:
                    throw new NotImplementedException(type);
            }
        }
    }
}
