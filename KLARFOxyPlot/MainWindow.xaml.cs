using Microsoft.Win32;
using ScottPlot;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KLARFOxyPlot
{
    public partial class MainWindow : Window
    {
        public GraphWindow grap;
        public DataGrabber dgrab;
        public DataTable dtb;
        public ConfigGrabber confg;
        ScottPlot.PlottableScatterHighlight sph;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            //We try to load in a config file, this should be where the .exe is
            try
            {
                confg = new ConfigGrabber("config.xml");
                LoadConfig.Content = "Config file loaded! \n(Click to load a different one)";
            }
            catch (FileNotFoundException e)
            {
                LoadConfig.Content = "No config file detected\n(Click here to load)";
            }
        }

        private void btnOpenFile_ClickPop(object sender, RoutedEventArgs e)
        {
            grap = new GraphWindow(dgrab, MainPlot, confg);
            grap.Show();
            //Changes our datagrid size for better viewing
            DGrid.Margin = new System.Windows.Thickness(0, 150, 0, 0);
        }

        private void btnOpenFile_ClickConfig(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                confg = new ConfigGrabber(openFileDialog.FileName);
                LoadConfig.Content = "Config file loaded! \n(Click to load a different one)";
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                dgrab = new DataGrabber(openFileDialog.FileName);

                PopOut.IsEnabled = true;

                //Resets our datagrid size since the user may want to look at the graph
                DGrid.Margin = new System.Windows.Thickness(0, 369, 0, 0);

                createScatterPlot();

                dtb = dgrab.df.ToDataTable();
                DGrid.DataContext = dtb;
            }
        }

        private void btnOpenFile_ClickClear(object sender, RoutedEventArgs e)
        {
            MainPlot.plt.Clear();
            MainPlot.Render();
            //Resets our datagrid size since the user may want to look at the graph
            DGrid.Margin = new System.Windows.Thickness(0, 369, 0, 0);
            DGrid.DataContext = null;
        }



        private void createScatterPlot()
        {
            double[] X = dgrab.df[dgrab.df.IndexOfColumn("CalcX")].ToDoubleArray();
            double[] Y = dgrab.df[dgrab.df.IndexOfColumn("CalcY")].ToDoubleArray();
            double[] col = dgrab.df[dgrab.df.IndexOfColumn("CLASSNUMBER")].ToDoubleArray();

            int pointsPerPolygon = 100;
            int polyR = 150000;
            double polyX = dgrab.xDieOri + dgrab.xCenter;
            double polyY = dgrab.yDieOri + dgrab.yCenter;

            int markSize = 10;

            double[] xs = Enumerable.Range(0, pointsPerPolygon).Select(x => polyR * Math.Cos(2.0 * Math.PI * x / pointsPerPolygon) + polyX).ToArray();
            double[] ys = Enumerable.Range(0, pointsPerPolygon).Select(x => polyR * Math.Sin(2.0 * Math.PI * x / pointsPerPolygon) + polyY).ToArray();
            MainPlot.plt.PlotPolygon(xs, ys, lineColor: System.Drawing.Color.Black, fillColor: System.Drawing.Color.DarkGray);

            sph = MainPlot.plt.PlotScatterHighlight(X, Y, markerSize: markSize, lineWidth: 0, markerShape: MarkerShape.filledSquare);

            //Create the rectangles
            for (double x = dgrab.xDieOri + dgrab.xCenter - 150000; x < dgrab.xDieOri + dgrab.xCenter + 150000; x = x + dgrab.xDiePit)
            {
                for (double y = dgrab.yDieOri + dgrab.yCenter - 150000; y < dgrab.yDieOri + dgrab.yCenter + 150000; y = y + dgrab.yDiePit)
                {
                    if (isWithin(dgrab.xCenter, dgrab.yCenter, x, y, dgrab.xDiePit, dgrab.yDiePit))
                    {
                        MainPlot.plt.PlotPolygon(
                        xs: new double[] { x, x, x + dgrab.xDiePit, x + dgrab.xDiePit },
                        ys: new double[] { y, y + dgrab.yDiePit, y + dgrab.yDiePit, y },
                        lineWidth: 2, fillAlpha: .8, fillColor: System.Drawing.Color.Transparent,
                        lineColor: System.Drawing.Color.Black);
                    }
                }
            }

            //Draw the color on the points
            for (int i = 0; i < X.Length; i++)
            {
                int r = 0;
                int b = 0;
                int g = 0;
                HsvToRgb((int)col[i] % 125, 60, 45, out r, out g, out b);

                if (confg != null)
                {
                    try
                    {
                        System.Drawing.Color n = System.Drawing.ColorTranslator.FromHtml(confg.ht[("" + (int)col[i])]);
                        MainPlot.plt.PlotPoint(X[i], Y[i], markerSize: markSize, markerShape: MarkerShape.filledSquare, color: n);
                    }
                    catch (System.Collections.Generic.KeyNotFoundException)
                    {
                        MainPlot.plt.PlotPoint(X[i], Y[i], markerSize: markSize, markerShape: MarkerShape.filledSquare, color: System.Drawing.Color.FromArgb(r, g, b));
                    }
                }
                else
                {
                    MainPlot.plt.PlotPoint(X[i], Y[i], markerSize: markSize, markerShape: MarkerShape.filledSquare, color: System.Drawing.Color.FromArgb(r, g, b));
                }
            }
        }

        private void plotMouseDoubleClick(object sender, MouseEventArgs e)
        {
            (double mouseX, double mouseY) = MainPlot.GetMouseCoordinates();

            sph.HighlightClear();
            var (trackerX, trackerY, index) = sph.HighlightPointNearest(mouseX, mouseY);

            for (int i = 0; i < dtb.Rows.Count; i++)
            {
                DataTable curTable = (DataTable)DGrid.DataContext;
                DataRow curRow = curTable.Rows[i];

                string cellContent = String.Join(", ", curRow.ItemArray);

                if (cellContent.Contains(trackerX.ToString()) && cellContent.Contains(trackerY.ToString()))
                {
                    DGrid.ScrollIntoView(DGrid.Items[i]);
                    DGrid.SelectedIndex = i;
                    DGrid.UpdateLayout();
                }
            }
            MainPlot.Render();
        }

        public string CurrentTrackerValue { get; set; }


        public bool isWithin(double centerX, double centerY, double rectX, double rectY, double diePitX, double diePitY)
        {
            bool dis1 = (Math.Pow(centerX - rectX, 2) + Math.Pow(centerY - rectY, 2)) < (150000.0 * 150000.0); //Is TRUE when we are in circle bounds
            bool dis2 = (Math.Pow(centerX - (rectX + diePitX), 2) + Math.Pow(centerY - rectY, 2)) < (150000.0 * 150000.0);
            bool dis3 = (Math.Pow(centerX - rectX, 2) + Math.Pow(centerY - (rectY + diePitY), 2)) < (150000.0 * 150000.0);
            bool dis4 = (Math.Pow(centerX - (rectX + diePitX), 2) + Math.Pow(centerY - (rectY + diePitY), 2)) < (150000.0 * 150000.0);

            return dis1 && dis2 && dis3 && dis4;
        }

        public void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

    }
}
