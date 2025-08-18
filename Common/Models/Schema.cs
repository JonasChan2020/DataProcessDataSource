namespace Common.Models;

public class Schema
{
    public List<FieldMeta> Fields { get; set; } = new();
}

public class FieldMeta
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? Length { get; set; }
    public bool Nullable { get; set; }
}
