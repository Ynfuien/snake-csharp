using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Snake
{
    public partial class SnakeForm : Form
    {
        #region Configuration variables
        private const int _gridSize = 32;
        private const int _snakeSize = 5;
        private const int _scale = 20;
        private const int _size = _gridSize * _scale + _gridSize - 1;
        private const int _tickTime = 100;
        #endregion

        #region Game variables
        private static Random rng = new Random();
        private Border border;
        private Snake snake;
        private Berry berry;
        private Snake.Direction newDirection;
        private Snake.Direction direction;
        private bool gameOver = false;
        #endregion

        #region Colors
        private Color _colorBackground = Color.FromArgb(36, 36, 36);
        private Color _colorSnakeHead = Color.FromArgb(103, 36, 117);
        private Color _colorSnakeBody = Color.FromArgb(154, 74, 148);
        private Color _colorBerry = Color.FromArgb(255, 85, 85);
        private Color _colorBorder = Color.FromArgb(85, 85, 85);
        private Color _colorGameover = Color.FromArgb(255, 85, 85);
        private Color _colorScore = Color.FromArgb(255, 255, 85);
        private Color _colorScoreNumber = Color.FromArgb(255, 170, 0);
        #endregion


        public SnakeForm()
        {
            InitializeComponent();
            canvas.Width = _size;
            canvas.Height = _size;
        }

        private void SnakeForm_Load(object sender, EventArgs e)
        {
            Setup();
        }

        private void Setup()
        {
            border = new Border(_gridSize);
            snake = new Snake(_snakeSize);
            berry = new Berry(snake);
            direction = Snake.Direction.Right;
            newDirection = direction;

            canvas.BackColor = _colorBackground;

            gameTimer.Interval = _tickTime;
            gameTimer.Enabled = true;
            gameTimer.Start();
        }


        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Game over screen
            if (gameOver)
            {
                int scale = (int)(_scale * 1.2);
                int score = snake.GetSize() - _snakeSize;

                Font font = new Font("Arial", scale);
                RenderText(g, "Game over!", font, _colorGameover, (int) (_size / 2), (int) (_size / 2 - (scale * 2.3)));
                RenderText(g, $"Score: {score}", font, _colorScoreNumber, (int) (_size / 2), (int) (_size / 2 - scale));
                RenderText(g, $"Score: {new string(' ', score.ToString().Length * 2)}", font, _colorScore, (int) (_size / 2), (int) (_size / 2 - scale));

                border.Render(g, _colorBorder);
                return;
            }

            // Render everything
            snake.Render(g, _colorSnakeHead, _colorSnakeBody);
            berry.Render(g, _colorBerry);
            border.Render(g, _colorBorder);
        }

        // Renders centered text
        private void RenderText(Graphics g, string text, Font font, Color color, int x, int y) {
            Size textSize = TextRenderer.MeasureText(text, font);
            g.DrawString(text, font, new SolidBrush(color), new Point(x - (textSize.Width / 2), y - (textSize.Height / 2)));
        }

        private void SnakeForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameOver) return;

            Keys key = e.KeyCode;
            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    if (direction == Snake.Direction.Down) break;
                    newDirection = Snake.Direction.Up;
                    break;
                case Keys.Down:
                case Keys.S:
                    if (direction == Snake.Direction.Up) break;
                    newDirection = Snake.Direction.Down;
                    break;
                case Keys.Left:
                case Keys.A:
                    if (direction == Snake.Direction.Right) break;
                    newDirection = Snake.Direction.Left;
                    break;
                case Keys.Right:
                case Keys.D:
                    if (direction == Snake.Direction.Left) break;
                    newDirection = Snake.Direction.Right;
                    break;
            }
        }

        private void gameTimer_Tick(object sender, EventArgs e)
        {
            direction = newDirection;

            // Move snake and check if it actually moved
            if (!snake.Move(direction, border))
            {
                // Game over
                gameOver = true;
                canvas.Refresh();
                gameTimer.Stop();
                return;
            }

            // Check if snake got the berry
            if (snake.ContainsBerry(berry))
            {
                berry = new Berry(snake);
                snake.Grow();
            }

            // Render everything to user
            canvas.Refresh();
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

            public void Render(Graphics g, Color color)
            {
                Brush brush = new SolidBrush(color);
                g.FillRectangle(brush, x * _scale + x, y * _scale + y, _scale, _scale);
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
                for (int i = 0; i <= size - 1; i++)
                {
                    // Border in width
                    borderPixels.Add(new Pixel(i, 0));
                    borderPixels.Add(new Pixel(i, size - 1));

                    // Border in height
                    if (i == 0 || i == size - 1) continue;
                    borderPixels.Add(new Pixel(0, i));
                    borderPixels.Add(new Pixel(size - 1, i));
                }
            }

            public void Render(Graphics g, Color color)
            {
                foreach (Pixel p in borderPixels)
                {
                    p.Render(g, color);
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

            public Snake(int size)
            {
                headPixel = new Pixel(_gridSize / 2 + (size / 2), _gridSize / 2 - 1);
                for (int i = size - 1; i > 0; i--)
                {
                    bodyPixels.Add(new Pixel(headPixel.x - i, headPixel.y));
                }
            }

            public void Render(Graphics g, Color headColor, Color bodyColor)
            {
                headPixel.Render(g, headColor);
                foreach (Pixel p in bodyPixels)
                {
                    p.Render(g, bodyColor);
                }
            }

            public bool Move(Direction direction, Border border)
            {
                int x = headPixel.x;
                int y = headPixel.y;

                if (direction == Direction.Up) y--;
                else if (direction == Direction.Right) x++;
                else if (direction == Direction.Down) y++;
                else if (direction == Direction.Left) x--;

                Pixel newHead = new Pixel(x, y);
                if (this.Contains(newHead)) return false;
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

            // Separate method for checking for berry,
            // because only head pixel can move onto berry position
            public bool ContainsBerry(Berry berry)
            {
                return headPixel.Equals(berry.position);
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
                    position = new Pixel(rng.Next(1, _gridSize - 1), rng.Next(1, _gridSize - 1));
                } while (snake.Contains(position));
            }

            public void Render(Graphics g, Color color)
            {
                position.Render(g, color);
            }
        }
    }
}
