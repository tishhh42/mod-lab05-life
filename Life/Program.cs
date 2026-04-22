using ScottPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace cli_life {
  public class Cell {
    public bool IsAlive;
    public readonly List<Cell> neighbors = new List<Cell>();
    public bool IsAliveNext;
    public void DetermineNextLiveState() {
      int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
      if (IsAlive)
        IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
      else
        IsAliveNext = liveNeighbors == 3;
    }
    public void Advance() {
      IsAlive = IsAliveNext;
    }
  }
  public class Board {
    public readonly Cell[,] Cells;
    public readonly int CellSize;
    private const int StabilityThreshold = 5;
    private Queue<int> history = new Queue<int>();

    public int Columns { get { return Cells.GetLength(0); } }
    public int Rows { get { return Cells.GetLength(1); } }
    public int Width { get { return Columns * CellSize; } }
    public int Height { get { return Rows * CellSize; } }

    public Board(int width, int height, int cellSize, double liveDensity = .1) {
      CellSize = cellSize;

      Cells = new Cell[width / cellSize, height / cellSize];
      for (int x = 0; x < Columns; x++)
        for (int y = 0; y < Rows; y++)
          Cells[x, y] = new Cell();

      ConnectNeighbors();
      Randomize(liveDensity);
    }

    readonly Random rand = new Random();
    public void Randomize(double liveDensity) {
      foreach (var cell in Cells)
        cell.IsAlive = rand.NextDouble() < liveDensity;
    }

    public void Advance() {
      foreach (var cell in Cells)
        cell.DetermineNextLiveState();
      foreach (var cell in Cells)
        cell.Advance();
    }
    private void ConnectNeighbors() {
      for (int x = 0; x < Columns; x++) {
        for (int y = 0; y < Rows; y++) {
          int xL = (x > 0) ? x - 1 : Columns - 1;
          int xR = (x < Columns - 1) ? x + 1 : 0;

          int yT = (y > 0) ? y - 1 : Rows - 1;
          int yB = (y < Rows - 1) ? y + 1 : 0;

          Cells[x, y].neighbors.Add(Cells[xL, yT]);
          Cells[x, y].neighbors.Add(Cells[x, yT]);
          Cells[x, y].neighbors.Add(Cells[xR, yT]);
          Cells[x, y].neighbors.Add(Cells[xL, y]);
          Cells[x, y].neighbors.Add(Cells[xR, y]);
          Cells[x, y].neighbors.Add(Cells[xL, yB]);
          Cells[x, y].neighbors.Add(Cells[x, yB]);
          Cells[x, y].neighbors.Add(Cells[xR, yB]);
        }
      }
    }
    public static Board LoadBoard(string boardFile) {
      using (StreamReader reader = new StreamReader(boardFile)) {
        var dimensions = reader.ReadLine().Split(' ');
        int cols = int.Parse(dimensions[0]);
        int rows = int.Parse(dimensions[1]);
        int cellSize = int.Parse(dimensions[2]);
        Board board = new Board(cols * cellSize, rows * cellSize, cellSize);

        for (int y = 0; y < rows; y++) {
          string line = reader.ReadLine();
          for (int x = 0; x < cols; x++) {
            board.Cells[x, y].IsAlive = line[x] == '*';
          }
        }

        return board;
      }
    }
    public void SaveBoard(string boardFile) {
      using (StreamWriter writer = new StreamWriter(boardFile)) {
        writer.WriteLine($"{Columns} {Rows} {CellSize}");
        for (int y = 0; y < Rows; y++) {
          for (int x = 0; x < Columns; x++) {
            writer.Write(Cells[x, y].IsAlive ? '*' : ' ');
          }
          writer.WriteLine();
        }
      }
    }
    public void LoadPattern(string patternFile, int offsetX = 0, int offsetY = 0) {
      string[] lines = File.ReadAllLines(patternFile);
      for (int y = 0; y < lines.Length; y++) {
        for (int x = 0; x < lines[y].Length; x++) {
          int targetX = (x + offsetX) % Columns;
          int targetY = (y + offsetY) % Rows;
          Cells[targetX, targetY].IsAlive = lines[y][x] == '*';
        }
      }
    }
    public bool CheckStability() {
      int aliveCount = 0;
      for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Columns; x++)
          if (Cells[x, y].IsAlive)
            aliveCount++;

      history.Enqueue(aliveCount);
      if (history.Count > StabilityThreshold)
        history.Dequeue();

      return history.Distinct().Count() == 1 && history.Count == StabilityThreshold;
    }
    public void SaveValue(int generation, string statFile) {
      if (File.Exists(statFile)) {
        List<string> lines = File.ReadAllLines(statFile).ToList();
        string newRecord = $"{DateTime.Now.ToString()}: {generation}";
        if (lines.Count > 0)
          lines[lines.Count - 1] = newRecord;
        else
          lines.Add(newRecord);
        int average = 0;
        foreach (string line in lines) {
          average += int.Parse(line.Split(": ")[1]);
        }
        average /= lines.Count;
        lines.Add($"Average genereations count: {average}");
        File.WriteAllLines(statFile, lines);
      }
      else {
        throw new Exception($"File {statFile} not found");
      }
    }
    public List<HashSet<(int, int)>> FindColonies() {
      var colonies = new List<HashSet<(int, int)>>();
      var visited = new bool[Columns, Rows];

      for (int y = 0; y < Rows; y++) {
        for (int x = 0; x < Columns; x++) {
          if (Cells[x, y].IsAlive && !visited[x, y]) {
            var cluster = new HashSet<(int, int)>();
            ExploreColony(x, y, visited, cluster);
            colonies.Add(cluster);
          }
        }
      }

      return colonies;
    }
    private void ExploreColony(int x, int y, bool[,] visited, HashSet<(int, int)> colony) {
      var queue = new Queue<(int, int)>();
      queue.Enqueue((x, y));
      visited[x, y] = true;

      while (queue.Count > 0) {
        var (cx, cy) = queue.Dequeue();
        colony.Add((cx, cy));

        for (int dy = -1; dy <= 1; dy++) {
          for (int dx = -1; dx <= 1; dx++) {
            if (dx == 0 && dy == 0)
              continue;

            int nx = (cx + dx + Columns) % Columns;
            int ny = (cy + dy + Rows) % Rows;

            if (Cells[nx, ny].IsAlive && !visited[nx, ny]) {
              visited[nx, ny] = true;
              queue.Enqueue((nx, ny));
            }
          }
        }
      }
    }
    public string ClassifyColony(HashSet<(int x, int y)> colony) {
      var normalized = NormalizeColony(colony);
      foreach (var (x, y) in normalized) {
        Console.Write(x);
        Console.Write(" ");
        Console.Write(y);
        Console.Write(") (");
      }
      Console.WriteLine();


      var colonies = LoadColonies();

      foreach (var (name, loadedColony) in colonies) {
        if (name == "beehive") {
          foreach (var (x, y) in loadedColony) {
            Console.Write(x);
            Console.Write(" ");
            Console.Write(y);
            Console.Write(") (");
          }
          Console.WriteLine();
        }
        if (AreColoniesEqual(normalized, loadedColony)) {
          return name;
        }
      }

      return $"Unknown ({colony.Count} cells)";
    }
    private HashSet<(int x, int y)> NormalizeColony(HashSet<(int x, int y)> colony) {
      if (colony.Count == 0) return [];

      var bestNormalized = colony;
      int bestSpread = int.MaxValue;

      for (int shiftX = 0; shiftX < Width; shiftX++) {
        for (int shiftY = 0; shiftY < Height; shiftY++) {
          var shifted = colony.Select(p => (
              (p.x + shiftX) % Width,
              (p.y + shiftY) % Height
          )).ToHashSet();

          int minX = shifted.Min(p => p.Item1);
          int minY = shifted.Min(p => p.Item2);
          int maxX = shifted.Max(p => p.Item1);
          int maxY = shifted.Max(p => p.Item2);

          int spread = (maxX - minX) + (maxY - minY);

          var normalized = shifted.Select(p => (p.Item1 - minX, p.Item2 - minY)).ToHashSet();

          if (spread < bestSpread ||
              (spread == bestSpread && normalized.GetHashCode() > bestNormalized.GetHashCode())) {
            bestSpread = spread;
            bestNormalized = normalized;
          }
        }
      }

      return bestNormalized;
    }
    private Dictionary<string, HashSet<(int x, int y)>> LoadColonies() {
      var colonies = new Dictionary<string, HashSet<(int x, int y)>>();

      try {
        foreach (var file in Directory.GetFiles(ResourcesPaths.coloniesPath, "*.txt")) {
          var colony = new HashSet<(int x, int y)>();
          string[] lines = File.ReadAllLines(file);

          for (int y = 0; y < lines.Length; y++) {
            for (int x = 0; x < lines[y].Length; x++) {
              if (lines[y][x] == '*') {
                colony.Add((x, y));
              }
            }
          }

          colonies.Add(Path.GetFileNameWithoutExtension(file), colony);
        }
      }
      catch (Exception ex) {
        Console.WriteLine(ex.ToString());
      }

      return colonies;
    }
    private bool AreColoniesEqual(
        HashSet<(int x, int y)> colony1,
        HashSet<(int x, int y)> colony2) {
      if (colony1.Count != colony2.Count)
        return false;

      for (int rotation = 0; rotation < 4; rotation++) {
        var rotated = RotateColony(colony1, rotation);
        if (rotated.SetEquals(colony2))
          return true;
      }

      return false;
    }
    private HashSet<(int x, int y)> RotateColony(
        HashSet<(int x, int y)> colony,
        int rotations) {
      var result = new HashSet<(int x, int y)>();
      int size = colony.Max(p => Math.Max(p.x, p.y)) + 1;

      foreach (var (x, y) in colony) {
        var (rx, ry) = (x, y);

        for (int i = 0; i < rotations; i++) {
          (rx, ry) = (ry, size - 1 - rx);
        }

        result.Add((rx, ry));
      }

      return result;
    }
  }
  public record GameConfig(int Width = 50, int Height = 20, int CellSize = 1, double LiveDensity = 0.5, int UpdateDelay = 1000);
  class Program {
    static Board board;
    static GameConfig config;
    static int delay;
    static int generation = 1;
    static int stableGeneration = 1;
    static private void Reset(string boardFile = "") {
      generation = 1;
      stableGeneration = 1;
      if (string.IsNullOrEmpty(boardFile)) {
        board = new Board(
          width: config.Width,
          height: config.Height,
          cellSize: config.CellSize,
          liveDensity: config.LiveDensity);
      }
      else {
        board = Board.LoadBoard(boardFile);
      }
    }
    static void Render() {
      for (int row = 0; row < board.Rows; row++) {
        for (int col = 0; col < board.Columns; col++) {
          var cell = board.Cells[col, row];
          if (cell.IsAlive) {
            Console.Write('*');
          }
          else {
            Console.Write(' ');
          }
        }
        Console.Write('\n');
      }
      Console.Write($"Generation: {generation}");
      generation++;
    }
    static void LoadConfig() {
      try {
        string json = File.ReadAllText(ResourcesPaths.configPath);
        config = JsonSerializer.Deserialize<GameConfig>(json);
        delay = config.UpdateDelay;
      }
      catch {
        config = new GameConfig();
        delay = config.UpdateDelay;
      }
    }
    static int keyAction() {
      if (Console.KeyAvailable) {
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.S) {
          board.SaveBoard(ResourcesPaths.boardPath);
          Console.WriteLine("\nBoard saved to board.txt");
        }
        else if (key == ConsoleKey.L) {
          board = Board.LoadBoard(ResourcesPaths.boardPath);
          Console.WriteLine("\nBoard loaded from board.txt");
        }
        else if (key == ConsoleKey.Enter) {
          return 1;
        }
      }
      return 0;
    }
    static void PrintColoniesInfo() {
      var colonies = board.FindColonies();
      Console.WriteLine($"\ncolonies count: {colonies.Count}");
      foreach (var colony in colonies.OrderBy(c => -c.Count)) {
        Console.WriteLine($"{board.ClassifyColony(colony)} (size: {colony.Count})");
      }
    }
    static void BuildGraph() {
      string filePath = "C:\\Users\\Dora\\source\\repos\\mod-lab05-life\\Data\\data.txt";
      var lines = File.ReadAllLines(filePath);

      List<double> xs = new List<double>();
      List<double> ys = new List<double>();

      foreach (var line in lines) {
        if (string.IsNullOrWhiteSpace(line)) continue;

        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) {
          if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
              double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y)) {
            xs.Add(x);
            ys.Add(y);
          }
        }
      }

      var plt = new ScottPlot.Plot();

      var scatter = plt.Add.Scatter(xs.ToArray(), ys.ToArray());
      scatter.LegendText = "Результаты эксперимента";
      scatter.MarkerSize = 10;

      plt.XLabel("Плотность");
      plt.YLabel("Среднее число поколений до стабильного состояния");
      plt.Title("График зависимости");

      plt.SavePng("C:\\Users\\Dora\\source\\repos\\mod-lab05-life\\Data\\plot.png", 800, 600);
    }
    static void Main(string[] args) {
      for (int i = 0; i < 15; i++) {
        LoadConfig();
        Reset();

        //board.LoadPattern(ResourcesPaths.train);
        while (true) {
          if (keyAction() == 1)
            break;

          Console.Clear();
          Render();

          if (board.CheckStability()) {
            Console.WriteLine($"\nStable generation: {stableGeneration}");
            board.SaveValue(stableGeneration, ResourcesPaths.d0_9);
            PrintColoniesInfo();
            break;
          }
          else {
            stableGeneration++;
          }

          board.Advance();
          Thread.Sleep(delay);
        }
      }
      //BuildGraph();
    }
  }
}