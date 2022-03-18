namespace TreeViewTest.ValueObjects;

public readonly record struct FindAndAll(uint Find, uint All)
{
    public readonly uint Find = Find;
    public readonly uint All = All;

    public override string ToString()
    {
        return $"{Find} / {All}";
    }
}