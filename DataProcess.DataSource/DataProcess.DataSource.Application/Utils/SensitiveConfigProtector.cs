using System.Security.Cryptography;
using System.Text;

namespace DataProcess.DataSource.Application.Utils;

public static class SensitiveConfigProtector
{
    private static readonly byte[] AesKey = SHA256.HashData(Encoding.UTF8.GetBytes("DataSource:Default:KMS:Key"));
    private static readonly byte[] AesIv  = MD5.HashData(Encoding.UTF8.GetBytes("DataSource:Default:KMS:IV"));

    private static readonly HashSet<string> Keys = new(StringComparer.OrdinalIgnoreCase)
    { "password","pwd","pass","secret","token","accesskey","accesskeyid","accesskeysecret","clientsecret","apikey","privatekey" };

    public static string? EncryptSensitiveFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            var dict = JSON.Deserialize<Dictionary<string, object?>>(json!) ?? new();
            foreach (var k in dict.Keys.ToList())
            {
                if (dict[k] is string s && Keys.Contains(k) && !s.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
                    dict[k] = "enc:" + Encrypt(s);
            }
            return JSON.Serialize(dict);
        }
        catch { return json; }
    }

    public static string? DecryptSensitiveFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            var dict = JSON.Deserialize<Dictionary<string, object?>>(json!) ?? new();
            foreach (var k in dict.Keys.ToList())
            {
                if (dict[k] is string s && s.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
                    dict[k] = Decrypt(s[4..]);
            }
            return JSON.Serialize(dict);
        }
        catch { return json; }
    }

    private static string Encrypt(string plain)
    {
        using var aes = Aes.Create();
        aes.Key = AesKey; aes.IV = AesIv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        using var enc = aes.CreateEncryptor();
        var src = Encoding.UTF8.GetBytes(plain);
        var bytes = enc.TransformFinalBlock(src, 0, src.Length);
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
}