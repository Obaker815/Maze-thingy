namespace Maze_thingy
{
    internal struct Cell
    {
        public Dictionary<string, bool> walls;
        public Cell()
        {
            walls = new()
            {
                { "N", true },
                { "S", true },
                { "E", true },
                { "W", true } 
            };
        }
    }

    internal class Maze
    {
        public int Width { get; }
        public int Height { get; }

        public Cell[,] Cells { get; }

        public (int X, int Y) Start { get; set; }
        public (int X, int Y) End { get; set; }

        public Maze(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new Cell[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Cells[i, j] = new();
                }
            }
        }
    }
}
