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
                    case "test": test(); break;
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

        string cFiles = getCFiles();

        if (!string.IsNullOrEmpty(cFiles)) {
            
            if (!compile(cFiles)) return false;

            if (!link()) return false;

            System.Console.WriteLine("Build Successfull!");
        } else {

            //TODO: there may have been an earlier linker error, so check if the executable exists, if not, linking must happen
            System.Console.WriteLine("Everything up to date.");
        }


        return true;
    }

    static bool link() {
        // linking objs
        System.Console.WriteLine("Linking...");
        var args = "./obj/*.o -o ./bin/" + Project.projectExe + " " + Project.args;
        System.Console.WriteLine(args);
        var process = Process.Start("clang", args);
        process.WaitForExit();
        if (process.ExitCode != 0) {
            System.Console.WriteLine("Link exit code: " + process.ExitCode);
            System.Console.WriteLine("Resolve errors and try again.");
            return false;
        }
        return true;
    }

    static bool compile(string cFiles) {
        // compiling objs
        System.Console.WriteLine("Compiling... " + cFiles);
        var args = "-c" + cFiles; //+ " " + project.args;
        System.Console.WriteLine(args);
        var psi = new ProcessStartInfo("clang", args) { WorkingDirectory = workingDir + "\\obj\\" };
        var process = Process.Start(psi);
        process.WaitForExit();

        if (process.ExitCode != 0) {
            System.Console.WriteLine("Compile exit code: " + process.ExitCode);
            System.Console.WriteLine("Resolve errors and try again.");
            return false;
        }
        return true;
    }

    static string getCFiles() {
        List<(string cFile, string objFile, bool mustCompile)> cFilesList = new();
        int maxNameLength = 0;

        // find all modified c files
        foreach (var cFile in Directory.EnumerateFiles("src\\", "*.c", SearchOption.AllDirectories)) {
            
            var objFile = "obj\\" + Path.GetFileNameWithoutExtension(cFile) + ".o";

            var mustCompile = true;
            if (File.Exists(objFile)) {
                var cFile_lw = File.GetLastWriteTime(cFile);
                var objFile_lw = File.GetLastWriteTime(objFile);

                if (cFile_lw < objFile_lw) mustCompile = false;
            }

            cFilesList.Add((cFile, objFile, mustCompile));
            maxNameLength = cFile.Length > maxNameLength ? cFile.Length : maxNameLength;
        }

        maxNameLength += 5;

        string cFiles = "";
        foreach (var item in cFilesList) {
            if (item.mustCompile) cFiles += " ..\\" + item.cFile;
            var cname = item.cFile.PadRight(maxNameLength, '.');
            var status = item.mustCompile ? "compiled" : "uptodate";
            System.Console.WriteLine("    " + cname + status);
        }

        return cFiles;
    }

    static void test() {

        // enumerate C files
        foreach (var cFile in Directory.EnumerateFiles("src\\", "*.c", SearchOption.AllDirectories)) {
            System.Console.WriteLine(cFile);
            
            // construct corresponding obj filename
            var objfile = Path.ChangeExtension(cFile, "o");
            objfile = "obj" + objfile.Substring(objfile.IndexOf(Path.DirectorySeparatorChar));

            System.Console.WriteLine(objfile);

            // ensure that obj directories exist
            Directory.CreateDirectory(Path.GetDirectoryName(objfile));

            if (File.Exists(objfile)) {
                var cFile_lw = File.GetLastWriteTime(cFile);
                var objFile_lw = File.GetLastWriteTime(objfile);

                if (objFile_lw < cFile_lw) {
                    // queue for compilation
                }
            }
            
        }
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
