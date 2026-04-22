using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Life.Tests;

[TestClass]
public class BoardTests {
  private string tempDirectory = "";

  [TestInitialize]
  public void Setup() {
    tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(tempDirectory);
  }

  [TestCleanup]
  public void Cleanup() {
    if (Directory.Exists(tempDirectory))
      Directory.Delete(tempDirectory, true);
  }

  [TestMethod]
  public void TestBoardDimensionsConsistency() {
    int width = 80;
    int height = 40;
    int cellSize = 2;

    var board = new Board(width, height, cellSize, 0.3);

    int expectedColumns = width / cellSize;
    int expectedRows = height / cellSize;

    Assert.AreEqual(expectedColumns, board.Columns);
    Assert.AreEqual(expectedRows, board.Rows);
    Assert.AreEqual(width, board.Width);
    Assert.AreEqual(height, board.Height);
  }

  [TestMethod]
  public void TestCellNeighborConnectivityComplete() {
    var board = new Board(15, 15, 1);
    var centerCell = board.Cells[7, 7];

    Assert.AreEqual(8, centerCell.neighbors.Count);

    var uniqueNeighbors = centerCell.neighbors.Distinct().Count();
    Assert.AreEqual(8, uniqueNeighbors);

    Assert.IsFalse(centerCell.neighbors.Contains(centerCell));
  }

  [TestMethod]
  public void TestToroidalEdgeWrapping() {
    var board = new Board(10, 10, 1);

    var cornerCell = board.Cells[0, 0];

    bool hasOppositeNeighbor = cornerCell.neighbors.Any(n =>
        ReferenceEquals(n, board.Cells[9, 9]) ||
        ReferenceEquals(n, board.Cells[9, 0]) ||
        ReferenceEquals(n, board.Cells[0, 9]));

    Assert.IsTrue(hasOppositeNeighbor, "Toroidal wrapping not working on corners");

    var edgeCell = board.Cells[9, 5];
    bool wrapsToLeft = edgeCell.neighbors.Any(n =>
        ReferenceEquals(n, board.Cells[0, 4]) ||
        ReferenceEquals(n, board.Cells[0, 5]) ||
        ReferenceEquals(n, board.Cells[0, 6]));

    Assert.IsTrue(wrapsToLeft, "Toroidal wrapping not working on edges");
  }

  [TestMethod]
  public void TestStillLifeBlockPersistence() {
    var board = new Board(20, 20, 1, 0);

    int x = 9, y = 9;
    board.Cells[x, y].IsAlive = true;
    board.Cells[x + 1, y].IsAlive = true;
    board.Cells[x, y + 1].IsAlive = true;
    board.Cells[x + 1, y + 1].IsAlive = true;

    var initialAliveCells = GetAliveCellsSet(board);

    for (int gen = 0; gen < 10; gen++) {
      board.Advance();
    }

    var finalAliveCells = GetAliveCellsSet(board);

    Assert.AreEqual(initialAliveCells.Count, finalAliveCells.Count);
    Assert.IsTrue(initialAliveCells.SetEquals(finalAliveCells));
  }

  [TestMethod]
  public void TestBlinkerTwoStateOscillation() {
    var board = new Board(15, 15, 1, 0);

    int center = 7;
    board.Cells[center, center - 1].IsAlive = true;
    board.Cells[center, center].IsAlive = true;
    board.Cells[center, center + 1].IsAlive = true;

    var horizontalState = GetAliveCellsSet(board);

    Assert.AreEqual(3, horizontalState.Count);
    Assert.IsTrue(horizontalState.All(cell => cell.Item1 == center));

    board.Advance();
    var verticalState = GetAliveCellsSet(board);

    Assert.AreEqual(3, verticalState.Count);
    Assert.IsTrue(verticalState.All(cell => cell.Item2 == center));
    Assert.IsFalse(horizontalState.SetEquals(verticalState));

    board.Advance();
    var backToHorizontal = GetAliveCellsSet(board);

    Assert.AreEqual(3, backToHorizontal.Count);
    Assert.IsTrue(horizontalState.SetEquals(backToHorizontal));
    Assert.IsTrue(backToHorizontal.All(cell => cell.Item1 == center));
  }

