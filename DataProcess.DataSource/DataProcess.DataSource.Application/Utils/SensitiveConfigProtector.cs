using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DataProcess.DataSource.Application.Utils;

public static class SensitiveConfigProtector
{
    // 注意：生产环境请改为配置/密钥管理，或使用 KMS
    private static readonly byte[] AesKey = SHA256.HashData(Encoding.UTF8.GetBytes("DataSource:Default:KMS:Key"));
    private static readonly byte[] AesIv  = MD5.HashData(Encoding.UTF8.GetBytes("DataSource:Default:KMS:IV"));

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password","pwd","pass","secret","token","accesskey","accesskeyid","accesskeysecret","clientsecret","apikey","privatekey"
    };

    public static string? EncryptSensitiveFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            var doc = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
            var flat = Flatten(doc);
            foreach (var key in flat.Keys.ToList())
            {
                var last = key.Split(':').Last();
                if (SensitiveKeys.Contains(last) && flat[key] is string str && !IsEncrypted(str))
                    flat[key] = "enc:" + Encrypt(str);
            }
            var merged = UnFlatten(flat);
            return JsonSerializer.Serialize(merged);
        }
        catch
        {
            return json; // 容错
        }
    }

    public static string? DecryptSensitiveFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            var doc = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
            var flat = Flatten(doc);
            foreach (var key in flat.Keys.ToList())
            {
                if (flat[key] is string s && IsEncrypted(s))
                    flat[key] = Decrypt(s[4..]);
            }
            var merged = UnFlatten(flat);
            return JsonSerializer.Serialize(merged);
        }
        catch
        {
            return json; // 容错
        }
    }

    private static bool IsEncrypted(string s) => s.StartsWith("enc:", StringComparison.OrdinalIgnoreCase);

    private static string Encrypt(string plain)
    {
        using var aes = Aes.Create();
        aes.Key = AesKey; aes.IV = AesIv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        using var enc = aes.CreateEncryptor();
        var bytes = enc.TransformFinalBlock(Encoding.UTF8.GetBytes(plain), 0, Encoding.UTF8.GetByteCount(plain));
        return Convert.ToBase64String(bytes);
    }

    private static string Decrypt(string b64)
    {
        using var aes = Aes.Create();
        aes.Key = AesKey; aes.IV = AesIv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        using var dec = aes.CreateDecryptor();
        var src = Convert.FromBase64String(b64);
        var bytes = dec.TransformFinalBlock(src, 0, src.Length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static Dictionary<string, object?> Flatten(Dictionary<string, object?> dict, string prefix = "")
    {
        var res = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in dict)
        {
            var key = string.IsNullOrEmpty(prefix) ? kv.Key : $"{prefix}:{kv.Key}";
            if (kv.Value is JsonElement je) // 兼容 System.Text.Json 的不同 ValueKind
            {
                if (je.ValueKind == JsonValueKind.Object)
                {
                    var sub = JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText()) ?? new();
                    foreach (var p in Flatten(sub, key))
                        res[p.Key] = p.Value;
                }
                else
                {
                    res[key] = je.ValueKind switch
                    {
                        JsonValueKind.String => je.GetString(),
                        JsonValueKind.Number => je.ToString(),
                        JsonValueKind.True or JsonValueKind.False => je.GetBoolean().ToString(),
                        _ => je.GetRawText()
                    };
                }
            }
            else if (kv.Value is Dictionary<string, object?> map)
            {
                foreach (var p in Flatten(map, key))
                    res[p.Key] = p.Value;
            }
            else
            {
                res[key] = kv.Value?.ToString();
            }
        }
        return res;
    }

    private static Dictionary<string, object?> UnFlatten(Dictionary<string, object?> flat)
    {
        var root = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in flat)
        {
            var parts = kv.Key.Split(':');
            var cur = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!cur.TryGetValue(parts[i], out var next) || next is not Dictionary<string, object?> dict)
                {
                    dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    cur[parts[i]] = dict;
                }
                cur = (Dictionary<string, object?>)cur[parts[i]]!;
            }
            cur[parts[^1]] = kv.Value;
        }
        return root;
    }
}