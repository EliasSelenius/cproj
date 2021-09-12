using System;
using System.Diagnostics;

static class Clang {

    public static Process clang(string args, out string output) {
        var p = new Process();
        p.StartInfo = new ProcessStartInfo("clang", args) {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        p.Start();
        p.WaitForExit();

        output = p.StandardOutput.ReadToEnd();

        return p;
    }

    public static int compileFile(string inputfile, string outputfile, out string output) {
        var p = clang("-c " + inputfile + " -o " + outputfile, out output);
        return p.ExitCode;
    }


}