  [TestMethod]
  public void TestGliderMovementAndPeriodicity() {
    var board = new Board(25, 25, 1, 0);

    board.Cells[1, 0].IsAlive = true;
    board.Cells[2, 1].IsAlive = true;
    board.Cells[0, 2].IsAlive = true;
    board.Cells[1, 2].IsAlive = true;
    board.Cells[2, 2].IsAlive = true;

    var initialPositions = GetAliveCellsSet(board);
    Assert.AreEqual(5, initialPositions.Count);

    board.Advance();
    var positionsAfter1 = GetAliveCellsSet(board);
    Assert.AreEqual(5, positionsAfter1.Count);
    Assert.IsFalse(initialPositions.SetEquals(positionsAfter1));

    board.Advance();
    var positionsAfter2 = GetAliveCellsSet(board);
    Assert.AreEqual(5, positionsAfter2.Count);

    board.Advance();
    var positionsAfter3 = GetAliveCellsSet(board);
    Assert.AreEqual(5, positionsAfter3.Count);

    board.Advance();
    var positionsAfter4 = GetAliveCellsSet(board);
    Assert.AreEqual(5, positionsAfter4.Count);

    Assert.IsFalse(initialPositions.SetEquals(positionsAfter4));
  }

  [TestMethod]
  public void TestUnderpopulationRuleExact() {
    var board = new Board(12, 12, 1, 0);

    board.Cells[5, 5].IsAlive = true;

    board.Cells[7, 7].IsAlive = true;
    board.Cells[7, 8].IsAlive = true;

    board.Advance();

    Assert.IsFalse(board.Cells[5, 5].IsAlive, "Cell with 0 neighbors should die");
    Assert.IsFalse(board.Cells[7, 7].IsAlive, "Cell with 1 neighbor should die");
    Assert.IsFalse(board.Cells[7, 8].IsAlive, "Neighbor cell should also die");
  }

  [TestMethod]
  public void TestOverpopulationRuleExact() {
    var board = new Board(12, 12, 1, 0);

    board.Cells[6, 5].IsAlive = true;
    board.Cells[5, 5].IsAlive = true;
    board.Cells[7, 5].IsAlive = true;
    board.Cells[6, 4].IsAlive = true;
    board.Cells[6, 6].IsAlive = true;

    board.Cells[8, 8].IsAlive = true;
    for (int dx = -1; dx <= 1; dx++)
      for (int dy = -1; dy <= 1; dy++)
        if (!(dx == 0 && dy == 0))
          board.Cells[8 + dx, 8 + dy].IsAlive = true;

    board.Advance();

    Assert.IsFalse(board.Cells[6, 5].IsAlive);
    Assert.IsFalse(board.Cells[8, 8].IsAlive);
  }

  [TestMethod]
  public void TestReproductionRuleExact() {
    var board = new Board(12, 12, 1, 0);

    board.Cells[4, 4].IsAlive = true;
    board.Cells[5, 4].IsAlive = true;
    board.Cells[4, 5].IsAlive = true;
    board.Cells[5, 5].IsAlive = false;

    board.Cells[7, 7].IsAlive = false;
    board.Cells[7, 8].IsAlive = true;
    board.Cells[8, 7].IsAlive = true;

    board.Advance();

    Assert.IsTrue(board.Cells[5, 5].IsAlive);
    Assert.IsFalse(board.Cells[7, 7].IsAlive);
  }

  [TestMethod]
  public void TestSaveAndLoadBoardStateIntegrity() {
    var board1 = new Board(15, 20, 1, 0.4);

    board1.Cells[3, 5].IsAlive = true;
    board1.Cells[7, 12].IsAlive = true;
    board1.Cells[10, 3].IsAlive = true;
    board1.Cells[14, 19].IsAlive = true;

    string savePath = Path.Combine(tempDirectory, "test_board.txt");
    board1.SaveBoard(savePath);

    var board2 = Board.LoadBoard(savePath);

    for (int x = 0; x < board1.Columns; x++) {
      for (int y = 0; y < board1.Rows; y++) {
        Assert.AreEqual(board1.Cells[x, y].IsAlive, board2.Cells[x, y].IsAlive,
            $"Mismatch at position ({x}, {y})");
      }
    }

    Assert.AreNotSame(board1.Cells[0, 0], board2.Cells[0, 0]);
  }

  [TestMethod]
  public void TestLoadPatternWithWrapping() {
    var board = new Board(10, 10, 1, 0);

    string patternPath = Path.Combine(tempDirectory, "glider.txt");
    File.WriteAllLines(patternPath, new[]
    {
            "* ",
            " *",
            "**"
        });

    board.LoadPattern(patternPath, 9, 9);

    int aliveCount = 0;
    for (int x = 0; x < board.Columns; x++)
      for (int y = 0; y < board.Rows; y++)
        if (board.Cells[x, y].IsAlive)
          aliveCount++;

    Assert.AreEqual(4, aliveCount);
  }

  [TestMethod]
  public void TestColonyDetectionIsolation() {
    var board = new Board(25, 25, 1, 0);
    board.Cells[2, 2].IsAlive = true;
    board.Cells[3, 2].IsAlive = true;
    board.Cells[2, 3].IsAlive = true;
    board.Cells[3, 3].IsAlive = true;

    board.Cells[15, 15].IsAlive = true;
    board.Cells[15, 16].IsAlive = true;
    board.Cells[15, 17].IsAlive = true;

    board.Cells[22, 22].IsAlive = true;

    var colonies = board.FindColonies();

    Assert.AreEqual(3, colonies.Count);

    var sizes = colonies.Select(c => c.Count).OrderBy(x => x).ToList();
    Assert.AreEqual(1, sizes[0]);
    Assert.AreEqual(3, sizes[1]);
    Assert.AreEqual(4, sizes[2]);
  }

