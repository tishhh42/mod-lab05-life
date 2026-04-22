using System;
using System.IO;

public static class ResourcesPaths {
  public static readonly string projectDirectory;
  public static readonly string coloniesPath;
  public static readonly string configPath;
  public static readonly string boardPath;
  public static readonly string beehive;
  public static readonly string blinker;
  public static readonly string glider;
  public static readonly string gosper;
  public static readonly string pulsar;
  public static readonly string eater;
  public static readonly string spaceship;
  public static readonly string train;

  public static readonly string d0_1;
  public static readonly string d0_2;
  public static readonly string d0_3;
  public static readonly string d0_4;
  public static readonly string d0_5;
  public static readonly string d0_6;
  public static readonly string d0_7;
  public static readonly string d0_8;
  public static readonly string d0_9;

  static ResourcesPaths() {
    projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
    coloniesPath = Path.Combine(projectDirectory, "colonies/");
    configPath = Path.Combine(projectDirectory, "config.json");
    boardPath = Path.Combine(projectDirectory, "saved.txt");
    beehive = coloniesPath + "beehive.txt";
    blinker = coloniesPath + "blinker.txt";
    glider = coloniesPath + "glider.txt";
    gosper = coloniesPath + "gosper.txt";
    pulsar = coloniesPath + "pulsar.txt";
    eater = coloniesPath + "eater.txt";
    spaceship = coloniesPath + "spaceship.txt";
    train = coloniesPath + "train.txt";

    d0_1 = Path.Combine(projectDirectory, "stabilityStatistic/density0_1.txt");
    d0_2 = Path.Combine(projectDirectory, "stabilityStatistic/density0_2.txt");
    d0_3 = Path.Combine(projectDirectory, "stabilityStatistic/density0_3.txt");
    d0_4 = Path.Combine(projectDirectory, "stabilityStatistic/density0_4.txt");
    d0_5 = Path.Combine(projectDirectory, "stabilityStatistic/density0_5.txt");
    d0_6 = Path.Combine(projectDirectory, "stabilityStatistic/density0_6.txt");
    d0_7 = Path.Combine(projectDirectory, "stabilityStatistic/density0_7.txt");
    d0_8 = Path.Combine(projectDirectory, "stabilityStatistic/density0_8.txt");
    d0_9 = Path.Combine(projectDirectory, "stabilityStatistic/density0_9.txt");
  }
}