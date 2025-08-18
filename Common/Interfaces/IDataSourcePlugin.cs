namespace Common.Interfaces;

public interface IDataSourcePlugin
{
    string TypeCode { get; }
    string TypeName { get; }
    IDataSourceAdapter CreateAdapter();
}
