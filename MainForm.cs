using DescartesApp.Models;
using DescartesApp.Services;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Globalization;

namespace DescartesApp
{
    public partial class MainForm : Form
    {
        private DataTable currentData = new DataTable();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Программа для построения графика Декартова листа.\n" +
                "Автор: Афаунова Вероника Муратовна\n" +
                "Группа: 444\n" +
                "Задача: визуализация функции y = ± x * sqrt((l+x)/(l-3x)), l = 3a/√2\n" +
                "Дробные числа можно вводить как с точкой, так и с запятой.",
                "О программе",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            txtA.Text = "1";
            txtLeft.Text = "-1.5";
            txtRight.Text = "0.6";
            txtStep.Text = "0.01";

            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("X", "X");
            dataGridView1.Columns.Add("Y+", "Y+");
            dataGridView1.Columns.Add("Y-", "Y-");
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            try
            {
                double a = double.Parse(txtA.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                double left = double.Parse(txtLeft.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                double right = double.Parse(txtRight.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                double step = double.Parse(txtStep.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

                if (step <= 0) throw new Exception("Шаг должен быть положительным");
                if (left > right) throw new Exception("Левая граница не может быть больше правой");

                // Проверка области определения (предупреждение)
                var domain = DescartesFunction.GetDomain(a);
                if (domain.HasValue)
                {
                    var (dLeft, dRight) = domain.Value;
                    // Если заданный интервал выходит за область определения, предупреждаем
                    if (left < dLeft || right > dRight)
                    {
                        MessageBox.Show($"Область определения для a={a} примерно ({dLeft:F3}; {dRight:F3}).\n" +
                                        "График может быть построен только в этом интервале. Неопределённые значения будут пропущены.",
                                        "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    // Если a=0 или область не определена
                    MessageBox.Show("При a=0 функция вырождается, попробуйте другое значение a.",
                                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                currentData.Generate(left, right, step, a);

                dataGridView1.Rows.Clear();
                foreach (var p in currentData.Points)
                {
                    dataGridView1.Rows.Add(
                        p.X.ToString("F4"),
                        p.YPositive?.ToString("F4") ?? "",
                        p.YNegative?.ToString("F4") ?? ""
                    );
                }

                DrawGraph();

                // Если точек слишком много – предупреждение
                if (currentData.Points.Count > 10000)
                {
                    MessageBox.Show("Построено большое количество точек, отображение может быть замедленно.",
                                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Введите корректные числа (используйте точку или запятую).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DrawGraph()
        {
            if (currentData.Points.Count == 0) return;

            Bitmap bmp = new Bitmap(pictureBoxGraph.Width, pictureBoxGraph.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;

                foreach (var p in currentData.Points)
                {
                    if (p.YPositive.HasValue)
                    {
                        if (p.X < minX) minX = p.X;
                        if (p.X > maxX) maxX = p.X;
                        if (p.YPositive.Value < minY) minY = p.YPositive.Value;
                        if (p.YPositive.Value > maxY) maxY = p.YPositive.Value;
                    }
                    if (p.YNegative.HasValue)
                    {
                        if (p.X < minX) minX = p.X;
                        if (p.X > maxX) maxX = p.X;
                        if (p.YNegative.Value < minY) minY = p.YNegative.Value;
                        if (p.YNegative.Value > maxY) maxY = p.YNegative.Value;
                    }
                }

                double margin = 0.1 * (maxX - minX);
                if (margin == 0) margin = 0.5;
                minX -= margin;
                maxX += margin;
                double yMargin = 0.1 * (maxY - minY);
                if (yMargin == 0) yMargin = 0.5;
                minY -= yMargin;
                maxY += yMargin;

                float w = pictureBoxGraph.Width;
                float h = pictureBoxGraph.Height;
                float scaleX = w / (float)(maxX - minX);
                float scaleY = h / (float)(maxY - minY);

                Func<double, double, PointF> toPixel = (x, y) =>
                    new PointF(
                        (float)((x - minX) * scaleX),
                        (float)((maxY - y) * scaleY)
                    );

                using (Pen axisPen = new Pen(Color.LightGray, 1))
                {
                    if (minY <= 0 && maxY >= 0)
                    {
                        float y0 = toPixel(0, 0).Y;
                        g.DrawLine(axisPen, 0, y0, w, y0);
                    }
                    if (minX <= 0 && maxX >= 0)
                    {
                        float x0 = toPixel(0, 0).X;
                        g.DrawLine(axisPen, x0, 0, x0, h);
                    }
                }

                // Вычисляем шаг из данных для определения разрывов
                double actualStep = 0;
                if (currentData.Points.Count >= 2)
                    actualStep = Math.Abs(currentData.Points[1].X - currentData.Points[0].X);
                else
                {
                    try { actualStep = double.Parse(txtStep.Text.Replace(',', '.'), CultureInfo.InvariantCulture); }
                    catch { actualStep = 0.01; }
                }

                using (Pen penPos = new Pen(Color.Blue, 2))
                using (Pen penNeg = new Pen(Color.Red, 2))
                {
                    PointF? prevPos = null;
                    PointF? prevNeg = null;

                    for (int i = 0; i < currentData.Points.Count; i++)
                    {
                        var p = currentData.Points[i];

                        if (p.YPositive.HasValue)
                        {
                            PointF cur = toPixel(p.X, p.YPositive.Value);
                            if (prevPos.HasValue)
                            {
                                double dx = (i > 0) ? Math.Abs(p.X - currentData.Points[i - 1].X) : 0;
                                if (dx <= 2 * actualStep * 1.5)
                                    g.DrawLine(penPos, prevPos.Value, cur);
                                else
                                    prevPos = null;
                            }
                            prevPos = cur;
                        }
                        else prevPos = null;

                        if (p.YNegative.HasValue)
                        {
                            PointF cur = toPixel(p.X, p.YNegative.Value);
                            if (prevNeg.HasValue)
                            {
                                double dx = (i > 0) ? Math.Abs(p.X - currentData.Points[i - 1].X) : 0;
                                if (dx <= 2 * actualStep * 1.5)
                                    g.DrawLine(penNeg, prevNeg.Value, cur);
                                else
                                    prevNeg = null;
                            }
                            prevNeg = cur;
                        }
                        else prevNeg = null;
                    }
                }
            }
            pictureBoxGraph.Image = bmp;
        }

        // Обработчики меню
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    currentData = FileManager.LoadTable(openFileDialog1.FileName);
                    dataGridView1.Rows.Clear();
                    foreach (var p in currentData.Points)
                    {
                        dataGridView1.Rows.Add(p.X.ToString("F4"), p.YPositive?.ToString("F4") ?? "", p.YNegative?.ToString("F4") ?? "");
                    }
                    DrawGraph();
                    MessageBox.Show("Данные загружены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentData.Points.Count == 0)
            {
                MessageBox.Show("Нет данных для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            saveFileDialog1.Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileManager.SaveTable(saveFileDialog1.FileName, currentData);
                    MessageBox.Show("Данные сохранены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Декартов лист\n" +
                "Функция: y = ± x * sqrt((l+x)/(l-3x)), l = 3a/√2\n" +
                "Автор: Афаунова Вероника Муратовна\n" +
                "Группа: 444\n" +
                "2026",
                "О программе",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void pictureBoxGraph_Resize(object sender, EventArgs e)
        {
            if (currentData.Points.Count > 0)
                DrawGraph();
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void pictureBoxGraph_Click(object sender, EventArgs e)
        {
        }
    }
}