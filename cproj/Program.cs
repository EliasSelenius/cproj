using System;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;

/*
    cbuilder
    twix
    cproj run
*/

class Program {
    
    static string workingDir;
    static string topDirName;

    const string xmlFilename = "project.xml";
    static XmlDocument projectXml = new();
    static string projectExe => projectName + ".exe"; 
    static string projectName;


  

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
                if (!File.Exists(xmlFilename)) {
                    System.Console.WriteLine("This is not a valid project. Run the \"new\" command to initialize one.");
                    return;
                }

                load_xml();

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

    static void load_xml() {
        projectXml.Load("project.xml");

        projectName = projectXml["project"]["output"]["name"].InnerText;

    }

    static void new_project() {
        Directory.CreateDirectory("bin");
        Directory.CreateDirectory("obj");
        Directory.CreateDirectory("src");
        if (File.Exists(xmlFilename)) {
            System.Console.WriteLine("There is already a project file here.");
            return;
        }

        using var stream = typeof(Program).Assembly.GetManifestResourceStream("cproj.projectFileTemplate.xml");
        using var sr = new StreamReader(stream);
        var template = sr.ReadToEnd();
        template = template.Replace("__REPLACE_NAME__", topDirName);
        File.WriteAllText(xmlFilename, template);

        System.Console.WriteLine("Created new project \"" + topDirName + "\"");
    }

    static void clear() {
        Directory.Delete("bin", true);
        Directory.Delete("obj", true);
    }

    static void run() {
        if (build()) {
            System.Console.WriteLine("Running...");
            var exeFile = ".\\bin\\" + projectExe;
            try {
                Process.Start(exeFile).WaitForExit();
            } catch {
                System.Console.WriteLine("Could not run \"" + projectExe + "\".");
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
        var process = Process.Start("clang", "./obj/*.o -o ./bin/" + projectExe);
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
        var psi = new ProcessStartInfo("clang", "-c" + cFiles) { WorkingDirectory = workingDir + "\\obj\\" };
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
        // TODO: detect if two files have the same name, wich will create an error in the objectfiles
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
}
