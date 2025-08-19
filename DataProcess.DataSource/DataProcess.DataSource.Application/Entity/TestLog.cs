using SqlSugar;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 日志库测试表
/// </summary>
[SugarTable("TestLog")]
public class TestLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string Log { get; set; }
}