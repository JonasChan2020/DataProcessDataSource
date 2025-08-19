// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.Text.Encodings.Web;
using System.Text.Json;

namespace Admin.NET.Core;

public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    public LoggingHandler(HttpMessageHandler innerHandler)
    {
        InnerHandler = innerHandler ?? new HttpClientHandler();
        _logger = App.GetService<ILogger<LoggingHandler>>();
    }

    private string FormatJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            return JsonSerializer.Serialize(jsonElement, options);
        }
        catch
        {
            return json;
        }
    }

    private string ConvertUnicodeToChinese(string text)
    {
        return Regex.Replace(text, @"\\u([0-9a-fA-F]{4})", match =>
        {
            return ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString();
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var loggerContent = new StringBuilder();
        loggerContent.AppendLine("=== HTTP Request ===");
        loggerContent.AppendLine($"{request.Method} {request.RequestUri} HTTP/1.1");
        foreach (var header in request.Headers)
        {
            loggerContent.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            if (request.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
            {
                content = FormatJson(content);
            }
            content = ConvertUnicodeToChinese(content);
            loggerContent.AppendLine($"\n{content}");
        }
        loggerContent.AppendLine();
        _logger.LogInformation("======== LLM请求开始 =========");
        _logger.LogInformation(loggerContent.ToString());
        _logger.LogInformation("======== LLM请求结束 =========");
        var response = await base.SendAsync(request, cancellationToken);
        loggerContent.Clear();

        // 检查是否是流式响应
        if (response.Content != null &&
            response.Content.Headers.ContentType?.MediaType?.Contains("text/event-stream") == true)
        {
            // 记录响应头
            loggerContent.AppendLine("=== HTTP Response (Stream) ===");
            loggerContent.AppendLine($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}");
            foreach (var header in response.Headers)
            {
                loggerContent.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            _logger.LogInformation("======== LLM流式响应开始 =========");
            _logger.LogInformation(loggerContent.ToString());

            // 创建包装流来记录内容
            var originalStream = await response.Content.ReadAsStreamAsync();
            var loggingStream = new LoggingStream(originalStream, chunk =>
            {
                _logger.LogInformation($"流式响应内容: {chunk}");
            });

            // 创建新的响应内容
            var newContent = new StreamContent(loggingStream);
            foreach (var header in response.Content.Headers)
            {
                newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.Content = newContent;
            return response;
        }

        // 非流式响应的处理保持不变
        loggerContent.AppendLine("=== HTTP Response ===");
        loggerContent.AppendLine($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}");
        foreach (var header in response.Headers)
        {
            loggerContent.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
        if (response.Content != null)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
            {
                content = FormatJson(content);
            }
            content = ConvertUnicodeToChinese(content);
            loggerContent.AppendLine($"\n{content}");
        }
        loggerContent.AppendLine();
        _logger.LogInformation("======== LLM响应开始 =========");
        _logger.LogInformation(loggerContent.ToString());
        _logger.LogInformation("======== LLM响应结束 =========");

        return response;
    }
}

// 用于记录流内容的包装流
public class LoggingStream : Stream
{
    private readonly Stream _innerStream;
    private readonly Action<string> _logAction;
    private readonly StringBuilder _buffer = new StringBuilder();

    public LoggingStream(Stream innerStream, Action<string> logAction)
    {
        _innerStream = innerStream;
        _logAction = logAction;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        if (bytesRead > 0)
        {
            var chunk = Encoding.UTF8.GetString(buffer, offset, bytesRead);
            _buffer.Append(chunk);

            // 检查是否是一个完整的SSE消息
            if (chunk.Contains("\n\n") || chunk.Contains("\r\n\r\n"))
            {
                var message = _buffer.ToString().Trim();
                _logAction(message);
                _buffer.Clear();
            }
        }
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        if (bytesRead > 0)
        {
            var chunk = Encoding.UTF8.GetString(buffer, offset, bytesRead);
            _buffer.Append(chunk);

            // 检查是否是一个完整的SSE消息
            if (chunk.Contains("\n\n") || chunk.Contains("\r\n\r\n"))
            {
                var message = _buffer.ToString().Trim();
                _logAction(message);
                _buffer.Clear();
            }
        }
        return bytesRead;
    }

    public override void Flush() => _innerStream.Flush();

    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

    public override void SetLength(long value) => _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }
}