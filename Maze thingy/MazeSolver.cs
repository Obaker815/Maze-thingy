using System;
using System.Collections.Generic;

namespace Maze_thingy
{
    internal class MazeSolver
    {
        private readonly Maze _maze;
        private readonly int _width;
        private readonly int _height;
        private readonly (int X, int Y) _start;
        private readonly (int X, int Y) _end;

        // 4-way movement: dx, dy, this cell's wall, neighbor's opposite wall
        private readonly (int dx, int dy, string wall, string oppositeWall)[] _directions =
        [
            (0, -1, "N", "S"),
            (0,  1, "S", "N"),
            (1,  0, "E", "W"),
            (-1, 0, "W", "E")
        ];

        private readonly bool[,] _visited;
        private readonly (int X, int Y)?[,] _prev;

        public MazeSolver(Maze maze)
        {
            _maze = maze;
            _width = maze.Width;
            _height = maze.Height;
            _start = maze.Start;
            _end = maze.End;

            _visited = new bool[_width, _height];
            _prev = new (int X, int Y)?[_width, _height];
        }

        // A* heuristic: Manhattan distance on a 4-way grid
        private int Heuristic(int x, int y)
        {
            return Math.Abs(x - _end.X) + Math.Abs(y - _end.Y);
        }

        // Each yielded value is the next cell expanded by A* during search.
        public IEnumerable<(int X, int Y)> SearchSteps()
        {
            // Clear visited/prev in case this solver is ever reused
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _visited[x, y] = false;
                    _prev[x, y] = null;
                }
            }

            // gScore = best known distance from start to this cell
            var gScore = new int[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    gScore[x, y] = int.MaxValue;
                }
            }

            var openSet = new PriorityQueue<(int X, int Y), int>();

            gScore[_start.X, _start.Y] = 0;
            int startH = Heuristic(_start.X, _start.Y);
            openSet.Enqueue(_start, startH); // f = g(=0) + h

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                int cx = current.X;
                int cy = current.Y;

                // Skip if we've already processed a better path to this cell
                if (_visited[cx, cy])
                    continue;

                _visited[cx, cy] = true;

                // This is the "expansion" step we want to animate
                yield return current;

                if (current.Equals(_end))
                    yield break;

                int currentG = gScore[cx, cy];
                foreach (var (dx, dy, wall, _) in _directions)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (nx < 0 || ny < 0 || nx >= _width || ny >= _height)
                        continue;

                    if (_visited[nx, ny])
                        continue;

                    // You can move in direction d only if there is NO wall
                    if (_maze.Cells[cx, cy].walls[wall])
                        continue;

                    // Uniform cost: each move costs 1
                    int tentativeG = currentG + 1;

                    if (tentativeG >= gScore[nx, ny])
                        continue; // not a better path

                    gScore[nx, ny] = tentativeG;
                    _prev[nx, ny] = current;

                    int fScore = tentativeG + Heuristic(nx, ny);
                    openSet.Enqueue((nx, ny), fScore);
                }
            }
        }

        public List<(int X, int Y)> BuildPath()
        {
            var path = new List<(int X, int Y)>();

            // If end was never visited, there is no path
            if (!_visited[_end.X, _end.Y])
                return path;

            var cur = _end;

            while (true)
            {
                path.Add(cur);
                if (cur.Equals(_start))
                    break;

                var p = _prev[cur.X, cur.Y];
                if (!p.HasValue)
                    break; // safety, shouldn't happen if _visited[end] is true

                cur = p.Value;
            }

            path.Reverse();
            return path;
        }
    }
}
