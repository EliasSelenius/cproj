using System.Xml;


static class Project {
    public const string xmlFilename = "project.xml";

    public static XmlDocument projectXml = new();

    public static string projectName;
    public static string projectExe => projectName + ".exe"; 
    
    public static string args = "";


    public static void load_xml() {
        projectXml.Load(xmlFilename);

        var output = projectXml["project"]["output"];

        projectName = output["name"].InnerText;

        args = output["args"]?.InnerText ?? "";

    }

}