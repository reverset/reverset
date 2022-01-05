using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

#nullable enable
namespace RParse
{   
    // -=-=-=-=-=-=-=-=-=-=-=
    // -=-=-=-=TOKEN=-=-=-=-=
    // -=-=-=-=-=-=-=-=-=-=-=

    public enum TokenType
    {
        NONE,
        PLUS,
        SUB,
        MULT,
        DIV,
        LPAREN,
        RPAREN,
        INT,
        FLOAT,
    }

    public class Token
    {
        public TokenType tt;
        public dynamic? val;

        public Token()
        {
            tt = TokenType.NONE;
            val = null;
        }

        public Token(TokenType type)
        {
            tt = type;
            val = null;
        }

        public Token(TokenType type, dynamic? val)
        {
            tt = type;
            this.val = val;
        }

        public override string ToString()
        {
            if (val != null) return $"{tt}:{val}";
            return $"{tt}";
        }
    }

    // -=-=-=-=-=-=-=-=-=-=-=
    // -=-=-=-=LEXER=-=-=-=-=
    // -=-=-=-=-=-=-=-=-=-=-=

    public class Lexer
    {
        ProgramData data;

        const string digits = "0123456789";

        string text;
        int pos;
        char? currChar;

        public Lexer(string text, ProgramData data)
        {
            this.text = text;
            this.data = data;
            pos = -1;
            currChar = null;
            advance();
        }

        public void advance()
        {
            pos += 1;
            if (pos < text.Length) currChar = text[pos];
            else currChar = null;
        }

        public LxRes Tokenify()
        {
            List<Token> tokens = new List<Token>();
            while (currChar != null)
            {
                if (currChar.Equals(' ') || currChar.Equals('\t')) advance();
                else
                {
                    if (digits.Contains((char)currChar))
                        tokens.Add(Numberify());
                    else
                    {
                        if (currChar.Equals('+'))
                            tokens.Add(new Token(TokenType.PLUS, null));
                        else if (currChar.Equals('-'))
                            tokens.Add(new Token(TokenType.SUB, null));
                        else if (currChar.Equals('*'))
                            tokens.Add(new Token(TokenType.MULT, null));
                        else if (currChar.Equals('/'))
                            tokens.Add(new Token(TokenType.DIV, null));
                        else if (currChar.Equals('('))
                            tokens.Add(new Token(TokenType.LPAREN, null));
                        else if (currChar.Equals(')'))
                            tokens.Add(new Token(TokenType.RPAREN, null));
                    
                        else
                        {
                            char c = (char) currChar;
                            advance();
                            return new LxRes(null, new IllegalCharFailure($"{c}", pos, data.Contents()));
                        }

                        advance();
                    }
                }
            }

            return new LxRes(tokens, null);
        }

        public Token Numberify()
        {
            string numStr = "";
            bool hasDot = false;

            while (currChar != null && (digits + ".").Contains((char)currChar))
            {
                if (currChar.Equals('.'))
                {
                    if (hasDot) break;
                    hasDot = true;
                    numStr += ".";
                }
                else
                {
                    numStr += currChar;
                }
                advance();
            }

            if (!hasDot)
                return new Token(TokenType.INT, int.Parse(numStr));
            return new Token(TokenType.FLOAT, float.Parse(numStr));
        }
    }

    public class LxRes
    {
        public List<Token>? tokens;
        public Failure? err;

        public LxRes(List<Token>? tokens, Failure? err)
        {
            this.tokens = tokens;
            this.err = err;
        }

        public bool Failed()
        {
            if (err == null) return false;
            return true;
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            if (Failed()) res.Append("FAILED");
            else
            {
                Debug.Assert(tokens != null);
                int idx = 0;
                res.Append("[");

                foreach (var tok in tokens)
                {
                    if (idx == (tokens.Count - 1))
                    {
                        res.Append(tok);
                    } else
                    {
                        res.Append(tok + ", ");
                    }
                    idx += 1;
                }
                res.Append("]");
            }
            return res.ToString();
        }
    }

    // -=-=-=-=-=-=-=-=-=-=-=
    // -=-=-=-=PARSE=-=-=-=-=
    // -=-=-=-=-=-=-=-=-=-=-=

    public class Parser
    {
        List<Token> tokens;
        Token currentTok;
        ProgramData data;
        int tokIndex;

        public Parser(List<Token> tokens, ProgramData data)
        {
            this.tokens = tokens;
            this.data = data;
            tokIndex = -1;
            currentTok = new Token(TokenType.NONE);
            Advance();
        }

        Token Advance()
        {
            tokIndex += 1;
            if (tokIndex < tokens.Count)
            {
                currentTok = tokens[tokIndex];
            }
            return currentTok;
        }

        public ParserRes Parse()
        {
            return new ParserRes(Expr());
        }

