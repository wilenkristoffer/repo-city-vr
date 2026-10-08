using System;
using System.Collections.Generic;
using System.Globalization;

namespace RepoCity
{
    public class MiniLangResult
    {
        public bool success;
        public string errorMessage;
        public List<string> stdoutLines;
    }

    // Tiny sandboxed C-like scripting language: numbers (double), strings, booleans,
    // variables, arithmetic, comparisons, &&/||/!, if/else, while, print(expr).
    // No functions, no arrays, no file/network/reflection access - tree-walking
    // interpreter only. Deterministic: no Random, no Time, no wall-clock access.
    public static class MiniLang
    {
        public static MiniLangResult Run(string source, int maxSteps = 200000)
        {
            var result = new MiniLangResult();
            var interpreter = new MiniLangInterpreter(maxSteps);
            try
            {
                var lexer = new MiniLangLexer(source);
                List<MiniLangToken> tokens = lexer.ScanTokens();
                var parser = new MiniLangParser(tokens);
                List<MiniLangStmt> statements = parser.ParseProgram();
                interpreter.Run(statements);
                result.success = true;
                result.errorMessage = null;
            }
            catch (MiniLangError e)
            {
                result.success = false;
                result.errorMessage = "Line " + e.line + ": " + e.Message;
            }
            catch (Exception e)
            {
                result.success = false;
                result.errorMessage = "Internal error: " + e.Message;
            }
            result.stdoutLines = interpreter.stdout;
            return result;
        }
    }

    // Thrown for both parse-time and run-time problems; always caught inside MiniLang.Run.
    class MiniLangError : Exception
    {
        public int line;

        public MiniLangError(string message, int line) : base(message)
        {
            this.line = line;
        }
    }

    enum MiniLangTokType
    {
        Number, String, Identifier,
        Plus, Minus, Star, Slash, Percent,
        Assign, EqEq, NotEq, Less, Greater, LessEq, GreaterEq,
        AndAnd, OrOr, Bang,
        LParen, RParen, LBrace, RBrace, Semicolon, Comma,
        If, Else, While, Print, True, False,
        Eof
    }

    class MiniLangToken
    {
        public MiniLangTokType type;
        public string text;
        public double numberValue;
        public string stringValue;
        public int line;
    }

    class MiniLangLexer
    {
        static readonly Dictionary<string, MiniLangTokType> Keywords = new Dictionary<string, MiniLangTokType>
        {
            { "if", MiniLangTokType.If },
            { "else", MiniLangTokType.Else },
            { "while", MiniLangTokType.While },
            { "print", MiniLangTokType.Print },
            { "true", MiniLangTokType.True },
            { "false", MiniLangTokType.False },
        };

        readonly string source;
        int start;
        int current;
        int line = 1;
        readonly List<MiniLangToken> tokens = new List<MiniLangToken>();

        public MiniLangLexer(string source)
        {
            this.source = source ?? "";
        }

        public List<MiniLangToken> ScanTokens()
        {
            while (!IsAtEnd())
            {
                start = current;
                ScanOne();
            }
            tokens.Add(new MiniLangToken { type = MiniLangTokType.Eof, text = "", line = line });
            return tokens;
        }

        bool IsAtEnd()
        {
            return current >= source.Length;
        }

        char Advance()
        {
            char c = source[current];
            current++;
            return c;
        }

        char PeekChar()
        {
            return IsAtEnd() ? '\0' : source[current];
        }

        char PeekNextChar()
        {
            return current + 1 >= source.Length ? '\0' : source[current + 1];
        }

        bool MatchChar(char expected)
        {
            if (IsAtEnd() || source[current] != expected) return false;
            current++;
            return true;
        }

        void AddToken(MiniLangTokType type)
        {
            tokens.Add(new MiniLangToken { type = type, text = source.Substring(start, current - start), line = line });
        }

