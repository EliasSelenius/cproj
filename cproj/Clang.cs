using System;
using System.Diagnostics;

static class Clang {

    static void clang(string args, out string output) {
        var p = new Process();
        p.StartInfo = new ProcessStartInfo("clang", args) {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        p.Start();
        p.WaitForExit();

        output = p.StandardOutput.ReadToEnd();

    }

    public static void compile() {

    } 

}