        Node Factor()
        {
            Token tok = currentTok;
            Failure? err = null;

            if (tok.tt.Equals(TokenType.INT) || tok.tt.Equals(TokenType.FLOAT))
            {
                Advance();
                return new NumberNode(tok);
            } 
            else if (tok.tt.Equals(TokenType.LPAREN)) 
            {
                Advance();
                Node expr = Expr();
                if (!currentTok.tt.Equals(TokenType.RPAREN)) 
                {
                    err = new InvalidSyntaxFailure("Expected closing parenthesis", data.fLine + 1, data.Contents());
                }
                else 
                {
                    return expr;
                }

            }
            else 
            {
                err = new InvalidSyntaxFailure($"{currentTok}", data.fLine + 1, data.Contents());
            }

            if (err != null) err.Yeet();

            #pragma warning disable CS8603
            return null; // This is here so we don't get an error, even though in reality the system exits before it gets to this line.
            #pragma warning restore CS8603
        }

        Node Term()
        {
            Node left = Factor();
            BinOpNode? binOp;


            while (currentTok.tt.Equals(TokenType.MULT) || currentTok.tt.Equals(TokenType.DIV))
            {
                Token op = currentTok;
                Advance();
                Node? right = Factor();

                binOp = new BinOpNode(left, op, right);
                left = binOp;
            }

            return left;
        }
        Node Expr()
        {
            Node? left = Term();
            BinOpNode? binOp;

            while (currentTok.tt.Equals(TokenType.PLUS) || currentTok.tt.Equals(TokenType.SUB))
            {

                Token op = currentTok;
                Advance();
                Node? right = Term();

                binOp = new BinOpNode(left, op, right);
                left = binOp;
            }

            return left;
        }
    }

    public class ParserRes
    {
        public Node? res;

        public ParserRes(Node? res)
        {
            this.res = res;
        }
    }

    // -=-=-=-=-=-=-=-=-=-=-=
    // -=-=-=-=NODES=-=-=-=-=
    // -=-=-=-=-=-=-=-=-=-=-=

    public abstract class Node
    {
        //public abstract Node Res();
    }

    public class NumberNode : Node
    {
        public Token tok;

        public NumberNode(Token tok)
        {
            this.tok = tok;
        }

        public override string ToString()
        {
            return $"NN[{tok}]";
        }
    }

    public class BinOpNode : Node
    {
        public Node left;
        public Node right;
        public Token op;

        public BinOpNode(Node left, Token op, Node right)
        {
            this.left = left;
            this.right = right;
            this.op = op;
        }

        public override string ToString()
        {
            return $"BINOP[{left}, {op}, {right}]";
        }
    }

    // -=-=-=-=-=-=-=-=-=-=-=
    // -=-=-=-=ERROR=-=-=-=-=
    // -=-=-=-=-=-=-=-=-=-=-=

    public class Failure
    {
        public string? name;
        public string? details;
        public string contents;
        public int line;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Failure() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public Failure(string name, string details, int line, string contents)
        {
            this.name = name;
            this.details = details;
            this.line = line;
            this.contents = contents;
        }

        public void Yeet()
        {
            ROut.Println("\nFATAL ERROR - PARSING HALTED", null, ConsoleColor.DarkRed);
            ROut.Println(ToString(), ConsoleColor.DarkRed);
            Environment.Exit(1);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"FAILURE: {name}\n");
            stringBuilder.Append($"-> {details}\n");
            stringBuilder.Append($"-> Line #{line}\n");
            stringBuilder.Append($"-> Content '{contents}'");

            return stringBuilder.ToString();
        }
    }
    
    public class IllegalCharFailure : Failure
    {

        public IllegalCharFailure(string details, int pos, string contents)
        {
            name = "IllegalCharFailure";
            line = pos;
            this.details = $"Unsupported character '{details}'";
            this.contents = contents;
        }
    }

    public class InvalidSyntaxFailure : Failure
    {

        public InvalidSyntaxFailure(string details, int pos, string contents)
        {
            name = "InvalidSyntaxFailure";
            line = pos;
            this.details = $"Cannot understand line #{pos} Perhaps a typo? {details}";
            this.contents = contents;
        }
    }

    // -=-=-=-=-=-=-=-=-=-=-=
    // -=-=-=-=-MISC=-=-=-=-=
    // -=-=-=-=-=-=-=-=-=-=-=

    public class ProgramData
    {
        public string fileName;

        public string[] fLineContents;
        public int fLine;

        public ProgramData(string fileName, string contents)
        {
            this.fileName = fileName;
            fLine = -1;
            fLineContents = contents.Split("\n");

            NextLine();
        }

        public string NextLine()
        {
            string res;
            if (fileName.Equals("«stdin»"))
            {
                fLine = 0;
            } else
            {
                fLine += 1;
            }
            res = fLineContents[fLine];
            return res;
        }

        public string Contents()
        {
            return fLineContents[fLine];
        }
    }

    public class ROut
    {
        public static void Println(object x, ConsoleColor? front = null, ConsoleColor? back = null)
        {
            Console.ResetColor();
            if (back == null && front == null)
            {
                Console.WriteLine(x);
            } else
            {
                if (back != null) Console.BackgroundColor = (ConsoleColor) back;
                if (front != null) Console.ForegroundColor = (ConsoleColor) front;
                Console.WriteLine(x);
            }
            Console.ResetColor();
        }
    }
}
