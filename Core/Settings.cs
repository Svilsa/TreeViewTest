using System.IO;
using System.Xml.Serialization;

namespace TreeViewTest.Core;

public class Settings
{
    private const string SaveFilePath = "./save.xml";

    public string? Path { get; set; } 
    public string? Regex { get; set; } 

    public bool TrySave()
    {
        try
        {
            if (string.IsNullOrEmpty(Path) && string.IsNullOrEmpty(Regex))
                return false;
            
            var serializer = new XmlSerializer(typeof(Settings));
            using var fs = new FileStream(SaveFilePath, FileMode.Create);
            serializer.Serialize(fs, this);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public static bool TryLoad(out Settings? settings)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(Settings));
            using var fs = new FileStream(SaveFilePath, FileMode.Open);
            var deserializedObj = serializer.Deserialize(fs);

            if (deserializedObj == null)
            {
                settings = null;
                return false;
            }

            settings = (Settings)deserializedObj;
            return true;
        }
        catch
        {
            settings = null;
            return false;
        }
    }
}