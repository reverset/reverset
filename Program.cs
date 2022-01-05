using System;
using System.Diagnostics;
using System.IO;

#nullable enable
namespace RParse
{
    class Program
    {
        public static string name = "undecided";
        public static Version version = new Version(0, 0, 0);

        static void Main(string[] args)
        {
            string? fileName = null;
            bool debug = false;

            if (args.Length == 0) 
            {
                ROut.Println("Enabling interactive mode, re-run with 'help' as a parameter for help.\n", ConsoleColor.Yellow);
            }

            int idx = 0;
            foreach (var arg in args) 
            {
                switch (arg) 
                {
                    case "exec":
                        fileName = args[idx + 1];
                        break;
                    case "debug":
                        debug = true;
                        break;
                    case "help":
                        ROut.Println($"{name} v{version.ToString()} --- Help\n" + 
                        "If no 'exec' arguement is given, interactive mode is enabled.\n", ConsoleColor.Blue);
                        ROut.Println($"Commands:");
                        ROut.Println($"  exec  ---------- Executes a file given a filepath as the next arguement.");
                        ROut.Println($"  debug ---------- Enables debug mode.");
                        Environment.Exit(0);
                        break;
                }
                idx += 1;
            }

            if (fileName != null) 
            {
                string text = File.ReadAllText(fileName);
                exec(fileName, debug, text);
            }
            else 
            {
                while (true)
                {
                    Console.Write("> ");
                    string input = Console.ReadLine();

                    exec("«stdin»", debug, input);
                }
            }
        }

        public static void exec(string fileName, bool debug, string input) 
        {
            var programData = new ProgramData(fileName, input);
            ROut.Println("Analysing ...", ConsoleColor.Green);
            Lexer lx = new Lexer(input, programData);
            LxRes res = lx.Tokenify();
            if (res.Failed())
            {
                Debug.Assert(res.err != null);
                ROut.Println(res.err, ConsoleColor.DarkRed);
            }
            else
            {
                Debug.Assert(res.tokens != null);
                ROut.Println("Generating AST ...", ConsoleColor.Blue);

                var programData2 = new ProgramData("«stdin»", input);
                var parser = new Parser(res.tokens, programData2);
                var ast = parser.Parse();

                ROut.Println("_________________________");
                ROut.Println("");
                ROut.Println("_________________________");
                ROut.Println("");


                ROut.Println("Finished Successfully!", ConsoleColor.Green);
                if (debug) 
                {
                    ROut.Println("-> DEBUG: " + res, ConsoleColor.Yellow);
                    ROut.Println("-> DEBUG: " + ast.res, ConsoleColor.Yellow);
                }
                ROut.Println("Running ...\n", ConsoleColor.Blue);
            }
        }
    }
}
