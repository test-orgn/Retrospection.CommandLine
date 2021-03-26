
using System;
using System.Collections.Generic;
using System.Linq;


namespace Retrospection.CommandLine
{
    internal class Menu
    {
        internal enum InteractionKey : byte
        {
            Left,
            Right,
            Up,
            Down,
            Home,
            End,
            CtrlHome,
            CtrlEnd,
            Tab,
            ShiftTab
        }

        private bool _isOpen;
        private int _widthPerItem;
        private int _menuRowStart;
        private int _menuRowEnd;
        private List<string> _items;
        private string _selectedItem;
        private int _selectedIndex;

        // TODO: LPadding, RPadding
        internal int TopPadding { get; init; } = 1;
        internal int BottomPadding { get; init; } = 0;
        internal int RowHeight { get => TopPadding + 1 + BottomPadding; }
        internal int ItemsPerRow { get; private set; }
        internal string SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                _selectedIndex = _items.IndexOf(_selectedItem);
            }
        }
        internal int SelectedIndex
        {
            get => _selectedIndex;
            set => _selectedIndex = value;
        }
        internal bool IsOpen
        {
            get
            {
                return _isOpen;
            }
            set
            {
                if (value)
                {
                    DrawMenu();
                }
                else
                {
                    ClearMenu();
                }
            }
        }
        internal Action OnSelectedItemChanged { get; set; }
        internal ConsoleColor RegularTextColor { get; set; } = ConsoleColor.Green;
        internal ConsoleColor SelectedTextColor { get; set; } = ConsoleColor.White;
        internal ConsoleColor RegularBackColor { get; set; } = ConsoleColor.DarkGray;
        internal ConsoleColor SelectedBackColor { get; set; } = ConsoleColor.Blue;

        internal Menu(IEnumerable<string> items, string selectedItem, int itemsPerRow)
        {
            _items = new();
            _items.AddRange(items);
            SelectedItem = selectedItem;
            ItemsPerRow = itemsPerRow;
        }

        internal void RedrawMenu(IEnumerable<string> items, string selectedItem, int itemsPerRow)
        {
            if (IsOpen) IsOpen = false;

            _items = new();
            _items.AddRange(items);
            SelectedItem = selectedItem;
            ItemsPerRow = itemsPerRow;

            IsOpen = true;
        }
        internal void HandleInteractionKey(InteractionKey key)
        {
            var newSelectedIndex = -1;
            var rowBounds = GetRowIndexBounds(_selectedIndex);
            var rowCount = Math.Ceiling(_items.Count / (double)ItemsPerRow); //, MidpointRounding.ToPositiveInfinity);

            if (_selectedIndex == -1)
            {
                SelectMenuItem(0, true);
                return;
            }

            if ((key == InteractionKey.Left) && (_selectedIndex > rowBounds.First))
            {
                newSelectedIndex = _selectedIndex - 1;
            }
            else if ((key == InteractionKey.Right) && (_selectedIndex < rowBounds.Last))
            {
                newSelectedIndex = _selectedIndex + 1;
            }
            else if ((key == InteractionKey.Up) && (rowBounds.RowNum > 0))
            {
                newSelectedIndex = _selectedIndex - ItemsPerRow;
            }
            else if ((key == InteractionKey.Down) && (rowBounds.RowNum < rowCount - 1) && (_selectedIndex + ItemsPerRow < _items.Count))
            {
                newSelectedIndex = _selectedIndex + ItemsPerRow;
            }
            else if (key == InteractionKey.Home)
            {
                newSelectedIndex = rowBounds.First;
            }
            else if (key == InteractionKey.End)
            {
                newSelectedIndex = rowBounds.Last;
            }
            else if (key == InteractionKey.CtrlHome)
            {
                newSelectedIndex = 0;
            }
            else if (key == InteractionKey.CtrlEnd)
            {
                newSelectedIndex = _items.Count - 1;
            }
            else if ((key == InteractionKey.Tab) && (_selectedIndex < _items.Count - 1))
            {
                newSelectedIndex = _selectedIndex + 1;
            }
            else if ((key == InteractionKey.ShiftTab) && (_selectedIndex != 0))
            {
                newSelectedIndex = _selectedIndex - 1;
            }

            if ((newSelectedIndex != -1) && (newSelectedIndex != _selectedIndex))
            {
                SelectMenuItem(_selectedIndex, false);
                SelectMenuItem(newSelectedIndex, true);
            }
        }
        private (int First, int Last, int RowNum) GetRowIndexBounds(int index)
        {
            int row = index / ItemsPerRow;
            int first = row * ItemsPerRow;
            int last = first + ItemsPerRow - 1;
            last = Math.Min(last, _items.Count - 1);

            return (first, last, row);
        }
        private void SelectMenuItem(int index, bool selected)
        {
            var position = GetMenuItemPosition(index);
            DrawMenuItem(_items[index], selected, position, true);
            _selectedIndex = selected ? index : -1;
            _selectedItem = _items[index];
            OnSelectedItemChanged?.Invoke();
        }
        private (int Left, int Top) GetMenuItemPosition(int index)
        {
            var rowBounds = GetRowIndexBounds(index);

            var left = (index - rowBounds.First) * (_widthPerItem + 2);
            var top = _menuRowStart + (rowBounds.RowNum * RowHeight) + TopPadding;

            return (left, top);
        }
        internal void DrawMenu()
        {
            if (!_items?.Any() ?? false) return;

            var curPos = ConsoleEx.GetCursorPos();
            var linesAdded = 0;

            linesAdded += AddLines(1);

            var rowItems = _items.Take(ItemsPerRow);
            _widthPerItem = (Console.WindowWidth / ItemsPerRow) - 2;
            var rowNum = 0;

            while (rowItems.Any())
            {
                linesAdded += AddLines(TopPadding);
                var thisRow = Console.CursorTop;

                foreach (var item in rowItems)
                {
                    DrawMenuItem(item, (item == _selectedItem), ConsoleEx.GetCursorPos(), false);
                }
                rowNum++;

                var linesToAdd = BottomPadding - (Console.CursorTop - thisRow); // If the console host moved us down a row, subtract one from the bottom padding
                linesAdded += AddLines(linesToAdd);

                rowItems = _items.Skip(ItemsPerRow * rowNum).Take(ItemsPerRow);
                if (rowItems.Any()) linesAdded += AddLines(1);
            }

            Console.WriteLine(); // Add a blank line at the bottom

            _menuRowEnd = Console.CursorTop;
            _menuRowStart = _menuRowEnd - linesAdded;

            Console.SetCursorPosition(curPos.Left, _menuRowStart - 1);
            _isOpen = true;
        }
        private void DrawMenuItem(string text, bool isSelected, (int Left, int Top) position, bool resetPos)
        {
            text = CenterString(text, _widthPerItem);
            var curBackClr = Console.BackgroundColor;

            DrawText(() =>
            {
                Console.SetCursorPosition(position.Left, position.Top);
                Console.ForegroundColor = isSelected ? SelectedTextColor : RegularTextColor;
                Console.BackgroundColor = isSelected ? SelectedBackColor : RegularBackColor;

                if (isSelected)
                {
                    DrawChar(ConsoleColor.Black, ConsoleColor.White, '[');
                    Console.Write($"{text}");
                    DrawChar(ConsoleColor.Black, ConsoleColor.White, ']');
                }
                else
                {
                    ClearChar(curBackClr);
                    Console.Write($"{text}");
                    ClearChar(curBackClr);
                }
            }, resetPos, true);
        }
        private void ClearMenu()
        {
            if (!_isOpen) return;

            DrawText(() =>
            {
                Console.ForegroundColor = ConsoleColor.Black;
                var line = new string(' ', Console.WindowWidth);
                Console.CursorLeft = 0;

                for (int i = _menuRowStart; i <= _menuRowEnd; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(line);
                }
            }, true, true);

            _selectedIndex = -1;
            _selectedItem = null;
            _isOpen = false;
        }
        private static void ClearChar(ConsoleColor color)
        {
            DrawChar(color, color, ' ');
        }
        private static void DrawChar(ConsoleColor backColor, ConsoleColor foreColor, char draw)
        {
            var curBClr = Console.BackgroundColor;
            var curFClr = Console.ForegroundColor;

            Console.ForegroundColor = foreColor;
            Console.BackgroundColor = backColor;
            Console.Write(draw);

            Console.BackgroundColor = curBClr;
            Console.ForegroundColor = curFClr;
        }
        private static void DrawText(Action drawAction, bool resetPos, bool resetColors)
        {
            var curPos = ConsoleEx.GetCursorPos();
            var curFColor = Console.ForegroundColor;
            var curBColor = Console.BackgroundColor;

            drawAction();

            if (resetPos) Console.SetCursorPosition(curPos.Left, curPos.Top);

            if (resetColors)
            {
                Console.ForegroundColor = curFColor;
                Console.BackgroundColor = curBColor;
            }
        }
        private static int AddLines(int lines)
        {
            if (lines < 0)
            {
                Console.SetCursorPosition(0, Console.CursorTop + lines);
            }
            else
            {
                for (int i = 0; i < lines; i++)
                {
                    Console.WriteLine();
                }
                Console.CursorLeft = 0;
            }
            return lines;
        }
        private static string CenterString(string text, int length)
        {
            var expression = text;

            if (expression.Length > length)
            {
                // TODO: trimming mode
                expression = text.Substring(0, length - 3) + "...";
            }
            else
            {
                var spaces = new string(' ', (length - expression.Length) / 2);
                expression = $"{spaces}{expression}{spaces}".PadRight(length);
            }

            return expression;
        }
    }
}