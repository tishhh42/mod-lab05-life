using System;
using System.Collections.Generic;
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
    private bool IsAliveNext;
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
  }
  public record GameConfig(int Width = 50, int Height = 20, int CellSize = 1, double LiveDensity = 0.5, int UpdateDelay = 1000);
  class Program {
    static Board board;
    static GameConfig config;
    static int delay;
    static int generation = 1;
    static private void Reset(string boardFile = "") {
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
    static void LoadConfig(string configFile) {
      try {
        string json = File.ReadAllText(configFile);
        config = JsonSerializer.Deserialize<GameConfig>(json);
        delay = config.UpdateDelay;
      }
      catch {
        config = new GameConfig();
        delay = config.UpdateDelay;
      }
    }
    static int keyAction(string boardFile) {
      if (Console.KeyAvailable) {
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.S) {
          board.SaveBoard(boardFile);
          Console.WriteLine("\nState saved to board.txt");
        }
        else if (key == ConsoleKey.L) {
          board = Board.LoadBoard(boardFile);
          Console.WriteLine("\nState loaded from board.txt");
        }
        else if (key == ConsoleKey.Enter) {
          return 1;
        }
      }
      return 0;
    }
    static void Main(string[] args) {
      LoadConfig(ResourcesPaths.configPath);
      Reset();

      board.LoadPattern(ResourcesPaths.beehive);
      while (true) {
        if (keyAction(ResourcesPaths.boardPath) == 1)
          break;

        Console.Clear();
        Render();
        board.Advance();
        Thread.Sleep(delay);
      }
    }
  }
}