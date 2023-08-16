using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Snake
{
    class Program
    {
        #region Static configuration variables
        private const int _gridSize = 24;
        private const int _snakeSize = 5;
        private const char _pixelChar = '■';
        private const int _tickTime = 150;

        // Colors
        private const ConsoleColor _colorSnakeHead = ConsoleColor.DarkYellow;
        private const ConsoleColor _colorSnakeBody = ConsoleColor.Yellow;
        private const ConsoleColor _colorBerry = ConsoleColor.Red;
        private const ConsoleColor _colorBorder = ConsoleColor.DarkGray;
        private const ConsoleColor _colorGameover = ConsoleColor.Red;
        private const ConsoleColor _colorScore = ConsoleColor.Yellow;
        private const ConsoleColor _colorScoreNumber = ConsoleColor.DarkYellow;
        #endregion

        #region Game variables
        private static Random rng = new Random();
        private static Border border;
        private static Snake snake;
        private static Berry berry;
        private static Snake.Direction direction;
        private static bool gameover = false;
        #endregion


        #region Blocking window resize
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_SIZE = 0xF000;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        #endregion

        static void Main(string[] args)
        {
            Setup();
            Render();

            // Blocking window resize
            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);

            if (handle != IntPtr.Zero)
            {
                DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
            }


            // Running game
            DateTime lastTick = DateTime.Now;
            Snake.Direction newDirection = direction;
            while (!gameover)
            {
                // Check for pressing keys
                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;

                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            if (direction == Snake.Direction.Down) break;
                            newDirection = Snake.Direction.Up;
                            break;
                        case ConsoleKey.DownArrow:
                            if (direction == Snake.Direction.Up) break;
                            newDirection = Snake.Direction.Down;
                            break;
                        case ConsoleKey.LeftArrow:
                            if (direction == Snake.Direction.Right) break;
                            newDirection = Snake.Direction.Left;
                            break;
                        case ConsoleKey.RightArrow:
                            if (direction == Snake.Direction.Left) break;
                            newDirection = Snake.Direction.Right;
                            break;
                    }
                }

                // Tick every _tickTime miliseconds
                DateTime now = DateTime.Now;
                if (now.Subtract(lastTick).TotalMilliseconds >= _tickTime)
                {
                    lastTick = now;
                    direction = newDirection;
                    Tick();
                }
            }

            Thread.Sleep(1000);
            Console.ReadKey();
        }

        private static void Setup()
        {
            Console.SetWindowSize(_gridSize * 2 + 1, _gridSize + 1);
            Console.SetBufferSize(_gridSize * 2 + 2, _gridSize + 2);

            border = new Border(_gridSize);
            snake = new Snake(_snakeSize, _gridSize);
            berry = new Berry(snake);
            direction = Snake.Direction.Right;
        }

        private static void Render()
        {
            // Clear screen
            Console.Clear();

            // Game over screen
            if (gameover)
            {
                Console.ForegroundColor = _colorGameover;
                Console.SetCursorPosition(_gridSize - 4, _gridSize / 2 - 2);
                Console.Write("Game over!");

                int score = snake.GetSize() - _snakeSize;
                Console.ForegroundColor = _colorScore;
                // _gridSize - (score > 9 ? 4 : 3)
                // Means:
                // half of the screen width minus (4 if score is at least 2 numbers or else minus 3)
                // Just so it's more centered
                Console.SetCursorPosition(_gridSize - (score > 9 ? 4 : 3), _gridSize / 2 - 1);
                Console.Write("Score: ");
                Console.ForegroundColor = _colorScoreNumber;
                Console.Write(score);

                border.Render(_colorBorder);
                return;
            }

            // Render everything
            snake.Render(_colorSnakeHead, _colorSnakeBody);
            berry.Render(_colorBerry);
            border.Render(_colorBorder);
        }

        private static void Tick()
        {
            // Move snake and check if it actually moved
            if (!snake.Move(direction))
            {
                // Game over
                gameover = true;
                Render();
                return;
            }

            // Check if snake got the berry
            if (snake.Contains(berry.position))
            {
                berry = new Berry(snake);
                snake.Grow();
            }

            // Render everything to user
            Render();
        }

        class Pixel
        {
            public int x;
            public int y;

            public Pixel(int x, int y)
            {
                this.x = x > _gridSize ? _gridSize : x;
                this.y = y > _gridSize ? _gridSize : y;
            }

            public void Render(ConsoleColor color)
            {
                Console.ForegroundColor = color;
                Console.SetCursorPosition(x * 2, y);
                Console.Write(_pixelChar);
            }

            public bool Equals(Pixel pixel)
            {
                return pixel.x == x && pixel.y == y;
            }
        }

        class Border
        {
            private List<Pixel> borderPixels = new List<Pixel>();

            public Border(int size)
            {
                for (int i = 0; i <= size; i++)
                {
                    // Border in width
                    borderPixels.Add(new Pixel(i, 0));
                    borderPixels.Add(new Pixel(i, size));

                    // Border in height
                    if (i == 0 || i == size) continue;
                    borderPixels.Add(new Pixel(0, i));
                    borderPixels.Add(new Pixel(size, i));
                }
            }

            public void Render(ConsoleColor color)
            {
                foreach (Pixel p in borderPixels)
                {
                    p.Render(color);
                }
            }

            public bool Contains(Pixel pixel)
            {
                foreach (Pixel p in borderPixels)
                {
                    if (p.Equals(pixel)) return true;
                }

                return false;
            }
        }

        class Snake
        {
            private List<Pixel> bodyPixels = new List<Pixel>();
            private Pixel headPixel;
            private int gridSize;

            public Snake(int size, int gridSize)
            {
                this.gridSize = gridSize;

                headPixel = new Pixel(gridSize / 2 + (size / 2), gridSize / 2 - 1);
                for (int i = size - 1; i > 0; i--)
                {
                    bodyPixels.Add(new Pixel(headPixel.x - i, headPixel.y));
                }
            }

            public void Render(ConsoleColor headColor, ConsoleColor bodyColor)
            {
                headPixel.Render(headColor);
                foreach (Pixel p in bodyPixels)
                {
                    p.Render(bodyColor);
                }
            }

            public bool Move(Direction direction)
            {
                int x = headPixel.x;
                int y = headPixel.y;

                if (direction == Direction.Up) y--;
                else if (direction == Direction.Right) x++;
                else if (direction == Direction.Down) y++;
                else if (direction == Direction.Left) x--;

                Pixel newHead = new Pixel(x, y);
                if (snake.Contains(newHead)) return false;
                if (border.Contains(newHead)) return false;

                bodyPixels.Add(headPixel);
                bodyPixels.RemoveAt(0);
                headPixel = newHead;
                return true;
            }

            public void Grow(int by = 1)
            {
                Pixel newBody = new Pixel(bodyPixels[0].x, bodyPixels[0].y);
                for (int i = 0; i < by; i++)
                {
                    bodyPixels.Insert(0, newBody);
                }
            }

            public int GetSize()
            {
                return bodyPixels.Count + 1;
            }

            public bool Contains(Pixel pixel)
            {
                if (headPixel.Equals(pixel)) return true;
                foreach (Pixel p in bodyPixels)
                {
                    if (p.Equals(pixel)) return true;
                }

                return false;
            }

            public enum Direction
            {
                Up, Down, Left, Right
            }
        }

        class Berry
        {
            public Pixel position;

            public Berry(Snake snake)
            {
                do
                {
                    position = new Pixel(rng.Next(1, _gridSize), rng.Next(1, _gridSize));
                } while (snake.Contains(position));
            }

            public void Render(ConsoleColor color)
            {
                position.Render(color);
            }
        }

    }
}
