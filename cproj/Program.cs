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
                    case "watch": watch(); break;
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


    // TODO: make watch mode cli output much nicer.
    static void watch() {
        System.Console.WriteLine("Starting watch mode...");
        using var watcher = new FileSystemWatcher(".\\src\\");
        watcher.IncludeSubdirectories = true;
        //watcher.EnableRaisingEvents = true;
        watcher.NotifyFilter = NotifyFilters.LastWrite;


        /*watcher.Changed += (s, o) => {
            
            System.Console.WriteLine("Change in: " + o.Name + " " + o.ChangeType.ToString());
        };*/

        Process process = null;

        while (true) {
            if (build()) {
                var exeFile = ".\\bin\\" + Project.projectExe;
                try {
                    process = Process.Start(exeFile);
                } catch {
                    System.Console.WriteLine("Could not run \"" + Project.projectExe + "\".");
                    System.Console.WriteLine("Try performing a rebuild.");
                }
            }

            var s = watcher.WaitForChanged(WatcherChangeTypes.Changed);
            System.Console.WriteLine(s.Name + " " + s.ChangeType);
            process?.Kill();
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
        System.Console.WriteLine("    watch -> builds and runs the project. Any file change will rebuild and rerun the project.");
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
        var exeName = "./bin/" + Project.projectExe;
        var args = objfiles + " -o " + exeName + " " + Project.linkargs;
        Console.WriteLine("Linking...      clang " + args);
        //var p = Clang.clang(args, out string output, out string error);
        var p = Clang.link(args);
        if (p.ExitCode != 0) {
            Console.WriteLine("\n\nResolve errors and try again.");
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
                var exitcode = Clang.compileFile(item.inputfile, item.outputfile, out string output, out string errorOutput);
                if (exitcode != 0) {
                    Console.WriteLine(errorOutput);
                    Console.WriteLine("Compile exit code: " + exitcode);
                    Console.WriteLine("Resolve errors and try again.");
                    return false;
                } else {
                    if (!string.IsNullOrWhiteSpace(errorOutput)) {
                        Console.WriteLine(output);
                        Console.WriteLine(errorOutput);
                    }
                }
            }

            var message = item.inputfile.PadRight(maxNameLength, '.') + (item.uptodate ? "uptodate" : "compiled");
            Console.WriteLine("    " + message);
            // System.Console.Write(Clang.getUserDependencies(item.inputfile));
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

            files.Add((
                inputfile:cFile, 
                outputfile:objfile, 
                uptodate:isUptodate(cFile, objfile)));

            maxNameLength = cFile.Length > maxNameLength ? cFile.Length : maxNameLength;
        }

        return files;
    }

    static bool isUptodate(string cFile, string objfile) {
        if (File.Exists(objfile)) {
            var cFile_lw = File.GetLastWriteTime(cFile);
            var objFile_lw = File.GetLastWriteTime(objfile);

            // is the cFile newer?
            if (objFile_lw < cFile_lw) return false;
            
            // is any of the user header files newer?
            var headerfiles = getUserDependencies(cFile);
            for (int i = 2; i < headerfiles.Length; i++) {
                var headerFile_lw = File.GetLastWriteTime(headerfiles[i]);
                if (objFile_lw < headerFile_lw) return false;
            }

            // everything up to date
            return true;
        }

        // object file does not exist, must compile...
        return false;
    }

    static string[] getUserDependencies(string cFile) {
        var ud = Clang.getUserDependencies(cFile);
        var headerfiles = ud.TrimEnd()
                            .Replace(" \\\r\n", " ") // TODO: what if '\n' instead of '\r\n'
                            .Split(' ', 
                                StringSplitOptions.RemoveEmptyEntries | 
                                StringSplitOptions.TrimEntries);
        return headerfiles;
    }
}
