
using System;
using System.Collections.Generic;
using System.Linq;


namespace Retrospection.CommandLine
{
    /// <summary>
    /// Specifies constants that define the way in which the AutoComplete feature works.
    /// </summary>
    public enum AutoCompleteMode : byte
    {
        /// <summary>
        /// Commands which start with the current input will be presented as AutoComplete options.
        /// </summary>
        StartsWith,

        /// <summary>
        /// Commands which contain the current input will be presented as AutoComplete options.
        /// </summary>
        Contains,

        /// <summary>
        /// The OnAutoComplete callback will be called to obtain a list of AutoComplete options.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Provides services for reading input from the console.
    /// </summary>
    public sealed class IntelliPrompt
    {
        private ConsoleColor _backColor;
        private ConsoleColor _foreColor;
        private List<char> _input = new();
        private int _insertPos = 0;
        private bool _overType;
        private int _historyCursor = 0;
        private string _autoCompleteSearch;
        private int _autoCompleteCursor;
        private List<string> _history;
        private List<string> _commands;
        private List<char> _separatorChars;
        private Menu _menu;
        private string _prompt;

        /// <summary>
        /// Gets a list of strings which have been entered.
        /// </summary>
        public IEnumerable<string> History { get => _history; }

        /// <summary>
        /// Gets a list of possible commands to be used for AutoComplete.
        /// </summary>
        public IEnumerable<string> Commands { get => _commands; }

        /// <summary>
        /// Gets the list of SeparatorChars to be used as word-boundaries for cursor navigation.  If unspecified, Char.IsSeparator is used to detect word boundaries instead.
        /// </summary>
        public IEnumerable<char> SeparatorChars { get => _separatorChars; }

        /// <summary>
        /// Gets or sets the AutoCompleteMode
        /// </summary>
        public AutoCompleteMode AutoCompleteMode { get; set; }

        /// <summary>
        /// A callback which will be called when the prompt requires a list of AutoComplete options.  Only used if the AutoCompleteMode property is set to Custom.
        /// </summary>
        public Func<string, IEnumerable<string>> OnAutoComplete { get; set; }

        /// <summary>
        /// Initializes a new instance of the IntelliPrompt class
        /// </summary>
        public IntelliPrompt()
        {
            _backColor = Console.BackgroundColor;
            _foreColor = Console.ForegroundColor;
            _history = new List<string>();
            _commands = new List<string>();
            _separatorChars = new List<char>();

            _menu = new Menu(_commands, null, 10)
            {
                RegularBackColor = ConsoleColor.DarkBlue,
                SelectedBackColor = ConsoleColor.Red,
                RegularTextColor = ConsoleColor.Gray,
                SelectedTextColor = ConsoleColor.White
            };
            _menu.OnSelectedItemChanged = () => SetScreenInput(_menu.SelectedItem);

            ResetAutoCompleteState();
        }

        /// <summary>
        /// Initializes a new instance of the IntelliPrompt class
        /// </summary>
        /// <param name="commandHistory">A collection of strings to be used as the command history which can be accessed by the Up-Arrow key.</param>
        /// <param name="commands">A collection of strings to be used as the available auto-complete commands which can be accessed by the Tab key.</param>
        /// <param name="separatorChars">A collection of strings to be used as word-boundary separators for cursor-navigation events using Ctrl+Left-Arrow or Ctrl+Right-Arrow.
        ///   If this is not specified, Char.IsSeparator is used to determine word-boundaries.</param>
        public IntelliPrompt(
            IEnumerable<string> commandHistory,
            IEnumerable<string> commands,
            IEnumerable<char> separatorChars) : this()
        {
            _history.AddRange(commandHistory ?? Array.Empty<string>());
            _historyCursor = _history.Count;
            _separatorChars.AddRange(separatorChars ?? Array.Empty<char>());
            _commands.AddRange(commands ?? Array.Empty<string>());
            _commands.Sort();
        }

        /// <summary>
        /// Reads input at the command line as a series of characters terminated by a CrLf sequence.
        /// </summary>
        /// <returns>A string representing the input the user typed.</returns>
        public string ReadLine() => ReadLine("");

