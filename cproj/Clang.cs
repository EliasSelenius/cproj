using System;
using System.Diagnostics;

static class Clang {

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

    public static int compileFile(string inputfile, string outputfile, out string errorMsg) {
        var p = clang("-c " + inputfile + " -o " + outputfile + " -Iinclude", out _, out errorMsg);
        return p.ExitCode;
    }

    public static string getUserDependencies(string files) {
        var p = clang("-MM " + files, out string output, out _);
        return output;
    }
}