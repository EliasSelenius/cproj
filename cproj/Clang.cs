using System;
using System.Diagnostics;

static class Clang {

    static readonly string includeArg = " -Iinclude ";


    public static Process link(string args) {
        var p = new Process();
        p.StartInfo = new ProcessStartInfo("clang", args) {
            UseShellExecute = false,
        };

        p.Start();
        p.WaitForExit();

        return p;
    }


    public static Process clang(string args, out string output, out string errorMsg) {
        var p = new Process();
        p.StartInfo = new ProcessStartInfo("clang", args) {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        p.Start();
        p.WaitForExit();

        output = p.StandardOutput.ReadToEnd();
        errorMsg = p.StandardError.ReadToEnd();

        return p;
    }

    public static int compileFile(string inputfile, string outputfile, out string output, out string errorMsg) {
        var p = clang("-c " + inputfile + " -o " + outputfile + includeArg + Project.compileargs, out output, out errorMsg);
        return p.ExitCode;
    }

    public static string getUserDependencies(string files) {
        var args = "-MM" + includeArg + files;
        var p = clang(args, out string output, out string error);
        if (!string.IsNullOrEmpty(error)) throw new Exception("\"clang " + args + "\" had following error:\n" + error);
        return output;
    }
}