        /// <summary>
        /// Writes a prompt to the command line and then reads input as a series of characters terminated by a CrLf sequence.
        /// </summary>
        /// <param name="prompt">The prompt to write to the command line before reading input characters.</param>
        /// <returns>A string representing the input the user typed.</returns>
        public string ReadLine(string prompt)
        {
            _prompt = prompt;
            Console.TreatControlCAsInput = true;
            Console.Write(prompt);

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    return AcceptInput();
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    if (_menu.IsOpen)
                    {
                        _menu.HandleInteractionKey(key.Modifiers == ConsoleModifiers.Shift ? Menu.InteractionKey.ShiftTab : Menu.InteractionKey.Tab);
                    }
                    else
                    {
                        ScrollAutoComplete(!(key.Modifiers == ConsoleModifiers.Shift));
                    }
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    if (_menu.IsOpen)
                    {
                        _menu.IsOpen = false;
                    }
                    else
                    {
                        ClearScreenInput();
                    }
                    _historyCursor = _history.Count;
                    ResetAutoCompleteState();
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    Backspace(key.Modifiers.HasFlag(ConsoleModifiers.Control));
                }
                else if (key.Key == ConsoleKey.Delete)
                {
                    if (_insertPos < _input.Count && _input.Any())
                    {
                        AdvanceCursor(1, key.Modifiers.HasFlag(ConsoleModifiers.Control));
                        Backspace(key.Modifiers.HasFlag(ConsoleModifiers.Control));
                    }
                }
                else if (key.Key == ConsoleKey.Insert)
                {
                    _overType = !_overType;
                }
                else if ((key.Key == ConsoleKey.UpArrow) || (key.Key == ConsoleKey.DownArrow))
                {
                    if (_menu.IsOpen)
                    {
                        _menu.HandleInteractionKey(key.Key == ConsoleKey.UpArrow ? Menu.InteractionKey.Up : Menu.InteractionKey.Down);
                    }
                    else
                    {
                        ScrollHistory(key.Key == ConsoleKey.DownArrow);
                    }
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (_menu.IsOpen)
                    {
                        _menu.HandleInteractionKey(Menu.InteractionKey.Left);
                    }
                    else
                    {
                        BackupCursor(1, key.Modifiers.HasFlag(ConsoleModifiers.Control));
                    }
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    if (_menu.IsOpen)
                    {
                        _menu.HandleInteractionKey(Menu.InteractionKey.Right);
                    }
                    else
                    {
                        AdvanceCursor(1, key.Modifiers.HasFlag(ConsoleModifiers.Control));
                    }
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    if (_menu.IsOpen)
                    {
                        _menu.HandleInteractionKey(key.Modifiers == ConsoleModifiers.Control ? Menu.InteractionKey.CtrlHome : Menu.InteractionKey.Home);
                    }
                    else
                    {
                        GoToStartOfInput();
                    }
                }
                else if (key.Key == ConsoleKey.End)
                {
                    if (_menu.IsOpen)
                    {
                        _menu.HandleInteractionKey(key.Modifiers == ConsoleModifiers.Control ? Menu.InteractionKey.CtrlEnd : Menu.InteractionKey.End);
                    }
                    else
                    {
                        GoToEndOfInput();
                    }
                }
                else if (!(
                    char.IsLetterOrDigit(key.KeyChar) ||
                    char.IsWhiteSpace(key.KeyChar) ||
                    char.IsSeparator(key.KeyChar) ||
                    char.IsSymbol(key.KeyChar) ||
                    char.IsPunctuation(key.KeyChar)))
                {
                    HandleSpecialInput(key);
                }
                else
                {
                    AddToInput(key);
                }
            }
        }

