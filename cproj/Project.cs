using System.Xml;
using System.IO;

public enum ProjectType {
    None,
    Executable,
    StaticLibrary
}

static class Project {
    public const string xmlFilename = "project.xml";

    public static XmlDocument projectXml = new();

    public static string projectName;
    public static ProjectType projectType;
    public static string projectExe => projectName + ".exe"; 
    public static string projectLib => projectName + ".lib";

    public static string linkargs = "";
    public static string compileargs = "";

    public static bool load_xml() {
        // make sure the project exists before we continue
        if (!File.Exists(xmlFilename)) {
            System.Console.WriteLine("This is not a valid project. Run the \"new\" command to initialize one.");
            return false;
        }

        projectXml.Load(xmlFilename);

        var output = projectXml["project"]["output"];

        projectName = output["name"].InnerText;
        projectType = output["type"].InnerText switch {
            "exe" => ProjectType.Executable,
            "lib" => ProjectType.StaticLibrary,
            _ => ProjectType.None
        };

        if (projectType == ProjectType.None) {
            System.Console.WriteLine("Project type must be one of 'exe' or 'lib' in project.xml");
            return false;
        }

        linkargs = output["linkargs"]?.InnerText ?? "";
        compileargs = output["compileargs"]?.InnerText ?? "";

        return true;
    }

}