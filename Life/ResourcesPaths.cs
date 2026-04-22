using System;
using System.IO;

public static class ResourcesPaths {
  public static readonly string projectDirectory;
  public static readonly string configPath;
  public static readonly string boardPath;
  public static readonly string beehive;
  public static readonly string blinker;
  public static readonly string glider;
  public static readonly string gosper;
  public static readonly string pulsar;

  static ResourcesPaths() {
    projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
    configPath = Path.Combine(projectDirectory, "config.json");
    boardPath = Path.Combine(projectDirectory, "saved.txt");
    beehive = Path.Combine(projectDirectory, "colonies/beehive.txt");
    blinker = Path.Combine(projectDirectory, "colonies/blinker.txt");
    glider = Path.Combine(projectDirectory, "colonies/glider.txt");
    gosper = Path.Combine(projectDirectory, "colonies/gosper.txt");
    pulsar = Path.Combine(projectDirectory, "colonies/pulsar.txt");
  }
}