        private void HandleSpecialInput(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Applications)
            {
                _menu.RedrawMenu(GetAutoCompletePossibilities(), null, 10);
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{key.Modifiers} {key.Key}");
            Console.ForegroundColor = _foreColor;
        }
        private void AddToInput(ConsoleKeyInfo key)
        {
            var startPos = ConsoleEx.GetCursorPos();

            if (_insertPos == _input.Count)
            {
                _input.Add(key.KeyChar);
                Console.Write(key.KeyChar);
            }
            else
            {
                if (_overType)
                {
                    _input[_insertPos] = key.KeyChar;
                }
                else
                {
                    _input.Insert(_insertPos, key.KeyChar);
                }

                Console.Write(key.KeyChar);
                var curPos = ConsoleEx.GetCursorPos();
                Console.Write(new string(_input.Skip(_insertPos + 1).ToArray()));
                Console.SetCursorPosition(curPos.Left, curPos.Top);
            }
            _insertPos++;
            ResetAutoCompleteState();

            if ((startPos.Left == Console.WindowWidth - 1) && (Console.CursorLeft == Console.WindowWidth - 1))
            {
                Console.SetCursorPosition(0, Console.CursorTop + 1);
            }
        }
        private string AcceptInput()
        {
            _menu.IsOpen = false;
            var result = new string(_input.ToArray());
            AddToHistory(result);
            _input.Clear();
            _insertPos = 0;
            ResetAutoCompleteState();
            return result;
        }
        private void Backspace(bool word)
        {
            if (!_input.Any()) return;

            var curPos = _insertPos;
            BackupCursor(1, word);
            Console.ForegroundColor = _backColor;

            _input.RemoveRange(_insertPos, curPos - _insertPos);
            var prePos = ConsoleEx.GetCursorPos();

            if (_insertPos != _input.Count)
            {
                Console.ForegroundColor = _foreColor;
                Console.Write(new string(_input.Skip(_insertPos).ToArray()));
                Console.ForegroundColor = _backColor;
            }

            Console.Write(new string(' ', curPos - _insertPos));
            Console.ForegroundColor = _foreColor;
            Console.SetCursorPosition(prePos.Left, prePos.Top);
            ResetAutoCompleteState();
        }
        private void BackupCursor(int amount, bool word)
        {
            if (_insertPos == 0) return;

            if (word)
            {
                var lastSep = _input
                    .Select((c, ndx) => (c, ndx))
                    .Where(pair => pair.ndx < (_insertPos - 1) &&
                        ((!_separatorChars.Any() && char.IsSeparator(pair.c)) || _separatorChars.Contains(pair.c)));

                amount = lastSep.Any() ? (_insertPos - lastSep.Last().ndx) - 1 : _insertPos;
            }

            if (amount > Console.WindowWidth)
            {
                var moveUp = Math.DivRem(amount, Console.WindowWidth, out var modulus);
                Console.SetCursorPosition(Console.CursorLeft - modulus, Console.CursorTop - moveUp);
            }
            else if (amount > Console.CursorLeft)
            {
                Console.SetCursorPosition(Console.WindowWidth - (amount - Console.CursorLeft), Console.CursorTop - 1);
            }
            else
            {
                Console.CursorLeft -= amount;
            }
            _insertPos -= amount;
        }
        private void AdvanceCursor(int amount, bool words)
        {
            if (_insertPos == _input.Count) return;

            if (words)
            {
                var nextSep = _input
                    .Select((c, ndx) => (c, ndx))
                    .Where(pair => pair.ndx > _insertPos &&
                        ((!_separatorChars.Any() && char.IsSeparator(pair.c)) || _separatorChars.Contains(pair.c)));

                amount = nextSep.Any() ? (nextSep.First().ndx - _insertPos) + 1 : _input.Count - _insertPos;
            }

            if (amount > Console.WindowWidth)
            {
                var moveDown = Math.DivRem(amount, Console.WindowWidth, out var modulus);
                if (moveDown > 0) modulus += _prompt.Length;
                Console.SetCursorPosition(modulus, Console.CursorTop + moveDown);
            }
            else if (amount >= Console.WindowWidth - Console.CursorLeft)
            {
                Console.SetCursorPosition(amount - (Console.WindowWidth - Console.CursorLeft), Console.CursorTop + 1);
            }
            else
            {
                Console.CursorLeft += amount;
            }
            _insertPos += amount;
        }
        private void GoToStartOfInput()
        {
            BackupCursor(_insertPos, false);
        }
        private void GoToEndOfInput()
        {
            var adv = _input.Count - _insertPos;
            AdvanceCursor(adv, false);
        }
        private void AddToHistory(string result)
        {
            if (result != "" && !_history.Contains(result))
            {
                _history.Add(result);
            }
            else if (_history.Contains(result) && _history.IndexOf(result) != _history.Count - 1)
            {
                _history.Remove(result);
                _history.Add(result);
            }
            _historyCursor = _history.Count;
        }
        private void SetScreenInput(string text)
        {
            ClearScreenInput();
            _input.Clear();
            _input.AddRange(text);
            _insertPos = _input.Count;
            Console.Write(text);
        }
        private void ClearScreenInput()
        {
            (int Left, int Top) backToPos;
            var amount = _input.Count;

            if (amount > Console.WindowWidth)
            {
                var moveUp = Math.DivRem(amount, Console.WindowWidth, out var modulus);
                backToPos = (Console.CursorLeft - modulus, Console.CursorTop - moveUp);
            }
            else if (amount > Console.CursorLeft)
            {
                backToPos = (Console.WindowWidth - (amount - Console.CursorLeft), Console.CursorTop - 1);
            }
            else
            {
                backToPos = (Console.CursorLeft - amount, Console.CursorTop);
            }
            ClearScreenInput(backToPos);
        }
        private void ClearScreenInput((int Left, int Top) backToPos)
        {
            Console.ForegroundColor = _backColor;
            var fullLine = new string(' ', Console.WindowWidth);

            GoToEndOfInput();

            if (backToPos.Top < Console.CursorTop)
            {
                for (int i = Console.CursorTop; i > backToPos.Top; i--)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(fullLine);
                }
            }

