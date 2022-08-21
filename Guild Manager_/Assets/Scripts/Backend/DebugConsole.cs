using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;

public static class DebugConsole
{
    public static List<string> output = new List<string>();
    public static Action<string, string> ConsoleUpdated;

    public static void Log(string line)
    {
        output.Add(DateTime.Now + "-Log " + line);
        ConsoleUpdated.Invoke("Info", DateTime.Now + "-Info " + line);
    }

    public static void LogWarning(string line)
    {
        output.Add(DateTime.Now + "-Warning " + line);
        ConsoleUpdated.Invoke("Warning", DateTime.Now + "-Warning " + line);
    }

    public static void LogError(string line)
    {
        output.Add(DateTime.Now + "-Error " + line);
        ConsoleUpdated.Invoke("Error", DateTime.Now + "-Error " + line);
    }

    public static void Dump()
    {
        System.IO.Directory.CreateDirectory(Application.persistentDataPath + "/Logs");
        File.AppendAllLines(Application.persistentDataPath + "/Logs/" + DateTime.Now.ToShortDateString() + ".txt", output);
    }
}