        void ScanOne()
        {
            char c = Advance();
            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    break;
                case '\n':
                    line++;
                    break;
                case '(': AddToken(MiniLangTokType.LParen); break;
                case ')': AddToken(MiniLangTokType.RParen); break;
                case '{': AddToken(MiniLangTokType.LBrace); break;
                case '}': AddToken(MiniLangTokType.RBrace); break;
                case ';': AddToken(MiniLangTokType.Semicolon); break;
                case ',': AddToken(MiniLangTokType.Comma); break;
                case '+': AddToken(MiniLangTokType.Plus); break;
                case '-': AddToken(MiniLangTokType.Minus); break;
                case '*': AddToken(MiniLangTokType.Star); break;
                case '%': AddToken(MiniLangTokType.Percent); break;
                case '/':
                    if (MatchChar('/'))
                    {
                        while (PeekChar() != '\n' && !IsAtEnd()) Advance();
                    }
                    else
                    {
                        AddToken(MiniLangTokType.Slash);
                    }
                    break;
                case '=': AddToken(MatchChar('=') ? MiniLangTokType.EqEq : MiniLangTokType.Assign); break;
                case '!': AddToken(MatchChar('=') ? MiniLangTokType.NotEq : MiniLangTokType.Bang); break;
                case '<': AddToken(MatchChar('=') ? MiniLangTokType.LessEq : MiniLangTokType.Less); break;
                case '>': AddToken(MatchChar('=') ? MiniLangTokType.GreaterEq : MiniLangTokType.Greater); break;
                case '&':
                    if (MatchChar('&')) AddToken(MiniLangTokType.AndAnd);
                    else throw new MiniLangError("unexpected character '&' (did you mean '&&'?)", line);
                    break;
                case '|':
                    if (MatchChar('|')) AddToken(MiniLangTokType.OrOr);
                    else throw new MiniLangError("unexpected character '|' (did you mean '||'?)", line);
                    break;
                case '"':
                    ScanString();
                    break;
                default:
                    if (char.IsDigit(c))
                    {
                        ScanNumber();
                    }
                    else if (char.IsLetter(c) || c == '_')
                    {
                        ScanIdentifier();
                    }
                    else
                    {
                        throw new MiniLangError("unexpected character '" + c + "'", line);
                    }
                    break;
            }
        }

