// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Admin.NET.Core;

/// <summary>
/// 首字母小写（驼峰样式）转换
/// </summary>
public class CamelCaseValueConverter : JsonConverter
{
    private static readonly CamelCaseNamingStrategy NamingStrategy = new(true, true);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (typeof(IEnumerable<string>).IsAssignableFrom(value.GetType()))
            serializer.Serialize(writer, ((IEnumerable<string>)value).Select(u => NamingStrategy.GetPropertyName(u, false)));
        else
            writer.WriteValue(NamingStrategy.GetPropertyName(value + "", false));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return reader.Value;
    }

    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(string) || typeof(IEnumerable<string>).IsAssignableFrom(objectType))
            return true;

        return false;
    }
}