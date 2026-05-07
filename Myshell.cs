using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyShell
{
    class SistemPogramlama
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║   Basic Shell                        ║");
            Console.WriteLine("║   Çıkmak için: exit                  ║");
            Console.WriteLine("║   Desteklenen komutlar için : help   ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            var shell = new Shell();
            shell.Run();
        }
    }

    class Shell
    {
        private string _currentDirectory;

        public Shell()
        {
            _currentDirectory = Directory.GetCurrentDirectory();
        }

        public void Run()
        {
            while (true)
            {
                PrintPrompt();

                string? line = Console.ReadLine();

                if (line == null)
                {
                    Console.WriteLine();
                    break;
                }

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    ExecuteLine(line);
                }
                catch (Exception ex)
                {
                    PrintError($"myshell: {ex.Message}");
                }
            }
        }

        private void PrintPrompt()
        {
            string user = Environment.UserName;
            string host = Environment.MachineName;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string shortDir = _currentDirectory.StartsWith(home)
                ? "~" + _currentDirectory[home.Length..]
                : _currentDirectory;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{user}@{host}");
            Console.ResetColor();
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(shortDir);
            Console.ResetColor();
            Console.Write("$ ");
        }

        private void ExecuteLine(string line)
        {
            int commentIdx = line.IndexOf('#');
            if (commentIdx >= 0)
                line = line[..commentIdx].Trim();

            if (string.IsNullOrEmpty(line)) return;

            List<string> segments = SplitByPipe(line);

            if (segments.Count == 1)
            {
                ExecuteSingleCommand(segments[0].Trim());
            }
            else
            {
                ExecutePipeline(segments);
            }
        }

        private List<string> SplitByPipe(string line)
        {
            var segments = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuote = false;
            char quoteChar = '\0';

            foreach (char ch in line)
            {
                if (!inQuote && (ch == '\'' || ch == '"'))
                {
                    inQuote = true;
                    quoteChar = ch;
                    current.Append(ch);
                }
                else if (inQuote && ch == quoteChar)
                {
                    inQuote = false;
                    current.Append(ch);
                }
                else if (!inQuote && ch == '|')
                {
                    segments.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            segments.Add(current.ToString());
            return segments;
        }

        private void ExecuteSingleCommand(string commandStr)
        {
            var (cmd, redirectIn, redirectOut, appendMode) = ParseRedirects(commandStr);

            string[] parts = TokenizeCommand(cmd);
            if (parts.Length == 0) return;

            string commandName = parts[0];
            string[] arguments = parts[1..];

            if (IsBuiltin(commandName))
            {
                ExecuteBuiltin(commandName, arguments);
                return;
            }

            ExecuteExternal(commandName, arguments,
                stdIn: redirectIn,
                stdOut: redirectOut,
                appendOut: appendMode,
                inputStream: null,
                captureOutput: false);
        }

        private bool IsBuiltin(string cmd)
            => cmd is "cd" or "pwd" or "exit" or "help";

        private void ExecuteBuiltin(string cmd, string[] args)
        {
            switch (cmd)
            {
                case "cd":
                    BuiltinCd(args);
                    break;

                case "pwd":
                    Console.WriteLine(_currentDirectory);
                    break;

                case "exit":
                    int code = args.Length > 0 && int.TryParse(args[0], out int c) ? c : 0;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Kabuktan çıkılıyor... Allaha Emanet!");
                    Console.ResetColor();
                    Environment.Exit(code);
                    break;

                case "help":
                    PrintHelp();
                    break;
            }
        }

        private void BuiltinCd(string[] args)
        {
            string targetDir;

            if (args.Length == 0 || args[0] == "~")
            {
                targetDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else if (args[0] == "-")
            {
                PrintError("cd: '-' desteği bu sürümde eklenmemiştir.");
                return;
            }
            else
            {
                targetDir = Path.IsPathRooted(args[0])
                    ? args[0]
                    : Path.Combine(_currentDirectory, args[0]);
            }

            targetDir = Path.GetFullPath(targetDir);

            if (!Directory.Exists(targetDir))
            {
                PrintError($"cd: {args[0]}: Böyle bir dizin yok");
                return;
            }

            _currentDirectory = targetDir;
            Directory.SetCurrentDirectory(_currentDirectory);
        }

        private StreamReader? ExecuteExternal(
            string command,
            string[] args,
            string? stdIn = null,
            string? stdOut = null,
            bool appendOut = false,
            StreamReader? inputStream = null,
            bool captureOutput = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = false,
                WorkingDirectory = _currentDirectory,
                RedirectStandardInput = (stdIn != null || inputStream != null),
                RedirectStandardOutput = (stdOut != null || captureOutput),
                RedirectStandardError = false,
            };

            foreach (string arg in args)
                psi.ArgumentList.Add(arg);

            Process? process;
            try
            {
                process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                PrintError($"myshell: {command}: {ex.Message}");
                return null;
            }

            if (process == null)
            {
                PrintError($"myshell: {command}: süreç başlatılamadı.");
                return null;
            }

            if (stdIn != null)
            {
                using var fs = new FileStream(stdIn, FileMode.Open, FileAccess.Read);
                fs.CopyTo(process.StandardInput.BaseStream);
                process.StandardInput.Close();
            }
            else if (inputStream != null)
            {
                inputStream.BaseStream.CopyTo(process.StandardInput.BaseStream);
                process.StandardInput.Close();
            }

            if (stdOut != null)
            {
                var fileMode = appendOut ? FileMode.Append : FileMode.Create;
                using var outFs = new FileStream(stdOut, fileMode, FileAccess.Write);
                process.StandardOutput.BaseStream.CopyTo(outFs);
            }

            if (captureOutput)
            {
                return process.StandardOutput;
            }

            process.WaitForExit();

            process.Dispose();
            return null;
        }

        private void ExecutePipeline(List<string> segments)
        {
            StreamReader? previousOutput = null;

            for (int i = 0; i < segments.Count; i++)
            {
                string seg = segments[i].Trim();
                bool isLast = (i == segments.Count - 1);

                var (cmd, redirectIn, redirectOut, appendMode) = ParseRedirects(seg);

                string[] parts = TokenizeCommand(cmd);
                if (parts.Length == 0) continue;

                string commandName = parts[0];
                string[] arguments = parts[1..];

                if (!isLast)
                {
                    previousOutput = ExecuteExternal(
                        commandName, arguments,
                        stdIn: redirectIn,
                        stdOut: null,
                        appendOut: false,
                        inputStream: previousOutput,
                        captureOutput: true);
                }
                else
                {
                    if (IsBuiltin(commandName))
                    {
                        if (previousOutput != null)
                            Console.Write(previousOutput.ReadToEnd());
                        ExecuteBuiltin(commandName, arguments);
                    }
                    else
                    {
                        ExecuteExternal(
                            commandName, arguments,
                            stdIn: redirectIn,
                            stdOut: redirectOut,
                            appendOut: appendMode,
                            inputStream: previousOutput,
                            captureOutput: false);
                    }

                    previousOutput?.Dispose();
                }
            }
        }

        private (string cmd, string? redirectIn, string? redirectOut, bool appendMode)
            ParseRedirects(string input)
        {
            string? redirectIn = null;
            string? redirectOut = null;
            bool appendMode = false;

            var tokens = TokenizeRaw(input);
            var cmdTokens = new List<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                string tok = tokens[i];

                if (tok == ">>" && i + 1 < tokens.Count)
                {
                    redirectOut = tokens[++i];
                    appendMode = true;
                }
                else if (tok == ">" && i + 1 < tokens.Count)
                {
                    redirectOut = tokens[++i];
                    appendMode = false;
                }
                else if (tok == "<" && i + 1 < tokens.Count)
                {
                    redirectIn = tokens[++i];
                }
                else
                {
                    cmdTokens.Add(tok);
                }
            }

            return (string.Join(" ", cmdTokens), redirectIn, redirectOut, appendMode);
        }

        private List<string> TokenizeRaw(string input)
        {
            var tokens = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];

                if (inSingleQuote)
                {
                    if (ch == '\'') inSingleQuote = false;
                    else current.Append(ch);
                }
                else if (inDoubleQuote)
                {
                    if (ch == '"') inDoubleQuote = false;
                    else current.Append(ch);
                }
                else if (ch == '\'')
                {
                    inSingleQuote = true;
                }
                else if (ch == '"')
                {
                    inDoubleQuote = true;
                }
                else if (ch == ' ' || ch == '\t')
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
                else if ((ch == '>' && i + 1 < input.Length && input[i + 1] == '>'))
                {
                    if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
                    tokens.Add(">>");
                    i++;
                }
                else if (ch == '>' || ch == '<')
                {
                    if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
                    tokens.Add(ch.ToString());
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (current.Length > 0)
                tokens.Add(current.ToString());

            return tokens;
        }

        private string[] TokenizeCommand(string input)
        {
            var tokens = TokenizeRaw(input);
            var result = new List<string>();
            foreach (var tok in tokens)
            {
                if (tok is ">" or ">>" or "<") break;
                result.Add(tok);
            }
            return result.ToArray();
        }

        private void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }

        private void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n── Basic Shell Komutları ──────────────────────────────");
            Console.ResetColor();
            Console.WriteLine("  Dahili Komutlar:");
            Console.WriteLine("    cd [dizin]    - Dizin değiştir");
            Console.WriteLine("    pwd           - Mevcut dizini göster");
            Console.WriteLine("    exit [kod]    - Kabuktan çık");
            Console.WriteLine("    help          - Bu yardım mesajını göster");
            Console.WriteLine();
            Console.WriteLine("  I/O Yönlendirme:");
            Console.WriteLine("    komut > dosya    - Çıkışı dosyaya yaz (üzerine yaz)");
            Console.WriteLine("    komut >> dosya   - Çıkışı dosyaya ekle");
            Console.WriteLine("    komut < dosya    - Dosyadan giriş oku");
            Console.WriteLine();
            Console.WriteLine("  Pipe:");
            Console.WriteLine("    komut1 | komut2  - komut1 çıkışını komut2'ye bağla");
            Console.WriteLine();
            Console.WriteLine("  Harici Komutlar:");
            Console.WriteLine("    ls, cat, grep, echo, date, whoami, df, du, vb.");
            Console.WriteLine("────────────────────────────────────────────────────\n");
        }
    }
}