        void ScanString()
        {
            int startLine = line;
            var sb = new System.Text.StringBuilder();
            while (PeekChar() != '"' && !IsAtEnd())
            {
                char c = Advance();
                if (c == '\n')
                {
                    line++;
                    sb.Append(c);
                }
                else if (c == '\\' && !IsAtEnd())
                {
                    char esc = Advance();
                    if (esc == 'n') sb.Append('\n');
                    else if (esc == 't') sb.Append('\t');
                    else if (esc == '"') sb.Append('"');
                    else if (esc == '\\') sb.Append('\\');
                    else sb.Append(esc);
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (IsAtEnd()) throw new MiniLangError("unterminated string literal", startLine);
            Advance(); // closing quote
            tokens.Add(new MiniLangToken { type = MiniLangTokType.String, text = sb.ToString(), stringValue = sb.ToString(), line = startLine });
        }

        void ScanNumber()
        {
            while (char.IsDigit(PeekChar())) Advance();
            if (PeekChar() == '.' && char.IsDigit(PeekNextChar()))
            {
                Advance();
                while (char.IsDigit(PeekChar())) Advance();
            }
            string text = source.Substring(start, current - start);
            double value = double.Parse(text, CultureInfo.InvariantCulture);
            tokens.Add(new MiniLangToken { type = MiniLangTokType.Number, text = text, numberValue = value, line = line });
        }

        void ScanIdentifier()
        {
            while (char.IsLetterOrDigit(PeekChar()) || PeekChar() == '_') Advance();
            string text = source.Substring(start, current - start);
            MiniLangTokType type;
            if (Keywords.TryGetValue(text, out type))
            {
                tokens.Add(new MiniLangToken { type = type, text = text, line = line });
            }
            else
            {
                tokens.Add(new MiniLangToken { type = MiniLangTokType.Identifier, text = text, line = line });
            }
        }
    }

    abstract class MiniLangExpr
    {
        public int line;
    }

    class NumberExpr : MiniLangExpr { public double value; }
    class StringExpr : MiniLangExpr { public string value; }
    class BoolExpr : MiniLangExpr { public bool value; }
    class VariableExpr : MiniLangExpr { public string name; }
    class AssignExpr : MiniLangExpr { public string name; public MiniLangExpr value; }
    class UnaryExpr : MiniLangExpr { public string op; public MiniLangExpr right; }
    class BinaryExpr : MiniLangExpr { public string op; public MiniLangExpr left; public MiniLangExpr right; }
    class LogicalExpr : MiniLangExpr { public string op; public MiniLangExpr left; public MiniLangExpr right; }
    class CallExpr : MiniLangExpr { public string name; public List<MiniLangExpr> args; }

    abstract class MiniLangStmt
    {
        public int line;
    }

    class ExprStmt : MiniLangStmt { public MiniLangExpr expr; }
    class PrintStmt : MiniLangStmt { public MiniLangExpr expr; }
    class BlockStmt : MiniLangStmt { public List<MiniLangStmt> statements; }
    class IfStmt : MiniLangStmt { public MiniLangExpr condition; public MiniLangStmt thenBranch; public MiniLangStmt elseBranch; }
    class WhileStmt : MiniLangStmt { public MiniLangExpr condition; public MiniLangStmt body; }

    class MiniLangParser
    {
        const int MaxParseDepth = 200;

        readonly List<MiniLangToken> tokens;
        int pos;
        int depth;

        public MiniLangParser(List<MiniLangToken> tokens)
        {
            this.tokens = tokens;
        }

        // Guards every recursive-descent entry point against unbounded recursion
        // (deeply nested parens, unary chains, blocks, etc.) that would otherwise
        // overflow the C# call stack with an uncatchable StackOverflowException.
        void EnterRule(int line)
        {
            depth++;
            if (depth > MaxParseDepth) throw new MiniLangError("expression/statement nested too deeply", line);
        }

        void ExitRule()
        {
            depth--;
        }

        public List<MiniLangStmt> ParseProgram()
        {
            var statements = new List<MiniLangStmt>();
            while (!IsAtEnd()) statements.Add(Statement());
            return statements;
        }

        MiniLangToken Peek() { return tokens[pos]; }
        MiniLangToken Previous() { return tokens[pos - 1]; }
        bool IsAtEnd() { return Peek().type == MiniLangTokType.Eof; }
        MiniLangToken Advance() { if (!IsAtEnd()) pos++; return Previous(); }
        bool Check(MiniLangTokType type) { return !IsAtEnd() && Peek().type == type; }

        bool Match(MiniLangTokType type)
        {
            if (!Check(type)) return false;
            Advance();
            return true;
        }

        MiniLangToken Consume(MiniLangTokType type, string message)
        {
            if (Check(type)) return Advance();
            throw new MiniLangError(message, Peek().line);
        }

        MiniLangStmt Statement()
        {
            int line = Peek().line;
            EnterRule(line);
            try
            {
                if (Match(MiniLangTokType.If)) return IfStatement(line);
                if (Match(MiniLangTokType.While)) return WhileStatement(line);
                if (Match(MiniLangTokType.Print)) return PrintStatement(line);
                if (Match(MiniLangTokType.LBrace)) return new BlockStmt { statements = Block(), line = line };
                return ExpressionStatement(line);
            }
            finally { ExitRule(); }
        }

        List<MiniLangStmt> Block()
        {
            int line = Peek().line;
            EnterRule(line);
            try
            {
                var statements = new List<MiniLangStmt>();
                while (!Check(MiniLangTokType.RBrace) && !IsAtEnd()) statements.Add(Statement());
                Consume(MiniLangTokType.RBrace, "expected '}' to close block");
                return statements;
            }
            finally { ExitRule(); }
        }

        MiniLangStmt IfStatement(int line)
        {
            Consume(MiniLangTokType.LParen, "expected '(' after 'if'");
            MiniLangExpr condition = Expression();
            Consume(MiniLangTokType.RParen, "expected ')' after if condition");
            MiniLangStmt thenBranch = Statement();
            MiniLangStmt elseBranch = null;
            if (Match(MiniLangTokType.Else)) elseBranch = Statement();
            return new IfStmt { condition = condition, thenBranch = thenBranch, elseBranch = elseBranch, line = line };
        }

        MiniLangStmt WhileStatement(int line)
        {
            Consume(MiniLangTokType.LParen, "expected '(' after 'while'");
            MiniLangExpr condition = Expression();
            Consume(MiniLangTokType.RParen, "expected ')' after while condition");
            MiniLangStmt body = Statement();
            return new WhileStmt { condition = condition, body = body, line = line };
        }

        MiniLangStmt PrintStatement(int line)
        {
            Consume(MiniLangTokType.LParen, "expected '(' after 'print'");
            MiniLangExpr expr = Expression();
            Consume(MiniLangTokType.RParen, "expected ')' after print argument");
            Consume(MiniLangTokType.Semicolon, "expected ';' after print statement");
            return new PrintStmt { expr = expr, line = line };
        }

        MiniLangStmt ExpressionStatement(int line)
        {
            MiniLangExpr expr = Expression();
            Consume(MiniLangTokType.Semicolon, "expected ';' after expression");
            return new ExprStmt { expr = expr, line = line };
        }

        MiniLangExpr Expression()
        {
            EnterRule(Peek().line);
            try { return Assignment(); }
            finally { ExitRule(); }
        }

        MiniLangExpr Assignment()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangExpr expr = Or();
                if (Match(MiniLangTokType.Assign))
                {
                    MiniLangToken eq = Previous();
                    MiniLangExpr value = Assignment();
                    var ve = expr as VariableExpr;
                    if (ve == null) throw new MiniLangError("invalid assignment target", eq.line);
                    return new AssignExpr { name = ve.name, value = value, line = eq.line };
                }
                return expr;
            }
            finally { ExitRule(); }
        }

