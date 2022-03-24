using System.Collections.ObjectModel;

namespace TreeViewTest.Models;

public class DirectoryModel : INode
{
    public DirectoryModel(string name)
    {
        Name = name;
    }
    
    public ObservableCollection<INode> Members { get; set; } = new();

    public string Name { get; }
}