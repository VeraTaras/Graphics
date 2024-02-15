using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Graphics
{
    public partial class Form1 : Form
    {
        enum Tools { Arrow, Line, Ellipse, Fill, Pencil, Eraser };
        Tools tool = Tools.Arrow;
        int x0, y0, x1, y1;
        System.Drawing.Graphics graphics;
        Bitmap bitmap;
        bool leftMouseDown = false;
        Pen pen = new Pen(Color.Red);
        bool isFillClicked = false; // Flag to track fill activation
        Color fillColor = Color.Black; // Fill color
        Stack<Image> historyStack = new Stack<Image>(); // Declare stack to store action history
        Stack<Image> redoStack = new Stack<Image>();
        private List<Point> pencilPoints = new List<Point>(); // List of points to store pencil strokes
        private bool isPencilDrawing = false; // Flag to track pencil drawing mode

        public Form1()
        {
            InitializeComponent();
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            graphics = System.Drawing.Graphics.FromImage(bitmap);
            pictureBox1.Image = bitmap;
        }

        #region Main functions
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (tool == Tools.Pencil)
                {
                    isPencilDrawing = true;
                    pencilPoints.Clear();
                    pencilPoints.Add(e.Location);
                }
                else
                {
                    x0 = e.X; y0 = e.Y;
                    leftMouseDown = true;
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (tool == Tools.Pencil && isPencilDrawing)
            {
                isPencilDrawing = false;
                if (pencilPoints.Count > 1)
                {
                    graphics.DrawLines(pen, pencilPoints.ToArray());
                    SaveToHistory();
                }
            }
            else if (isFillClicked && tool == Tools.Fill)
            {
                FloodFill(e.X, e.Y, bitmap.GetPixel(e.X, e.Y), pen.Color);
                pictureBox1.Image = bitmap;
                isFillClicked = false;
                SaveToHistory();
            }
            else
            {
                if (leftMouseDown)
                {
                    switch (tool)
                    {
                        case Tools.Line:
                            graphics.DrawLine(pen, x0, y0, e.X, e.Y);
                            pen.EndCap = LineCap.Flat;
                            break;
                        case Tools.Ellipse:
                            graphics.DrawEllipse(pen, x0, y0, Math.Abs(e.X - x0), Math.Abs(e.Y - y0));
                            break;
                        case Tools.Arrow:
                            graphics.DrawLine(pen, x0, y0, e.X, e.Y);
                            pen.CustomEndCap = new AdjustableArrowCap(5, 5);
                            pen.EndCap = LineCap.ArrowAnchor;
                            break;
                    }

                    pictureBox1.Image = bitmap;
                    leftMouseDown = false;
                    SaveToHistory();
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPencilDrawing)
            {
                pencilPoints.Add(e.Location);
                pictureBox1.Invalidate();
            }
            else if (leftMouseDown)
            {
                switch (tool)
                {
                    case Tools.Line:
                        x1 = e.X; y1 = e.Y;
                        pen.EndCap = LineCap.Flat;
                        break;
                    case Tools.Ellipse:
                        x1 = e.X; y1 = e.Y;
                        break;
                    case Tools.Arrow:
                        x1 = e.X; y1 = e.Y;
                        break;
                    case Tools.Eraser:
                        if (pen.Color != pictureBox1.BackColor)
                        {
                            pen.Color = pictureBox1.BackColor;
                        }
                        x1 = e.X; y1 = e.Y;
                        graphics.DrawLine(pen, x0, y0, x1, y1);
                        x0 = x1; y0 = y1;
                        pictureBox1.Refresh();
                        break;
                }
                pictureBox1.Invalidate();
                pictureBox1.Refresh();
            }
        }

        private void pictureBox1_Paint_1(object sender, PaintEventArgs e)
        {
            System.Drawing.Graphics graphics = e.Graphics;
            if (isPencilDrawing)
            {
                if (pencilPoints.Count > 1)
                {
                    graphics.DrawLines(pen, pencilPoints.ToArray());
                }
            }
            else if (leftMouseDown)
            {
                switch (tool)
                {
                    case Tools.Line:
                        graphics.DrawLine(pen, x0, y0, x1, y1);
                        pen.EndCap = LineCap.Flat;
                        break;
                    case Tools.Ellipse:
                        graphics.DrawEllipse(pen, x0, y0, Math.Abs(x1 - x0), Math.Abs(y1 - y0));
                        break;
                    case Tools.Arrow:
                        graphics.DrawLine(pen, x0, y0, x1, y1);
                        pen.EndCap = LineCap.ArrowAnchor;
                        pen.CustomEndCap = new AdjustableArrowCap(5, 5);
                        break;
                    case Tools.Eraser:
                        ErasePixels(x0, y0, x1, y1);
                        break;
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            pen.Width = trackBar1.Value;
        }
        #endregion

        #region Figures
        private void lineButton_Click(object sender, EventArgs e)
        {
            tool = Tools.Line;
        }

        private void ellipseButton_Click(object sender, EventArgs e)
        {
            tool = Tools.Ellipse;
        }

        private void arrowButton_Click(object sender, EventArgs e)
        {
            tool = Tools.Arrow;
        }
        #endregion

        #region Tools
        private void pencilButton_Click(object sender, EventArgs e)
        {
            tool = Tools.Pencil;
        }

        private void eraserButton_Click(object sender, EventArgs e)
        {
            tool = Tools.Eraser;
        }

        private void fillButton_Click(object sender, EventArgs e)
        {
            tool = Tools.Fill;
            isFillClicked = true;
            fillColor = pen.Color;
        }

        private void undoButton_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void redoButton_Click(object sender, EventArgs e)
        {
            Redo();
        }
        #endregion

        #region Functions
        private void ErasePixels(int startX, int startY, int endX, int endY)
        {
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
            using (Pen eraserPen = new Pen(pictureBox1.BackColor, trackBar1.Value))
            {
                g.DrawLine(eraserPen, startX, startY, endX, endY);
            }
        }

        private void FloodFill(int x, int y, Color targetColor, Color replacementColor)
        {
            if (bitmap.GetPixel(x, y) != targetColor || targetColor == replacementColor)
                return;

            if (targetColor.ToArgb() == replacementColor.ToArgb())
                return;

            Stack<Point> stack = new Stack<Point>();
            stack.Push(new Point(x, y));

            while (stack.Count > 0)
            {
                Point point = stack.Pop();
                int px = point.X;
                int py = point.Y;

                if (px >= 0 && px < bitmap.Width && py >= 0 && py < bitmap.Height &&
                    bitmap.GetPixel(px, py) == targetColor)
                {
                    bitmap.SetPixel(px, py, replacementColor);

                    stack.Push(new Point(px - 1, py));
                    stack.Push(new Point(px + 1, py));
                    stack.Push(new Point(px, py - 1));
                    stack.Push(new Point(px, py + 1));
                }
            }
            SaveToHistory();
        }

        // Метод для сохранения текущего состояния изображения в истории
        private void SaveToHistory()
        {
            Image currentImage = (Image)bitmap.Clone();
            historyStack.Push(currentImage);

            // При сохранении нового состояния изображения, очищаем стек отменённых состояний
            redoStack.Clear();
        }


        // Метод для отмены последнего действия
        private void Undo()
        {
            if (historyStack.Count >= 1)
            {
                // Извлекаем текущее состояние изображения из истории
                Image currentImage = historyStack.Pop();

                if (historyStack.Count == 0)
                {
                    // Если история пуста, очищаем графику и возвращаем пустое изображение
                    graphics.Clear(pictureBox1.BackColor);
                    bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                    graphics = System.Drawing.Graphics.FromImage(bitmap);
                    pictureBox1.Image = bitmap;
                }
                else
                {
                    // Восстанавливаем предыдущее состояние изображения
                    Image previousImage = historyStack.Peek();
                    bitmap = new Bitmap(previousImage);

                    // Обновляем graphics
                    graphics = System.Drawing.Graphics.FromImage(bitmap);

                    // Обновляем изображение на PictureBox
                    pictureBox1.Image = bitmap;
                }

                // Сохраняем текущее состояние в стеке отменённых состояний
                redoStack.Push(currentImage);
            }
        }


        // Метод для возврата отменённого действия
        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                // Извлекаем отменённое состояние изображения из стека отменённых состояний
                Image redoImage = redoStack.Pop();

                // Добавляем отменённое состояние в историю действий
                historyStack.Push(redoImage);

                // Восстанавливаем отменённое состояние изображения
                bitmap = new Bitmap(redoImage);
                graphics = System.Drawing.Graphics.FromImage(bitmap);
                pictureBox1.Image = bitmap;
            }
        }
        #endregion

        #region Buttons

        private void clearButton_Click(object sender, EventArgs e)
        {
            graphics.Clear(pictureBox1.BackColor);
            pictureBox1.Image = bitmap;
        }
        
        private void saveButton_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "JPG(*.JPG)|*.jpg";
            if(saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Save(saveFileDialog1.FileName);
                }
            }
        }
        #endregion

        #region Color
        private void white_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void lightCoral_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void sandyBrown_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void lemon_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void paleGreen_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void sky_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void royalBlue_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void magenta_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void orchid_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void gray_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void red_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void orrange_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void yellow_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void lime_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void deepBlue_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void blue_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void purple_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void black_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void brown_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void chocolate_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void gold_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void green_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void steelBlue_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }

        private void darkBlue_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
            fillColor = ((Button)sender).BackColor;
        }
        #endregion

        
    }
}