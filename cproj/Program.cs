using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

/*
    cbuilder
    twix
    cproj run
*/



class Program {
    
    static string workingDir;
    static string topDirName;

  

    static void Main(string[] args) {
        workingDir = Directory.GetCurrentDirectory();
        topDirName = Path.GetFileName(workingDir);


        /*foreach (var item in typeof(Program).Assembly.GetManifestResourceNames())
            System.Console.WriteLine(item);
        return;*/

        if (args.Length < 1) {
            help();
        } else if (args.Length > 1) {
            System.Console.WriteLine("Invalid arguments. Can only recive one argument.");
            help();
        } else {
            var arg = args[0];

            if (arg.Equals("new")) {
                new_project();
            } else {
                // make sure the project exists before we continue
                if (!File.Exists(Project.xmlFilename)) {
                    System.Console.WriteLine("This is not a valid project. Run the \"new\" command to initialize one.");
                    return;
                }

                Project.load_xml();

                switch (arg) {
                    case "build": build(); break;
                    case "rebuild": clear(); build(); break;
                    case "run": run(); break;
                    case "clear": clear(); break;
                    default: System.Console.WriteLine("Unknown option \"" + arg + "\""); return;
                }
            }

        }

    }



    static void new_project() {
        Directory.CreateDirectory("bin");
        Directory.CreateDirectory("obj");
        Directory.CreateDirectory("include");
        Directory.CreateDirectory("src");
        if (File.Exists(Project.xmlFilename)) {
            System.Console.WriteLine("There is already a project file here.");
            return;
        }

        using var stream = typeof(Program).Assembly.GetManifestResourceStream("cproj.projectFileTemplate.xml");
        using var sr = new StreamReader(stream);
        var template = sr.ReadToEnd();
        template = template.Replace("__REPLACE_NAME__", topDirName);
        File.WriteAllText(Project.xmlFilename, template);

        System.Console.WriteLine("Created new project \"" + topDirName + "\"");
    }

    static void clear() {
        if (Directory.Exists("bin")) Directory.Delete("bin", true);
        if (Directory.Exists("obj")) Directory.Delete("obj", true);
    }

    static void run() {
        if (build()) {
            System.Console.WriteLine("Running...");
            var exeFile = ".\\bin\\" + Project.projectExe;
            try {
                Process.Start(exeFile).WaitForExit();
            } catch {
                System.Console.WriteLine("Could not run \"" + Project.projectExe + "\".");
                System.Console.WriteLine("Try performing a rebuild.");
            }
        }
    }


    /*
        build -> build
        run -> build and run executable
        clear -> clear obj and bin folder
        new -> creates project scaffolding
    */
    static void help() {
        System.Console.WriteLine("Usage:");
        System.Console.WriteLine("    build -> builds the project.");
        System.Console.WriteLine("    rebuild -> performs a clear and then builds the project.");
        System.Console.WriteLine("    run -> builds and runs the project.");
        System.Console.WriteLine("    clear -> deletes the obj and bin folders.");
        System.Console.WriteLine("    new -> creates project scaffolding.");
    }

    static bool build() {

        // make sure there are obj and bin folders
        Directory.CreateDirectory("bin");
        Directory.CreateDirectory("obj");

        Console.WriteLine("Building...");

        if (!compile(out string objfiles)) return false;
        if (!link(objfiles)) return false;
        
        Console.WriteLine("Build Successfull.");

        return true;
    }

    static bool link(string objfiles) {
        Console.WriteLine("Linking...");
        var exeName = "./bin/" + Project.projectExe;
        var p = Clang.clang(objfiles + " -o " + exeName + " " + Project.linkargs, out string output, out string error);
        if (p.ExitCode != 0) {
            Console.WriteLine(output);
            System.Console.WriteLine(error);
            Console.WriteLine("Resolve errors and try again.");
            return false;
        }
        return true;
    }

    static bool compile(out string objfiles) {

        var files = getCFiles(out int maxNameLength);
        maxNameLength += 5;

        objfiles = "";

        foreach (var item in files) {
            
            objfiles += " " + item.outputfile;

            // ensure that obj directories exist
            Directory.CreateDirectory(Path.GetDirectoryName(item.outputfile));

            // kompili dosieron, se ĝi ne estas ĝisdata
            if (!item.uptodate) {
                var exitcode = Clang.compileFile(item.inputfile, item.outputfile, out string output);
                if (exitcode != 0) {
                    Console.WriteLine(output);
                    Console.WriteLine("Compile exit code: " + exitcode);
                    Console.WriteLine("Resolve errors and try again.");
                    return false;
                }
            }

            var message = item.inputfile.PadRight(maxNameLength, '.') + (item.uptodate ? "uptodate" : "compiled");
            Console.WriteLine("    " + message);
        }

        return true;
    }

    static List<(string inputfile, string outputfile, bool uptodate)> getCFiles(out int maxNameLength) {

        List<(string inputfile, string outputfile, bool uptodate)> files = new();
        maxNameLength = 0;

        // enumerate C files
        foreach (var cFile in Directory.EnumerateFiles("src\\", "*.c", SearchOption.AllDirectories)) {
            
            // construct corresponding obj filename
            var objfile = Path.ChangeExtension(cFile, "o");
            objfile = "obj" + objfile.Substring(objfile.IndexOf(Path.DirectorySeparatorChar));

            
            bool uptodate = false;
            if (File.Exists(objfile)) {
                var cFile_lw = File.GetLastWriteTime(cFile);
                var objFile_lw = File.GetLastWriteTime(objfile);

                if (cFile_lw < objFile_lw) uptodate = true;
            }
            
            files.Add((
                inputfile:cFile, 
                outputfile:objfile, 
                uptodate:uptodate));

            maxNameLength = cFile.Length > maxNameLength ? cFile.Length : maxNameLength;
        }

        return files;
    }

    static void getHeaderDependencies(string filename) {
        var p = new Process();
        p.StartInfo = new ProcessStartInfo("clang", "-MM " + filename) {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };


        p.Start();
        p.WaitForExit();

        var output = p.StandardOutput.ReadToEnd();

        var spl = output.Split(' ');
        foreach (var i in spl) {
            System.Console.WriteLine(i);
        }

    }


}
