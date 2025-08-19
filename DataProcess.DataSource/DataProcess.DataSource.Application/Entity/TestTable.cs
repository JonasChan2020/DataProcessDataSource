// DataProcess.DataSource.Application/Entity/TestTable.cs
using SqlSugar;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// Ö÷¿â²âÊÔ±í
/// </summary>
[SugarTable("TestTable")]
public class TestTable
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string Name { get; set; }
}
