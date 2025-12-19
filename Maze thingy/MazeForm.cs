using System.Diagnostics;

namespace Maze_thingy
{
    public partial class MazeForm : Form
    {
        private enum MazeState
        {
            Generating,
            Searching,
            DrawingSolution,
            ShowingSolution
        }

        private MazeState _state;

        private Maze _maze;
        private MazeGenerator _generator;
        private IEnumerator<(int fromX, int fromY, int x, int y)>? _genSteps;
        private MazeSolver? _solver;
        private IEnumerator<(int X, int Y)>? _searchSteps;

        private readonly int _cellSize  = 20;
        private readonly int _padding   = 10;

        private readonly int _width     = 40;
        private readonly int _height    = 40;

        // Generation animation
        private (int fromX, int fromY, int x, int y)? _lastGenStep;

        // Solving/search animation
        private readonly HashSet<(int X, int Y)> _visitedCells = [];

        // Solution path animation
        private List<(int X, int Y)> _solutionPath = [];
        private int _solutionIndex = 0;

        // How long to show full solution before resetting
        private int _showSolutionTicks = 0;
        private readonly int _maxShowSolutionTicks = 120; // timer ticks

        public MazeForm()
        {
            DoubleBuffered = true;
            Text = "Maze Generator & Solver";

            StartPosition = FormStartPosition.CenterScreen;

            // Size the window so the maze fits
            ClientSize = new Size(
                _padding * 2 + _width * _cellSize + 1,
                _padding * 2 + _height * _cellSize + 1
            );

            _maze = null!;
            _generator = null!;

        }

        private void ResetMaze()
        {
            _maze = new Maze(_width, _height);

            // Initialize all cells so their walls dictionary is created
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _maze.Cells[x, y] = new Cell();
                }
            }

            _generator = new MazeGenerator(_maze);
            _genSteps = _generator.GenerateSteps().GetEnumerator();

            _lastGenStep = null;

            _solver = null;
            _searchSteps = null;
            _visitedCells.Clear();

            _solutionPath = [];
            _solutionIndex = 0;
            _showSolutionTicks = 0;

            _state = MazeState.Generating;