        MiniLangExpr Or()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangExpr expr = And();
                while (Match(MiniLangTokType.OrOr))
                {
                    MiniLangToken op = Previous();
                    MiniLangExpr right = And();
                    expr = new LogicalExpr { op = "||", left = expr, right = right, line = op.line };
                }
                return expr;
            }
            finally { ExitRule(); }
        }

        MiniLangExpr And()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangExpr expr = Equality();
                while (Match(MiniLangTokType.AndAnd))
                {
                    MiniLangToken op = Previous();
                    MiniLangExpr right = Equality();
                    expr = new LogicalExpr { op = "&&", left = expr, right = right, line = op.line };
                }
                return expr;
            }
            finally { ExitRule(); }
        }

        MiniLangExpr Equality()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangExpr expr = Comparison();
                while (Check(MiniLangTokType.EqEq) || Check(MiniLangTokType.NotEq))
                {
                    MiniLangToken op = Advance();
                    MiniLangExpr right = Comparison();
                    expr = new BinaryExpr { op = op.type == MiniLangTokType.EqEq ? "==" : "!=", left = expr, right = right, line = op.line };
                }
                return expr;
            }
            finally { ExitRule(); }
        }

        MiniLangExpr Comparison()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangExpr expr = Term();
                while (Check(MiniLangTokType.Less) || Check(MiniLangTokType.Greater) || Check(MiniLangTokType.LessEq) || Check(MiniLangTokType.GreaterEq))
                {
                    MiniLangToken op = Advance();
                    MiniLangExpr right = Term();
                    expr = new BinaryExpr { op = OpText(op.type), left = expr, right = right, line = op.line };
                }
                return expr;
            }
            finally { ExitRule(); }
        }

        MiniLangExpr Term()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangExpr expr = Factor();
                while (Check(MiniLangTokType.Plus) || Check(MiniLangTokType.Minus))
                {
                    MiniLangToken op = Advance();
                    MiniLangExpr right = Factor();
                    expr = new BinaryExpr { op = op.type == MiniLangTokType.Plus ? "+" : "-", left = expr, right = right, line = op.line };
                }
                return expr;
            }
            finally { ExitRule(); }
        }

        MiniLangExpr Factor()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangExpr expr = Unary();
                while (Check(MiniLangTokType.Star) || Check(MiniLangTokType.Slash) || Check(MiniLangTokType.Percent))
                {
                    MiniLangToken op = Advance();
                    MiniLangExpr right = Unary();
                    expr = new BinaryExpr { op = OpText(op.type), left = expr, right = right, line = op.line };
                }
                return expr;
            }
            finally { ExitRule(); }
        }

        MiniLangExpr Unary()
        {
            EnterRule(Peek().line);
            try
            {
                if (Check(MiniLangTokType.Bang) || Check(MiniLangTokType.Minus))
                {
                    MiniLangToken op = Advance();
                    MiniLangExpr right = Unary();
                    return new UnaryExpr { op = op.type == MiniLangTokType.Bang ? "!" : "-", right = right, line = op.line };
                }
                return Primary();
            }
            finally { ExitRule(); }
        }

        MiniLangExpr Primary()
        {
            EnterRule(Peek().line);
            try
            {
                MiniLangToken t = Peek();
                if (Match(MiniLangTokType.Number)) return new NumberExpr { value = Previous().numberValue, line = t.line };
                if (Match(MiniLangTokType.String)) return new StringExpr { value = Previous().stringValue, line = t.line };
                if (Match(MiniLangTokType.True)) return new BoolExpr { value = true, line = t.line };
                if (Match(MiniLangTokType.False)) return new BoolExpr { value = false, line = t.line };
                if (Match(MiniLangTokType.LParen))
                {
                    MiniLangExpr expr = Expression();
                    Consume(MiniLangTokType.RParen, "expected ')' after expression");
                    return expr;
                }
                if (Match(MiniLangTokType.Identifier))
                {
                    string name = Previous().text;
                    if (Match(MiniLangTokType.LParen))
                    {
                        var args = new List<MiniLangExpr>();
                        if (!Check(MiniLangTokType.RParen))
                        {
                            do { args.Add(Expression()); } while (Match(MiniLangTokType.Comma));
                        }
                        Consume(MiniLangTokType.RParen, "expected ')' after arguments");
                        return new CallExpr { name = name, args = args, line = t.line };
                    }
                    return new VariableExpr { name = name, line = t.line };
                }
                throw new MiniLangError("expected expression", t.line);
            }
            finally { ExitRule(); }
        }

        static string OpText(MiniLangTokType type)
        {
            switch (type)
            {
                case MiniLangTokType.Less: return "<";
                case MiniLangTokType.Greater: return ">";
                case MiniLangTokType.LessEq: return "<=";
                case MiniLangTokType.GreaterEq: return ">=";
                case MiniLangTokType.Star: return "*";
                case MiniLangTokType.Slash: return "/";
                case MiniLangTokType.Percent: return "%";
                default: return "?";
            }
        }
    }

    class MiniLangInterpreter
    {
        const int MaxEvalDepth = 200;
        const long MaxStringCost = 2000000;

        readonly Dictionary<string, object> vars = new Dictionary<string, object>();
        public List<string> stdout = new List<string>();
        readonly int maxSteps;
        int steps;
        int depth;
        long stringCost;

        public MiniLangInterpreter(int maxSteps)
        {
            this.maxSteps = maxSteps;
        }

        public void Run(List<MiniLangStmt> statements)
        {
            foreach (MiniLangStmt s in statements) Execute(s);
        }

        // Guards every recursive branch (nested blocks/if/while bodies) against
        // unbounded recursion that would otherwise overflow the C# call stack
        // with an uncatchable StackOverflowException.
        void Execute(MiniLangStmt s)
        {
            depth++;
            try
            {
                if (depth > MaxEvalDepth) throw new MiniLangError("statement nested too deeply", s.line);

                steps++;
                if (steps > maxSteps) throw new MiniLangError("step limit exceeded (possible infinite loop)", s.line);

                var exprStmt = s as ExprStmt;
                if (exprStmt != null) { Evaluate(exprStmt.expr); return; }

                var printStmt = s as PrintStmt;
                if (printStmt != null) { stdout.Add(Stringify(Evaluate(printStmt.expr))); return; }

                var blockStmt = s as BlockStmt;
                if (blockStmt != null) { foreach (MiniLangStmt inner in blockStmt.statements) Execute(inner); return; }

                var ifStmt = s as IfStmt;
                if (ifStmt != null)
                {
                    if (ToBool(Evaluate(ifStmt.condition), ifStmt.line)) Execute(ifStmt.thenBranch);
                    else if (ifStmt.elseBranch != null) Execute(ifStmt.elseBranch);
                    return;
                }

                var whileStmt = s as WhileStmt;
                if (whileStmt != null)
                {
                    while (ToBool(Evaluate(whileStmt.condition), whileStmt.line))
                    {
                        Execute(whileStmt.body);
                    }
                    return;
                }

                throw new MiniLangError("unknown statement", s.line);
            }
            finally { depth--; }
        }

        object Evaluate(MiniLangExpr e)
        {
            depth++;
            try
            {
                if (depth > MaxEvalDepth) throw new MiniLangError("expression nested too deeply", e.line);

                var numberExpr = e as NumberExpr;
                if (numberExpr != null) return numberExpr.value;

                var stringExpr = e as StringExpr;
                if (stringExpr != null) return stringExpr.value;

                var boolExpr = e as BoolExpr;
                if (boolExpr != null) return boolExpr.value;

                var variableExpr = e as VariableExpr;
                if (variableExpr != null)
                {
                    object val;
                    if (vars.TryGetValue(variableExpr.name, out val)) return val;
                    throw new MiniLangError("undefined variable '" + variableExpr.name + "'", variableExpr.line);
                }

                var assignExpr = e as AssignExpr;
                if (assignExpr != null)
                {
                    object val = Evaluate(assignExpr.value);
                    vars[assignExpr.name] = val;
                    return val;
                }

                var unaryExpr = e as UnaryExpr;
                if (unaryExpr != null)
                {
                    object r = Evaluate(unaryExpr.right);
                    if (unaryExpr.op == "-") return -ToNumber(r, unaryExpr.line);
                    if (unaryExpr.op == "!") return !ToBool(r, unaryExpr.line);
                    throw new MiniLangError("unknown unary operator '" + unaryExpr.op + "'", unaryExpr.line);
                }

                var logicalExpr = e as LogicalExpr;
                if (logicalExpr != null)
                {
                    bool left = ToBool(Evaluate(logicalExpr.left), logicalExpr.line);
                    if (logicalExpr.op == "&&")
                    {
                        if (!left) return false;
                        return ToBool(Evaluate(logicalExpr.right), logicalExpr.line);
                    }
                    else
                    {
                        if (left) return true;
                        return ToBool(Evaluate(logicalExpr.right), logicalExpr.line);
                    }
                }

                var binaryExpr = e as BinaryExpr;
                if (binaryExpr != null) return EvalBinary(binaryExpr);

                var callExpr = e as CallExpr;
                if (callExpr != null) return EvalCall(callExpr);

                throw new MiniLangError("unknown expression", e.line);
            }
            finally { depth--; }
        }

        object EvalBinary(BinaryExpr b)
        {
            object l = Evaluate(b.left);
            object r = Evaluate(b.right);
            switch (b.op)
            {
                case "+":
                    if (l is string || r is string)
                    {
                        string concat = Stringify(l) + Stringify(r);
                        stringCost += concat.Length;
                        if (stringCost > MaxStringCost) throw new MiniLangError("output too large", b.line);
                        return concat;
                    }
                    return ToNumber(l, b.line) + ToNumber(r, b.line);
                case "-": return ToNumber(l, b.line) - ToNumber(r, b.line);
                case "*": return ToNumber(l, b.line) * ToNumber(r, b.line);
                case "/": return ToNumber(l, b.line) / ToNumber(r, b.line);
                case "%": return ToNumber(l, b.line) % ToNumber(r, b.line);
                case "==": return ValuesEqual(l, r);
                case "!=": return !ValuesEqual(l, r);
                case "<": return ToNumber(l, b.line) < ToNumber(r, b.line);
                case ">": return ToNumber(l, b.line) > ToNumber(r, b.line);
                case "<=": return ToNumber(l, b.line) <= ToNumber(r, b.line);
                case ">=": return ToNumber(l, b.line) >= ToNumber(r, b.line);
                default: throw new MiniLangError("unknown operator '" + b.op + "'", b.line);
            }
        }

        object EvalCall(CallExpr c)
        {
            if (c.name == "len")
            {
                if (c.args.Count != 1) throw new MiniLangError("len() takes exactly 1 argument", c.line);
                object a = Evaluate(c.args[0]);
                var s = a as string;
                if (s == null) throw new MiniLangError("len() expects a string argument", c.line);
                return (double)s.Length;
            }
            if (c.name == "charAt")
            {
                if (c.args.Count != 2) throw new MiniLangError("charAt() takes exactly 2 arguments", c.line);
                object a = Evaluate(c.args[0]);
                object idx = Evaluate(c.args[1]);
                var s = a as string;
                if (s == null) throw new MiniLangError("charAt() expects a string as the first argument", c.line);
                double indexNum = ToNumber(idx, c.line);
                int index = (int)indexNum;
                if (index < 0 || index >= s.Length) throw new MiniLangError("charAt() index out of range", c.line);
                return s.Substring(index, 1);
            }
            throw new MiniLangError("unknown function '" + c.name + "'", c.line);
        }

        static bool ValuesEqual(object l, object r)
        {
            if (l is double && r is double) return (double)l == (double)r;
            if (l is string && r is string) return (string)l == (string)r;
            if (l is bool && r is bool) return (bool)l == (bool)r;
            return false;
        }

        static double ToNumber(object v, int line)
        {
            if (v is double) return (double)v;
            throw new MiniLangError("expected a number", line);
        }

        static bool ToBool(object v, int line)
        {
            if (v is bool) return (bool)v;
            throw new MiniLangError("expected a boolean", line);
        }

        static string Stringify(object v)
        {
            if (v is double) return NumberToString((double)v);
            if (v is string) return (string)v;
            if (v is bool) return (bool)v ? "true" : "false";
            return "null";
        }

        static string NumberToString(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "Infinity";
            if (double.IsNegativeInfinity(d)) return "-Infinity";
            if (d == Math.Truncate(d) && Math.Abs(d) < 1e15)
            {
                return ((long)d).ToString(CultureInfo.InvariantCulture);
            }
            return d.ToString("G15", CultureInfo.InvariantCulture);
        }
    }
}