            var eraser = fullLine.Substring(0, Console.WindowWidth - backToPos.Left);
            Console.SetCursorPosition(backToPos.Left, backToPos.Top);
            Console.Write(eraser);

            Console.ForegroundColor = _foreColor;
            Console.SetCursorPosition(backToPos.Left, backToPos.Top);

            _input.Clear();
            _insertPos = 0;
        }
        private void ScrollHistory(bool forward)
        {
            var backward = !forward;

            if ((backward && _historyCursor == 0) ||
                (forward && _historyCursor >= _history.Count - 1))
            {
                return;
            }

            var mod = backward ? -1 : 1;
            _historyCursor += mod;
            var text = _history[_historyCursor];

            SetScreenInput(text);
            ResetAutoCompleteState();
        }
        private void ScrollAutoComplete(bool forward)
        {
            _autoCompleteCursor += forward ? 1 : -1;
            var possibilities = GetAutoCompletePossibilities();

            if (_autoCompleteCursor >= possibilities.Count())
            {
                _autoCompleteCursor = 0;
            }
            else if (_autoCompleteCursor == -1)
            {
                _autoCompleteCursor = possibilities.Count() - 1;
            }

            if (possibilities.Any())
            {
                SetScreenInput(possibilities.ToArray()[_autoCompleteCursor]);
            }
        }
        private IEnumerable<string> GetAutoCompletePossibilities()
        {
            if (_autoCompleteSearch == null) _autoCompleteSearch = new string(_input.ToArray());

            IEnumerable<string> possibilities = AutoCompleteMode switch
            {
                AutoCompleteMode.StartsWith => _commands.Where(c => c.StartsWith(_autoCompleteSearch)),
                AutoCompleteMode.Contains => _commands.Where(c => c.Contains(_autoCompleteSearch)),
                AutoCompleteMode.Custom => OnAutoComplete?.Invoke(_autoCompleteSearch) ?? Array.Empty<string>(),
                _ => Array.Empty<string>()
            };

            return possibilities;
        }
        private void ResetAutoCompleteState()
        {
            _autoCompleteCursor = -1;
            _autoCompleteSearch = null;

            if (_menu.IsOpen)
            {
                var possibilities = GetAutoCompletePossibilities();
                _menu.RedrawMenu(possibilities, null, 10);
            }
        }

        /// <summary>
        /// Parses a string and returns a collection of the arguments in a way that is similar to the standard C run-time argv.
        /// </summary>
        /// <param name="args">A string that contains the full command line.</param>
        /// <returns>A collection representing the arguments.</string></returns>
        public static IEnumerable<string> Argify(string args)
        {
            var parts = args.Split(' ');
            var stack = new Stack<string>(args.Length);
            bool openQuotes = false;

            foreach (var part in parts)
            {
                var arg = part;

                if (!openQuotes && part.ContainsUnescaped('"'))
                {
                    openQuotes = true;
                }
                else if (openQuotes)
                {
                    arg = stack.Pop() + ' ' + arg;
                    openQuotes = !part.EndsWithUnescaped('"');
                }

                stack.Push(arg);
            }

            var ret = stack.ToArray().Reverse();

            return ret;
        }

    }
}