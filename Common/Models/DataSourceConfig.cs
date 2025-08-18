namespace Common.Models;

public class DataSourceConfig
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? DbName { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? ExtraJson { get; set; }
}