            Invalidate();
        }

        private void Mainlooprun()
        {
            switch (_state)
            {
                case MazeState.Generating:
                    StepGeneration();
                    break;

                case MazeState.Searching:
                    StepSearching();
                    break;

                case MazeState.DrawingSolution:
                    StepDrawingSolution();
                    break;

                case MazeState.ShowingSolution:
                    StepShowingSolution();
                    break;
            }

            Invalidate(); // trigger redraw
        }

        private void StepGeneration()
        {
            if (_genSteps == null)
                return;

            if (!_genSteps.MoveNext())
            {
                // Maze generation finished
                _lastGenStep = null;

                // Prepare solver for animated search
                _solver = new MazeSolver(_maze);
                _searchSteps = _solver.SearchSteps().GetEnumerator();
                _visitedCells.Clear();

                _state = MazeState.Searching;
                return;
            }

            _lastGenStep = _genSteps.Current;
        }

        private void StepSearching()
        {
            if (_searchSteps == null || _solver == null)
                return;

            if (!_searchSteps.MoveNext())
            {
                // Search finished, build final path
                _solutionPath = _solver.BuildPath();
                _solutionIndex = _solutionPath.Count > 0 ? 1 : 0;

                _state = MazeState.DrawingSolution;
                return;
            }

            var current = _searchSteps.Current;
            _visitedCells.Add(current);
        }

        private void StepDrawingSolution()
        {
            if (_solutionPath == null || _solutionPath.Count == 0)
            {
                // No path? Just move on to showing/reset
                _state = MazeState.ShowingSolution;
                return;
            }

            _solutionIndex++;

            if (_solutionIndex >= _solutionPath.Count)
            {
                _solutionIndex = _solutionPath.Count;
                _state = MazeState.ShowingSolution;
                _showSolutionTicks = 0;
            }
        }

        private void StepShowingSolution()
        {
            _showSolutionTicks++;

            if (_showSolutionTicks >= _maxShowSolutionTicks)
            {
                ResetMaze();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_maze is null)
            {
                MazeForm_Shown(new(), e);
            }
            base.OnPaint(e);

            var g = e.Graphics;

            using var wallPen = new Pen(Color.Black, (_cellSize / 10));
            using var genHighlightPen = new Pen(Color.Red, 2);
            using var visitedBrush = new SolidBrush(Color.FromArgb(80, Color.DeepSkyBlue));
            using var solutionPen = new Pen(Color.Blue, 3);
            using var startBrush = new SolidBrush(Color.LimeGreen);
            using var endBrush = new SolidBrush(Color.Gold);

            int width = _maze!.Width;
            int height = _maze!.Height;

            // Draw visited cells (search animation) as a light overlay
            if (_state == MazeState.Searching || _state == MazeState.DrawingSolution || _state == MazeState.ShowingSolution)
            {
                foreach (var (vx, vy) in _visitedCells)
                {
                    int vx0 = _padding + vx * _cellSize + 1;
                    int vy0 = _padding + vy * _cellSize + 1;
                    int size = _cellSize - 2;
                    g.FillRectangle(visitedBrush, vx0, vy0, size, size);
                }
            }

            // Draw walls for each cell
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = _maze.Cells[x, y];

                    int x0 = _padding + x * _cellSize;
                    int y0 = _padding + y * _cellSize;
                    int x1 = x0 + _cellSize;
                    int y1 = y0 + _cellSize;

                    if (cell.walls["N"])
                        g.DrawLine(wallPen, x0, y0, x1, y0);

                    if (cell.walls["S"])
                        g.DrawLine(wallPen, x0, y1, x1, y1);

                    if (cell.walls["W"])
                        g.DrawLine(wallPen, x0, y0, x0, y1);

                    if (cell.walls["E"])
                        g.DrawLine(wallPen, x1, y0, x1, y1);
                }
            }

            // Highlight the most recent carved passage (generation animation)
            if (_state == MazeState.Generating && _lastGenStep.HasValue)
            {
                var (fromX, fromY, x, y) = _lastGenStep.Value;

                float fx = _padding + fromX * _cellSize + _cellSize / 2f;
                float fy = _padding + fromY * _cellSize + _cellSize / 2f;

                float tx = _padding + x * _cellSize + _cellSize / 2f;
                float ty = _padding + y * _cellSize + _cellSize / 2f;

                g.DrawLine(genHighlightPen, fx, fy, tx, ty);
            }

            // Draw the solution path (during DrawingSolution + ShowingSolution)
            if ((_state == MazeState.DrawingSolution || _state == MazeState.ShowingSolution) &&
                _solutionPath != null && _solutionPath.Count > 1 && _solutionIndex > 1)
            {
                int maxIndex = Math.Min(_solutionIndex, _solutionPath.Count);

                for (int i = 0; i < maxIndex - 1; i++)
                {
                    var (X, Y) = _solutionPath[i];
                    var b = _solutionPath[i + 1];

                    float ax = _padding + X * _cellSize + _cellSize / 2f;
                    float ay = _padding + Y * _cellSize + _cellSize / 2f;

                    float bx = _padding + b.X * _cellSize + _cellSize / 2f;
                    float by = _padding + b.Y * _cellSize + _cellSize / 2f;

                    g.DrawLine(solutionPen, ax, ay, bx, by);
                }
            }

            // Draw Start cell
            var (startX, startY) = _maze.Start;
            {
                int sx0 = _padding + startX * _cellSize + 1;
                int sy0 = _padding + startY * _cellSize + 1;
                int size = _cellSize - 2;
                g.FillRectangle(startBrush, sx0, sy0, size, size);
            }

            // Draw End cell
            var end = _maze.End;
            {
                int ex0 = _padding + end.X * _cellSize + 1;
                int ey0 = _padding + end.Y * _cellSize + 1;
                int size = _cellSize - 2;
                g.FillRectangle(endBrush, ex0, ey0, size, size);
            }
        }

        private void MazeForm_Shown(object sender, EventArgs e)
        {
            _maze = null!; // will be initialized in ResetMaz
            _generator = null!;

            // Initialize the first maze
            ResetMaze();

            Task.Run(() =>
            {
                Stopwatch elapsedsw = Stopwatch.StartNew();
                while (true)
                {
                    elapsedsw.Restart();
                    try
                    {
                        this.Invoke(Mainlooprun);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    while (elapsedsw.Elapsed.TotalMilliseconds < 1)
                        continue;
                }
            });
        }

        private void MazeForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
        }
    }
}
