using Cellular.Controller;
using Cellular.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Cellular.Controller.ConfigurationReader;

namespace Cellular
{
    public partial class Form1 : Form
    {
        const int SQUARE_SIZE = 14;
        public const int ROW_COUNT = 75;
        public const int COLLUMN_COUNT = 120;
        Graphics graphics;

        int contentWidth, contentHeight;
        List<Brush> colorBrushes;

        CellMapController mapController;
        public List<List<int>> CurrentMap => mapController.CurrentMap;
        public List<List<bool>> CellsChanged => mapController.CurrentChanges;

        // Current state:
        private bool paused = true;
        private System.Windows.Forms.Timer playTimer = new System.Windows.Forms.Timer();
        private int selectedColor = 1;
        const string PLAY_IMAGE = @"\..\..\images\Play.png";
        const string PAUSE_IMAGE = @"\..\..\images\Pause.png";
        const int SLOWEST_TEMPO = 460;  // Slowest play will refresh little faster than twice a second.
        const int TEMPO_STEP = 55;      // Each level of the stepbar decreases the interval by 55 ms.

        public Form1()
        {
            mapController = new CellMapController(ROW_COUNT, COLLUMN_COUNT);

            var configResult = ChangeConfig();
            if (configResult.result == LoadResult.Result.ERROR)
            {
                MessageBox.Show(configResult.message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            InitMap();

            InitializeComponent();
            leftWrapper.Width = this.Size.Width - 155;
            contentWidth = SQUARE_SIZE * COLLUMN_COUNT;
            contentHeight = SQUARE_SIZE * ROW_COUNT;
            (contentPanel.Width, contentPanel.Height) = (contentWidth, contentHeight);
            graphics = contentPanel.CreateGraphics();

            InitRadioButtons();
            playTimer.Enabled = false;
            playTimer.Tick += new EventHandler(OnTimerRefresh);

            if (configResult.result == LoadResult.Result.WARN)
            {
                MessageBox.Show(configResult.message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private LoadResult ChangeConfig()
        {
            var chooseFile = new OpenFileDialog();
            chooseFile.Title = "Choose your cellular automaton configuration";
            chooseFile.Filter = "Automata configuration files (*.aut) | *.aut";
            LoadResult loadResult = null;
            if (chooseFile.ShowDialog() == DialogResult.OK)
            {
                var reader = new StreamReader(chooseFile.FileName);
                var configReader = new ConfigurationReader(reader);
                mapController.colors = new List<CellColor>();
                return configReader.Load(ref mapController.colors);
            }
            else return LoadResult.ErrorResult("ERROR: Failed retreiving a file.");
        }

        private void InitMap()
        {
            colorBrushes = new List<Brush>();
            foreach (var color in mapController.colors)
            {
                colorBrushes.Add(new SolidBrush(color.Color));
            }
            mapController.Init();
        }

        private void InitRadioButtons()
        {
            radioPanel.Controls.Clear();
            for (int i = 0; i < mapController.colors.Count; ++i)
            {
                var color = mapController.colors[i];
                var rb = new RadioButton
                {
                    Text = color.Name,
                    BackColor = color.Color,
                    ForeColor = IsDark(color.Color) ? Color.White : Color.Black
                };
                int idx = i;
                rb.CheckedChanged += (Object sender, EventArgs e) => selectedColor = idx;
                radioPanel.Controls.Add(rb);
            }
            ((RadioButton)radioPanel.Controls[1]).Checked = true;
        }

        private bool IsDark(Color color) => (color.R + color.G + color.B) / 3 < 128;


        private void contentPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (!paused) return;
            int x = e.X / SQUARE_SIZE;
            int y = e.Y / SQUARE_SIZE;
            if (x > COLLUMN_COUNT || y > ROW_COUNT)
                return;
            CurrentMap[y][x] = selectedColor;

            graphics.FillRectangle(colorBrushes[selectedColor], new Rectangle(1 + x * SQUARE_SIZE, 1 + y * SQUARE_SIZE, SQUARE_SIZE - 1, SQUARE_SIZE - 1));
        }

        private void contentPanel_Paint(object sender, PaintEventArgs e)
        {
            int x = 1, y = 1;
            foreach (var row in CurrentMap)
            {
                foreach (var cellColor in row)
                {
                    graphics.FillRectangle(colorBrushes[cellColor], new RectangleF(x, y, SQUARE_SIZE - 1, SQUARE_SIZE - 1));
                    x += SQUARE_SIZE;
                }
                x = 1;
                y += SQUARE_SIZE;
            }

            Pen pen = new Pen(Color.FromArgb(73, 73, 73), 1);
            // horizontal lines:
            y = SQUARE_SIZE;
            for (int i = 1; i < ROW_COUNT; i++)
            {
                graphics.DrawLine(pen, 0, y, contentWidth, y);
                y += SQUARE_SIZE;
            }
            // vertical lines:
            x = SQUARE_SIZE;
            for (int i = 1; i < COLLUMN_COUNT; i++)
            {
                graphics.DrawLine(pen, x, 0, x, contentHeight);
                x += SQUARE_SIZE;
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            mapController.ComputeNext().Wait();
            mapController.SwitchMap();
            RedrawCells();
        }

        private void RedrawCells()
        {
            int x = 1, y = 1;
            for (int r = 0; r < ROW_COUNT; ++r)
            {
                for (int c = 0; c < COLLUMN_COUNT; ++c)
                {
                    if (CellsChanged[r][c])
                        graphics.FillRectangle(colorBrushes[CurrentMap[r][c]], x, y, SQUARE_SIZE - 1, SQUARE_SIZE - 1);
                    x += SQUARE_SIZE;
                }
                x = 1;
                y += SQUARE_SIZE;
            }
        }

        private void configButton_Click(object sender, EventArgs e)
        {
            var oldColors = mapController.colors;
            var configResult = ChangeConfig();
            if (configResult.result == LoadResult.Result.ERROR)
            {
                MessageBox.Show(configResult.message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                mapController.colors = oldColors;
                return;
            }

            InitMap();
            contentPanel.Refresh();
            InitRadioButtons();
            if (configResult.result == LoadResult.Result.WARN)
            {
                MessageBox.Show(configResult.message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            if (paused)
            {
                playTimer.Interval = SLOWEST_TEMPO - speedBar.Value * TEMPO_STEP;
                playButton.Image = Image.FromFile(Environment.CurrentDirectory + PAUSE_IMAGE);
                SetControlsVisible(false);
                playTimer.Enabled = true;
            } else
            {
                playTimer.Enabled = false;
                controllerTask = null;
                playButton.Image = Image.FromFile(Environment.CurrentDirectory + PLAY_IMAGE);
                SetControlsVisible(true);
            }
            paused = !paused;
        }

        private void SetControlsVisible(bool visible)
        {
            nextButton.Visible = visible;
            speedBar.Visible = visible;
            slowLabel.Visible = visible;
            fastLabel.Visible = visible;
            radioPanel.Visible = visible;
            configButton.Visible = visible;
        }

        private Task controllerTask;
        private void OnTimerRefresh(object sender, EventArgs e)
        {
            if (controllerTask != null)
            {
                controllerTask.Wait();
                mapController.SwitchMap();
                RedrawCells();
            }
            controllerTask = mapController.ComputeNext();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            leftWrapper.Width = this.Size.Width - 155;
            radioPanel.Height = rightWrapper.Height - 272;
            configButton.Location = new Point(30, rightWrapper.Height - 50);
        }
    }
}
