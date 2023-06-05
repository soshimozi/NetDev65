using System.Text;
using System.Xml;
using static System.Int32;

namespace Dev65.XObj;

public static class Parser
{
    private static readonly Stack<string> Tags = new();

    private static Module? _module;

    private static Section? _section;

    private static string? _sect = string.Empty;

    private static string _chars = string.Empty;

    private static StringBuilder _bytes = new();
    private static readonly Stack<object?> Stack = new();

    public static object? Parse(string fileName)
    {
        //var stack = new Stack<object?>();

        (Expr? lhs, Expr? rhs) PopTwo()
        {
            var rhs = Stack.Pop() as Expr;
            var lhs = Stack.Pop() as Expr;
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

                        Tags.Push(localName);

                        switch (localName)
                        {
                            case "library":
                                Stack.Push(new Library());
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

                                Stack.Push(_module);
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
                                var expression = Stack.Pop() as Expr;
                                _module?.AddGlobal(symbol ?? string.Empty, expression);
                                break;
                        }

                        break;
                    }
                    case XmlNodeType.Text:
                    {
                        var text = reader.Value.Trim();

                        _chars = new string(text);

                        switch (Tags.Peek())
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
                                Stack.Push(text);
                                break;
                        }

                        break;
                    }
                    case XmlNodeType.EndElement:
                    {
                        var localName = reader.LocalName;
                        Tags.Pop();

                        Expr? lhs, rhs, expr;

                        switch (localName)
                        {
                            case "add":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(new BinaryExpr.Add(lhs, rhs));
                                break;

                            case "and":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(new BinaryExpr.And(lhs, rhs));
                                break;

                            case "byte":
                                expr = Stack.Pop() as Expr;
                                _section?.AddByte(expr);
                                break;

                            case "cpl":
                                expr = Stack.Pop() as Expr;
                                Stack.Push(new UnaryExpr.Cpl(expr));
                                break;

                            case "eq":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(new BinaryExpr.Eq(lhs, rhs));
                                break;

                            case "ext":
                                Stack.Push(new Extern(_chars));
                                break;

                            case "ge":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(new BinaryExpr.Ge(lhs, rhs));
                                break;

                            case "gt":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(new BinaryExpr.Gt(lhs, rhs));
                                break;

                            case "gbl":
                                expr = Stack.Pop() as Expr;
                                var sym = Stack.Pop() as string ?? string.Empty;
                                _module?.AddGlobal(sym, expr);
                                break;

                            case "le":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(new BinaryExpr.Le(lhs, rhs));
                                break;

                            case "lt":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(new BinaryExpr.Lt(lhs, rhs));
                                break;

                            case "land":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(BinaryExpressionFactory.LogicalAnd(lhs, rhs));
                                break;

                            case "lor":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(BinaryExpressionFactory.LogicalOr(lhs, rhs));
                                break;

                            case "long":
                                expr = Stack.Pop() as Expr;
                                _section?.AddLong(expr);
                                break;

                            case "library":
                                var modules = new Stack<Module?>();

                                while (Stack.Peek() as Module is { } module)
                                {
                                    Stack.Pop();
                                    modules.Push(module);
                                }

                                var library = Stack.Peek() as Library;
                                while (modules.Count > 0)
                                {
                                    library?.AddModule(modules.Pop());
                                }
                                break;

                            case "mod":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(BinaryExpressionFactory.Mod(lhs, rhs));
                                break;

                            case "ne":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(BinaryExpressionFactory.NotEqual(lhs, rhs));
                                break;

                            case "neg":
                                Stack.Push(UnaryFactory.Negate(Stack.Pop() as Expr));
                                break;

                            case "not":
                                Stack.Push(UnaryFactory.Not(Stack.Pop() as Expr));
                                break;

                            case "or":
                                (lhs, rhs) = PopTwo();
                                Stack.Push(BinaryExpressionFactory.Subtract(lhs, rhs));
                                break;


                            case "val":
                                _ = TryParse(_chars, out var value);
                                Stack.Push(_sect != null
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

            return Stack.Pop();
    }
}