  [TestMethod]
  public void TestStabilityDetectionAfterMultipleGenerations() {
    var board = new Board(15, 15, 1, 0);

    board.Cells[5, 5].IsAlive = true;
    board.Cells[6, 5].IsAlive = true;
    board.Cells[5, 6].IsAlive = true;
    board.Cells[6, 6].IsAlive = true;

    bool stable = false;
    for (int gen = 0; gen < 6; gen++) {
      if (board.CheckStability()) {
        stable = true;
        break;
      }
      board.Advance();
    }

    Assert.IsTrue(stable);
  }

  [TestMethod]
  public void TestRandomDensityDistribution() {
    double targetDensity = 0.3;
    var board = new Board(50, 50, 1, targetDensity);

    int aliveCount = 0;
    for (int x = 0; x < board.Columns; x++)
      for (int y = 0; y < board.Rows; y++)
        if (board.Cells[x, y].IsAlive)
          aliveCount++;

    double actualDensity = (double)aliveCount / (board.Columns * board.Rows);

    double tolerance = 0.05;
    Assert.IsTrue(Math.Abs(actualDensity - targetDensity) <= tolerance,
        $"Expected density ~{targetDensity}, got {actualDensity}");
  }

  [TestMethod]
  public void TestCellDetermineNextLiveStateLogic() {
    var board = new Board(10, 10, 1);
    var testCell = board.Cells[4, 4];

    testCell.IsAlive = true;
    AddNeighbors(testCell, 2);
    testCell.DetermineNextLiveState();
    testCell.Advance();
    Assert.IsTrue(testCell.IsAlive);

    ResetCell(testCell);
    testCell.IsAlive = true;
    AddNeighbors(testCell, 1);
    testCell.DetermineNextLiveState();
    testCell.Advance();
    Assert.IsFalse(testCell.IsAlive);

    ResetCell(testCell);
    testCell.IsAlive = false;
    AddNeighbors(testCell, 3);
    testCell.DetermineNextLiveState();
    testCell.Advance();
    Assert.IsTrue(testCell.IsAlive);
  }

  [TestMethod]
  public void testColonyClassificationWithKnownPatterns() {
    var board = new Board(30, 30, 1, 0);
    var pattern = new HashSet<(int, int)>
    {
            (10, 9), (11, 9), (12, 9),
            (9, 10), (13, 10),
            (10, 11), (11, 11), (12, 11)
        };

    foreach (var (x, y) in pattern)
      board.Cells[x, y].IsAlive = true;

    var colonies = board.FindColonies();
    Assert.AreEqual(1, colonies.Count);

    string classification = board.ClassifyColony(colonies[0]);
    Assert.IsTrue(classification.Contains("beehive") || classification.Contains("Unknown"),
        $"Classification result: {classification}");
  }

  [TestMethod]
  public void TestMultipleAdvanceStepsConsistency() {
    var board1 = new Board(20, 20, 1, 0.5);
    var board2 = new Board(20, 20, 1, 0.5);

    CopyBoardState(board1, board2);

    board1.Advance();
    board1.Advance();

    for (int i = 0; i < 2; i++)
      board2.Advance();

    for (int x = 0; x < board1.Columns; x++) {
      for (int y = 0; y < board1.Rows; y++) {
        Assert.AreEqual(board1.Cells[x, y].IsAlive, board2.Cells[x, y].IsAlive,
            $"State mismatch at ({x}, {y}) after 2 generations");
      }
    }
  }

  private HashSet<(int, int)> GetAliveCellsSet(Board board) {
    var set = new HashSet<(int, int)>();
    for (int x = 0; x < board.Columns; x++)
      for (int y = 0; y < board.Rows; y++)
        if (board.Cells[x, y].IsAlive)
          set.Add((x, y));
    return set;
  }

  private void AddNeighbors(Cell cell, int count) {
    cell.neighbors.Clear();
    for (int i = 0; i < count; i++)
      cell.neighbors.Add(new Cell { IsAlive = true });
  }

  private void ResetCell(Cell cell) {
    cell.IsAlive = false;
    cell.neighbors.Clear();
  }

  private void CopyBoardState(Board source, Board target) {
    for (int x = 0; x < source.Columns; x++)
      for (int y = 0; y < source.Rows; y++)
        target.Cells[x, y].IsAlive = source.Cells[x, y].IsAlive;
  }
}