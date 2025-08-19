// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 对象转换扩展方法
/// </summary>
public static class ParseExtensions
{
    #region Bool

    /// <summary>
    /// 对象转布尔值
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static bool ParseToBool(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value && bool.TryParse(thisValue.ToString(), out var reveal) && reveal;
    }

    #endregion Bool

    #region Short

    /// <summary>
    /// 对象转短整数
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static short ParseToShort(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            short.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : (short)0;
    }

    /// <summary>
    /// 对象转短整数
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static short ParseToShort(this object? thisValue, short errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            short.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion Short

    #region Long

    /// <summary>
    /// 对象转长整数
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static long ParseToLong(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            long.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : 0L;
    }

    /// <summary>
    /// 对象转长整数
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static long ParseToLong(this object? thisValue, long errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            long.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion Long

    #region Float

    /// <summary>
    /// 对象转浮点数
    /// ±1.5 x 10 e−45 至 ±3.4 x 10 e38	大约 6-9 位数字
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static float ParseToFloat(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            float.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : 0.0F;
    }

    /// <summary>
    /// 对象转浮点数
    /// ±1.5 x 10 e−45 至 ±3.4 x 10 e38	大约 6-9 位数字
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static float ParseToFloat(this object? thisValue, float errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            float.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion Float

    #region Double

    /// <summary>
    /// 对象转浮点数
    /// ±5.0 × 10 e−324 到 ±1.7 × 10 e308	大约 15-17 位数字
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static double ParseToDouble(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            double.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : 0.0D;
    }

    /// <summary>
    /// 对象转浮点数
    /// ±5.0 × 10 e−324 到 ±1.7 × 10 e308	大约 15-17 位数字
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static double ParseToDouble(this object? thisValue, double errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            double.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion Double

    #region Decimal

    /// <summary>
    /// 对象转浮点数
    /// ±1.0 x 10 e-28 至 ±7.9228 x 10 e28	28-29 位
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static decimal ParseToDecimal(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            decimal.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : 0M;
    }

    /// <summary>
    /// 对象转浮点数
    /// ±1.0 x 10 e-28 至 ±7.9228 x 10 e28	28-29 位
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static decimal ParseToDecimal(this object? thisValue, decimal errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            decimal.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion Decimal

    #region Int

    /// <summary>
    /// 对象转数字
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static int ParseToInt(this object? thisValue)
    {
        return thisValue is null ? 0 :
            thisValue != DBNull.Value && int.TryParse(thisValue.ToString(), out var reveal) ? reveal : 0;
    }

    /// <summary>
    /// 对象转数字
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static int ParseToInt(this object? thisValue, int errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            int.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion Int

    #region Money

    /// <summary>
    /// 对象转金额
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static double ParseToMoney(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            double.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : 0;
    }

    /// <summary>
    /// 对象转金额
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static double ParseToMoney(this object? thisValue, double errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            double.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion Money

    #region String

    /// <summary>
    /// 对象转字符串
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static string ParseToString(this object? thisValue)
    {
        return thisValue is not null ? thisValue.ToString()!.Trim() : string.Empty;
    }

    /// <summary>
    /// 对象转字符串
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static string ParseToString(this object? thisValue, string errorValue)
    {
        return thisValue is not null ? thisValue.ToString()!.Trim() : errorValue;
    }

    /// <summary>
    /// 判断是否为空
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static bool IsEmptyOrNull(this object? thisValue)
    {
        return !thisValue.IsNotEmptyOrNull();
    }

    /// <summary>
    /// 判断是否为空
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static bool IsNotEmptyOrNull(this object? thisValue)
    {
        return thisValue is not null && thisValue.ParseToString() != string.Empty &&
            thisValue.ParseToString() != string.Empty &&
            thisValue.ParseToString() != "undefined" && thisValue.ParseToString() != "null";
    }

    /// <summary>
    /// 判断是否为空或零
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static bool IsNullOrZero(this object? thisValue)
    {
        return !thisValue.IsNotNullOrZero();
    }

    /// <summary>
    /// 判断是否为空或零
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static bool IsNotNullOrZero(this object? thisValue)
    {
        return thisValue.IsNotEmptyOrNull() && thisValue.ParseToString() != "0";
    }

    #endregion String

    #region DateTime

    /// <summary>
    /// 对象转日期
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static DateTime ParseToDateTime(this object? thisValue)
    {
        var reveal = DateTime.MinValue;
        if (thisValue is not null && thisValue != DBNull.Value && DateTime.TryParse(thisValue.ToString(), out reveal))
        {
            reveal = Convert.ToDateTime(thisValue);
        }

        return reveal;
    }

    /// <summary>
    /// 对象转日期
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static DateTime ParseToDateTime(this object? thisValue, DateTime errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value && DateTime.TryParse(thisValue.ToString(), out var reveal)
            ? reveal
            : errorValue;
    }

    #endregion DateTime

    #region DateTimeOffset

    /// <summary>
    /// 对象转 DateTimeOffset
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static DateTimeOffset ParseToDateTimeOffset(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            DateTimeOffset.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : DateTimeOffset.MinValue;
    }

    /// <summary>
    /// 对象转 DateTimeOffset
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static DateTimeOffset ParseToDateTimeOffset(this object? thisValue, DateTimeOffset errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            DateTimeOffset.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion DateTimeOffset

    #region TimeSpan

    /// <summary>
    /// 对象转 DateTimeOffset
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static TimeSpan ParseToTimeSpan(this object? thisValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            TimeSpan.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : TimeSpan.Zero;
    }

    /// <summary>
    /// 对象转 DateTimeOffset
    /// </summary>
    /// <param name="thisValue"></param>
    /// <param name="errorValue"></param>
    /// <returns></returns>
    public static TimeSpan ParseToTimeSpan(this object? thisValue, TimeSpan errorValue)
    {
        return thisValue is not null && thisValue != DBNull.Value &&
            TimeSpan.TryParse(thisValue.ToString(), out var reveal)
                ? reveal
                : errorValue;
    }

    #endregion TimeSpan

    #region Guid

    /// <summary>
    /// 将 string 转换为 Guid
    /// 若转换失败，则返回 Guid.Empty，不抛出异常。
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static Guid ParseToGuid(this object? thisValue)
    {
        try
        {
            return new Guid(thisValue.ParseToString());
        }
        catch
        {
            return Guid.Empty;
        }
    }

    #endregion Guid

    #region Dictionary

    /// <summary>
    /// 对象转换成字典
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static IEnumerable<Dictionary<string, dynamic>> ParseToDictionary(this object? obj)
    {
        if (obj is not IEnumerable<dynamic> objDynamics)
        {
            yield break;
        }

        foreach (var objDynamic in objDynamics)
        {
            // 找到所有的没有此特性、或有此特性但忽略字段的属性
            var item = (objDynamic as object).GetType().GetProperties()
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(objDynamic, null));

            yield return item;
        }
    }

    #endregion Dictionary
}