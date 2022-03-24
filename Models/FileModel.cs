namespace TreeViewTest.Models;

public record FileModel(string Name) : INode
{
    public string Name { get; } = Name;
}