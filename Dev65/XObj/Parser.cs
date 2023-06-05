using System.Text;
using System.Xml;
using static System.Int32;

namespace Dev65.XObj;

public class Parser
{
    private readonly Stack<string> _tags = new();

    private Module? _module;

    private Section? _section;

    private string? _sect = string.Empty;

    private string _chars = string.Empty;

    private StringBuilder _bytes = new();
    private readonly Stack<object?> _stack = new();

    public object? Parse(string fileName)
    {
        //var stack = new Stack<object?>();

        (Expr? lhs, Expr? rhs) PopTwo()
        {
            var rhs = _stack.Pop() as Expr;
            var lhs = _stack.Pop() as Expr;
            return (lhs, rhs);
        }

        using var fileStream = new FileStream(fileName, FileMode.Open);

            using var reader = XmlReader.Create(fileStream);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                    {
                        var localName = reader.LocalName;

                        _tags.Push(localName);

                        switch (localName)
                        {
                            case "library":
                                _stack.Push(new Library());
                                break;

                            case "module":
                                var endian = reader.GetAttribute("endian");
                                var byteSizeAttribute = reader.GetAttribute("byteSize");

                                _ = TryParse(byteSizeAttribute, out var byteSize);

                                _module = new Module(
                                    reader.GetAttribute("target"),
                                    endian != "little",
                                    byteSize);

                                if (reader.GetAttribute("name") != null)
                                    _module.Name = reader.GetAttribute("name");

                                _stack.Push(_module);
                                break;

                            case "section":
                                var sectionName = reader.GetAttribute("name") ?? string.Empty;
                                var addressAttr = reader.GetAttribute("addr");

                                if (addressAttr != null && long.TryParse(addressAttr,
                                        System.Globalization.NumberStyles.HexNumber, null, out var startAddress))
                                    _section = _module?.FindSection(sectionName, startAddress);
                                else
                                    _section = _module?.FindSection(sectionName);

                                _bytes.Clear();

                                break;

                            case "val":
                                _sect = reader.GetAttribute("sect");
                                break;

                            case "gbl":
                                var symbol = reader.GetAttribute("symbol");
                                var expression = _stack.Pop() as Expr;
                                _module?.AddGlobal(symbol ?? string.Empty, expression);
                                break;
                        }

                        break;
                    }
                    case XmlNodeType.Text:
                    {
                        var text = reader.Value.Trim();

                        _chars = new string(text);

                        switch (_tags.Peek())
                        {
                            case "section":
                            {
                                var span = _module?.GetByteSize() / 4;
                                _bytes = new StringBuilder(text);

                                while (_bytes.Length >= span)
                                {
                                    _section?.AddByte(long.Parse(_bytes.ToString(0, span ?? 0),
                                        System.Globalization.NumberStyles.HexNumber));
                                    _bytes.Remove(0, span ?? 0);
                                }

                                break;
                            }
                            case "gbl":
                                _stack.Push(text);
                                break;
                        }

                        break;
                    }
                    case XmlNodeType.EndElement:
                    {
                        var localName = reader.LocalName;
                        _tags.Pop();

                        Expr? lhs, rhs, expr;

                        switch (localName)
                        {
                            case "add":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(new BinaryExpr.Add(lhs, rhs));
                                break;

                            case "and":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(new BinaryExpr.And(lhs, rhs));
                                break;

                            case "byte":
                                expr = _stack.Pop() as Expr;
                                _section?.AddByte(expr);
                                break;

                            case "cpl":
                                expr = _stack.Pop() as Expr;
                                _stack.Push(new UnaryExpr.Cpl(expr));
                                break;

                            case "eq":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(new BinaryExpr.Eq(lhs, rhs));
                                break;

                            case "ext":
                                _stack.Push(new Extern(_chars));
                                break;

                            case "ge":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(new BinaryExpr.Ge(lhs, rhs));
                                break;

                            case "gt":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(new BinaryExpr.Gt(lhs, rhs));
                                break;

                            case "gbl":
                                expr = _stack.Pop() as Expr;
                                var sym = _stack.Pop() as string ?? string.Empty;
                                _module?.AddGlobal(sym, expr);
                                break;

                            case "le":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(new BinaryExpr.Le(lhs, rhs));
                                break;

                            case "lt":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(new BinaryExpr.Lt(lhs, rhs));
                                break;

                            case "land":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(BinaryExpressionFactory.LogicalAnd(lhs, rhs));
                                break;

                            case "lor":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(BinaryExpressionFactory.LogicalOr(lhs, rhs));
                                break;

                            case "long":
                                expr = _stack.Pop() as Expr;
                                _section?.AddLong(expr);
                                break;

                            case "library":
                                var modules = new Stack<Module?>();

                                while (_stack.Peek() as Module is { } module)
                                {
                                    _stack.Pop();
                                    modules.Push(module);
                                }

                                var library = _stack.Peek() as Library;
                                while (modules.Count > 0)
                                {
                                    library?.AddModule(modules.Pop());
                                }
                                break;

                            case "mod":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(BinaryExpressionFactory.Mod(lhs, rhs));
                                break;

                            case "ne":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(BinaryExpressionFactory.NotEqual(lhs, rhs));
                                break;

                            case "neg":
                                _stack.Push(UnaryFactory.Negate(_stack.Pop() as Expr));
                                break;

                            case "not":
                                _stack.Push(UnaryFactory.Not(_stack.Pop() as Expr));
                                break;

                            case "or":
                                (lhs, rhs) = PopTwo();
                                _stack.Push(BinaryExpressionFactory.Subtract(lhs, rhs));
                                break;


                            case "val":
                                _ = TryParse(_chars, out var value);
                                _stack.Push(_sect != null
                                    ? new Value(_module?.FindSection(_sect), value)
                                    : new Value(null, value));

                                break;

                        }

                        break;
                    }
                    default:
                        break;
                }
            }

            return _stack.Pop();
    }
}
