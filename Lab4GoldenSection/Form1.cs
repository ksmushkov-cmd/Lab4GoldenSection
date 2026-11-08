using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Lab4GoldenSection {
    public partial class Form1 : Form
    {
        private const double PHI = 0.6180339887498948482;
        private string _formula = "";

        public Form1()
        {
            InitializeComponent();
            SetupChart();
        }

        private void SetupChart()
        {
            if (chart1.ChartAreas.Count == 0)
                chart1.ChartAreas.Add(new ChartArea("main"));

            chart1.Series.Clear();
            var s = new Series("f(x)")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.SteelBlue,
                XValueType = ChartValueType.Double,
                YValueType = ChartValueType.Double
            };
            chart1.Series.Add(s);

            var ca = chart1.ChartAreas[0];
            ca.AxisX.Title = "x";
            ca.AxisY.Title = "f(x)";
            ca.AxisX.MajorGrid.LineColor = Color.LightGray;
            ca.AxisY.MajorGrid.LineColor = Color.LightGray;
            ca.AxisX.Crossing = 0;
            ca.AxisY.Crossing = 0;

            ca.AxisX.LabelStyle.Font = new Font("Microsoft Sans Serif", 9F);
            ca.AxisY.LabelStyle.Font = new Font("Microsoft Sans Serif", 9F);
            ca.AxisX.LabelStyle.Format = "0.##";
            ca.AxisY.LabelStyle.Format = "0.##";
        }

        private void btnPlot_Click(object sender, EventArgs e)
        {
            lblResult.Text = "";
            lblStatus.ForeColor = Color.Red;

            string formula = txtF.Text.Trim();
            if (string.IsNullOrWhiteSpace(formula))
            {
                lblStatus.Text = "Введите формулу f(x).";
                return;
            }
            if (!TryParseDouble(txtA.Text, out double a))
            {
                lblStatus.Text = "Параметр a задан некорректно.";
                return;
            }
            if (!TryParseDouble(txtB.Text, out double b))
            {
                lblStatus.Text = "Параметр b задан некорректно.";
                return;
            }
            if (a >= b)
            {
                lblStatus.Text = "Требуется a < b.";
                return;
            }
            if (!TryParseDouble(txtE.Text, out double eps) || eps <= 0)
            {
                lblStatus.Text = "Погрешность ε должна быть положительным числом.";
                return;
            }

            try
            {
                double test = FormulaParser.Eval(formula, (a + b) / 2.0);
                if (double.IsNaN(test) || double.IsInfinity(test))
                    throw new Exception();
            }
            catch
            {
                lblStatus.Text = "Не удалось вычислить f(x). Проверьте формулу.";
                return;
            }

            _formula = formula;
            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = "График построен. Выберите режим и нажмите «Рассчитать» в меню.";

            DrawGraph(a, b);
        }

        private void DrawGraph(double a, double b)
        {
            chart1.Series[0].Points.Clear();

            int N = 800;
            double step = (b - a) / N;
            double yMin = double.MaxValue, yMax = double.MinValue;

            for (int i = 0; i <= N; i++)
            {
                double x = a + i * step;
                double y;
                try { y = FormulaParser.Eval(_formula, x); }
                catch { y = double.NaN; }

                if (!double.IsNaN(y) && !double.IsInfinity(y) && Math.Abs(y) < 1e6)
                {
                    if (y < yMin) yMin = y;
                    if (y > yMax) yMax = y;
                }
            }

            double pad = (yMax - yMin) * 0.1;
            if (pad <= 0) pad = 1;
            yMin -= pad;
            yMax += pad;

            chart1.ChartAreas[0].AxisY.Minimum = yMin;
            chart1.ChartAreas[0].AxisY.Maximum = yMax;
            chart1.ChartAreas[0].AxisX.Minimum = a;
            chart1.ChartAreas[0].AxisX.Maximum = b;

            for (int i = 0; i <= N; i++)
            {
                double x = a + i * step;
                double y;
                try { y = FormulaParser.Eval(_formula, x); }
                catch { continue; }

                if (double.IsNaN(y) || double.IsInfinity(y) || Math.Abs(y) > 1e6)
                    continue;

                chart1.Series[0].Points.AddXY(x, y);
            }
        }

        private void menuCalc_Click(object sender, EventArgs e)
        {
            lblResult.Text = "";
            lblStatus.ForeColor = Color.Red;

            if (string.IsNullOrWhiteSpace(_formula))
            {
                lblStatus.Text = "Сначала постройте график (кнопка «Построить график»).";
                return;
            }
            if (!TryParseDouble(txtA.Text, out double a) ||
                !TryParseDouble(txtB.Text, out double b) ||
                !TryParseDouble(txtE.Text, out double eps) || eps <= 0)
            {
                lblStatus.Text = "Проверьте параметры a, b, ε.";
                return;
            }

            try
            {
                switch (cmbMode.SelectedIndex)
                {
                    case 0: FindSingleExtremum(a, b, eps, true); break;   
                    case 1: FindSingleExtremum(a, b, eps, false); break;  
                    case 2: FindAllExtrema(a, b, eps); break;             
                    case 3: FindRootsGoldenSection(a, b, eps); break;     
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Ошибка расчёта: " + ex.Message;
            }
        }

        private void FindSingleExtremum(double a, double b, double eps, bool isMin)
        {
            double x1 = b - PHI * (b - a);
            double x2 = a + PHI * (b - a);
            double f1 = FormulaParser.Eval(_formula, x1);
            double f2 = FormulaParser.Eval(_formula, x2);

            const int maxIter = 10000;
            int guard = 0;

            while ((b - a) > eps && guard < maxIter)
            {
                bool keepLeft = isMin ? (f1 < f2) : (f1 > f2);

                if (keepLeft)
                {
                    b = x2;
                    x2 = x1; f2 = f1;
                    x1 = b - PHI * (b - a);
                    f1 = FormulaParser.Eval(_formula, x1);
                }
                else
                {
                    a = x1;
                    x1 = x2; f1 = f2;
                    x2 = a + PHI * (b - a);
                    f2 = FormulaParser.Eval(_formula, x2);
                }
                guard++;
            }

            double xOpt = (a + b) / 2.0;
            double fOpt = FormulaParser.Eval(_formula, xOpt);

            MarkPoint(xOpt, fOpt, isMin ? "min" : "max", isMin ? Color.Red : Color.Green);

            string kind = isMin ? "Минимум" : "Максимум";
            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = kind + " найден.";
            lblResult.Text =
                $"{kind}:\n" +
                $"x* = {xOpt:F6}\n" +
                $"f(x*) = {fOpt:F6}\n" +
                $"Достигнутая точность: {(b - a):E4}";
        }

        private void FindAllExtrema(double a, double b, double eps)
        {
            var extrema = new List<(double x, double y, bool isMin)>();

            int N = 400;
            double h = (b - a) / N;

            double prevDeriv = Derivative(a);
            double prevX = a;

            for (int i = 1; i <= N; i++)
            {
                double x = a + i * h;
                double deriv = Derivative(x);

                if (double.IsNaN(prevDeriv) || double.IsNaN(deriv))
                {
                    prevDeriv = deriv; prevX = x; continue;
                }

                if (Math.Sign(prevDeriv) != Math.Sign(deriv))
                {
                    double xOpt = GoldenSectionLocal(prevX, x, eps);
                    double yOpt = SafeEval(xOpt);
                    if (!double.IsNaN(yOpt))
                    {
                        bool isMin = deriv > 0;
                        extrema.Add((xOpt, yOpt, isMin));
                    }
                }

                prevDeriv = deriv; prevX = x;
            }

            extrema = DeduplicateExtrema(extrema, 1e-4);

            if (extrema.Count == 0)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "Локальных экстремумов на интервале не найдено.";
                return;
            }

            foreach (var ex in extrema)
                MarkPoint(ex.x, ex.y, ex.isMin ? "min" : "max", ex.isMin ? Color.Red : Color.Green);

            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = $"Найдено экстремумов: {extrema.Count}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Локальные экстремумы:");
            foreach (var ex in extrema)
                sb.AppendLine($"x = {ex.x:F6},  f(x) = {ex.y:F6},  {(ex.isMin ? "минимум" : "максимум")}");
            lblResult.Text = sb.ToString();
        }

        private void FindRootsGoldenSection(double a, double b, double eps)
        {
            var roots = new List<double>();

            int N = 400;
            double h = (b - a) / N;
            double prevX = a;
            double prevF = SafeEval(a);

            for (int i = 1; i <= N; i++)
            {
                double x = a + i * h;
                double f = SafeEval(x);

                if (double.IsNaN(prevF) || double.IsNaN(f))
                {
                    prevF = f; prevX = x; continue;
                }

                if (Math.Sign(prevF) != Math.Sign(f))
                {
                    double root = GoldenSectionLocalAbs(prevX, x, eps);
                    if (!double.IsNaN(root))
                        roots.Add(root);
                }

                prevF = f; prevX = x;
            }

            roots = Deduplicate(roots, 1e-5);

            if (roots.Count == 0)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "Корней (точек смены знака) на интервале не найдено.";
                return;
            }

            foreach (var r in roots)
            {
                double yVal = SafeEval(r);
                MarkPoint(r, double.IsNaN(yVal) ? 0 : yVal, "root", Color.OrangeRed);
            }

            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = $"Найдено корней: {roots.Count}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Точки смены знака (корни):");
            foreach (var r in roots)
            {
                double yVal = SafeEval(r);
                sb.AppendLine($"x = {r:F6},  f(x) = {(double.IsNaN(yVal) ? 0 : yVal):E4}");
            }
            lblResult.Text = sb.ToString();
        }

        private double Derivative(double x)
        {
            double h = 1e-6;
            double f1 = SafeEval(x - h);
            double f2 = SafeEval(x + h);
            if (double.IsNaN(f1) || double.IsNaN(f2)) return double.NaN;
            return (f2 - f1) / (2 * h);
        }

        private double GoldenSectionLocal(double a, double b, double eps)
        {
            double x1 = b - PHI * (b - a);
            double x2 = a + PHI * (b - a);
            double f1 = SafeEval(x1);
            double f2 = SafeEval(x2);
            int iter = 0;
            while ((b - a) > eps && iter < 10000)
            {
                if (f1 < f2) { b = x2; x2 = x1; f2 = f1; x1 = b - PHI * (b - a); f1 = SafeEval(x1); }
                else { a = x1; x1 = x2; f1 = f2; x2 = a + PHI * (b - a); f2 = SafeEval(x2); }
                iter++;
            }
            return (a + b) / 2.0;
        }

        private double GoldenSectionLocalAbs(double a, double b, double eps)
        {
            double x1 = b - PHI * (b - a);
            double x2 = a + PHI * (b - a);
            double f1 = Math.Abs(SafeEval(x1));
            double f2 = Math.Abs(SafeEval(x2));
            int iter = 0;
            while ((b - a) > eps && iter < 10000)
            {
                if (f1 < f2) { b = x2; x2 = x1; f2 = f1; x1 = b - PHI * (b - a); f1 = Math.Abs(SafeEval(x1)); }
                else { a = x1; x1 = x2; f1 = f2; x2 = a + PHI * (b - a); f2 = Math.Abs(SafeEval(x2)); }
                iter++;
            }
            return (a + b) / 2.0;
        }

        private double SafeEval(double x)
        {
            try
            {
                double y = FormulaParser.Eval(_formula, x);
                if (double.IsNaN(y) || double.IsInfinity(y) || Math.Abs(y) > 1e12)
                    return double.NaN;
                return y;
            }
            catch { return double.NaN; }
        }

        private void MarkPoint(double x, double y, string name, Color color)
        {
            chart1.Series.Remove(chart1.Series.FindByName(name));
            var s = new Series(name)
            {
                ChartType = SeriesChartType.Point,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 10,
                MarkerColor = color,
                Color = color
            };
            chart1.Series.Add(s);
            s.Points.AddXY(x, y);
        }

        private List<double> Deduplicate(List<double> xs, double eps)
        {
            xs.Sort();
            var res = new List<double>();
            foreach (var x in xs)
            {
                if (res.Count == 0 || Math.Abs(x - res[res.Count - 1]) > eps)
                    res.Add(x);
            }
            return res;
        }

        private List<(double x, double y, bool isMin)> DeduplicateExtrema(
            List<(double x, double y, bool isMin)> src, double eps)
        {
            src.Sort((p, q) => p.x.CompareTo(q.x));
            var res = new List<(double x, double y, bool isMin)>();
            foreach (var e in src)
            {
                if (res.Count == 0 || Math.Abs(e.x - res[res.Count - 1].x) > eps)
                    res.Add(e);
            }
            return res;
        }

        private void menuClear_Click(object sender, EventArgs e)
        {
            txtA.Text = "";
            txtB.Text = "";
            txtE.Text = "";
            txtF.Text = "";
            lblResult.Text = "";
            lblStatus.Text = "";
            _formula = "";

            if (chart1.Series.Count > 0)
            {
                chart1.Series[0].Points.Clear();
                for (int i = chart1.Series.Count - 1; i >= 1; i--)
                    chart1.Series.RemoveAt(i);
            }
        }

        private bool TryParseDouble(string s, out double value)
        {
            s = s.Trim().Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out value);
        }
    }
}