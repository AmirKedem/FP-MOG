using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public class BuildUtils : EditorWindow
{
    public enum GameLoopMode
    {
        Server,
        Client,
        Undefined,
    }

    public enum EditorRole
    {
        Unused,
        Client,
        Server,
    }

    public static void StopAll()
    {
        KillAllProcesses();
        EditorApplication.isPlaying = false;
    }

    public static void KillAllProcesses()
    {
        var buildExe = GetBuildExe(GameLoopMode.Server);

        var processName = Path.GetFileNameWithoutExtension(buildExe);
        var processes = System.Diagnostics.Process.GetProcesses();
        foreach (var process in processes)
        {
            if (process.HasExited)
                continue;

            try
            {
                if (process.ProcessName != null && process.ProcessName == processName)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {

            }
        }

        buildExe = GetBuildExe(GameLoopMode.Client);

        processName = Path.GetFileNameWithoutExtension(buildExe);
        processes = System.Diagnostics.Process.GetProcesses();
        foreach (var process in processes)
        {
            if (process.HasExited)
                continue;

            try
            {
                if (process.ProcessName != null && process.ProcessName == processName)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {

            }
        }
    }

    public static string GetBuildPath()
    {
        return "Builds";
    }

    public static string GetBuildExe(GameLoopMode mode)
    {
        if (mode == GameLoopMode.Server)
            return "Server/Server.exe";
        else if (mode == GameLoopMode.Client)
            return "Client/Client.exe";
        else
            return "";
    }

    public static void RunBuild(GameLoopMode mode)
    {
        var buildPath = GetBuildPath();
        var buildExe = GetBuildExe(mode);
        Debug.Log("Starting " + buildPath + "/" + buildExe);
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = Application.dataPath + "/../" + buildPath + "/" + buildExe;
        process.StartInfo.WorkingDirectory = buildPath;
        process.Start();
    }

}
