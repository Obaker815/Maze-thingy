namespace Maze_thingy
{
    internal class MazeGenerator
    {
        private readonly Maze _maze;
        private readonly Random _rng;

        // dx, dy, wall name from this cell, opposite wall name on neighbor
        private readonly (int dx, int dy, string wall, string oppositeWall)[] _directions =
        [
            (0, -1, "N", "S"),
            (0,  1, "S", "N"),
            (1,  0, "E", "W"),
            (-1, 0, "W", "E")
        ];

        // 0..1: how often to behave like DFS (take newest cell).
        // 1.0 = pure DFS (very long corridors), 0.0 = Prim-like (cavey).
        private readonly double _corridorBias = 0.8;

        public MazeGenerator(Maze maze, int? seed = null)
        {
            _maze = maze;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Generates a perfect maze using a biased Growing Tree algorithm,
        /// and yields after each carved passage so you can animate step-by-step.
        ///
        /// Each yielded value is (fromX, fromY, x, y) indicating the edge just carved.
        /// </summary>
        public IEnumerable<(int fromX, int fromY, int x, int y)> GenerateSteps()
        {
            int width = _maze.Width;
            int height = _maze.Height;

            bool[,] visited = new bool[width, height];

            // Active cells list for Growing Tree:
            // we usually pick the newest cell (DFS-like),
            // but sometimes pick a random one to add variation.
            var active = new List<(int x, int y)>();

            // Pick random start cell
            int startX = 0;
            int startY = 0;

            visited[startX, startY] = true;
            _maze.Start = (startX, startY);
            active.Add((startX, startY));

            // Growing Tree main loop
            while (active.Count > 0)
            {
                // Choose which active cell to grow from:
                // - with corridorBias -> take newest (DFS)
                // - otherwise -> random active cell (adds branching)
                int index;
                if (_rng.NextDouble() < _corridorBias)
                {
                    index = active.Count - 1; // newest cell (stack-like)
                }
                else
                {
                    index = _rng.Next(active.Count); // random cell
                }

                var (cx, cy) = active[index];

                // Collect all unvisited neighbors
                var neighbors = new List<(int nx, int ny, string wall, string oppositeWall)>();

                foreach (var (dx, dy, wall, oppositeWall) in _directions)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;

                    if (visited[nx, ny])
                        continue;

                    neighbors.Add((nx, ny, wall, oppositeWall));
                }

                if (neighbors.Count == 0)
                {
                    // Dead end: remove this cell from active list
                    active.RemoveAt(index);
                    continue;
                }

                // Pick a random unvisited neighbor
                var neighbor = neighbors[_rng.Next(neighbors.Count)];
                int x = neighbor.nx;
                int y = neighbor.ny;
                string wallv = neighbor.wall;
                string oppositeWallv = neighbor.oppositeWall;

                // Knock down walls between (cx, cy) and (x, y)
                _maze.Cells[cx, cy].walls[wallv] = false;
                _maze.Cells[x, y].walls[oppositeWallv] = false;

                visited[x, y] = true;
                active.Add((x, y));

                // This is one "step" of the animation
                yield return (cx, cy, x, y);
            }

            // After the maze is fully carved, pick End as farthest from Start
            int bestDist = -1;
            int endX = startX;
            int endY = startY;

            for (int ix = 0; ix < width; ix++)
            {
                for (int iy = 0; iy < height; iy++)
                {
                    int dist = Math.Abs(ix - startX) + Math.Abs(iy - startY);
                    if (dist > bestDist)
                    {
                        bestDist = dist;
                        endX = ix;
                        endY = iy;
                    }
                }
            }

            _maze.End = (endX, endY);
        }
    }
}
