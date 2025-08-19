// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

public class SSHFileProvider : ICustomFileProvider, ITransient
{
    public Task DeleteFileAsync(SysFile sysFile)
    {
        var fullPath = string.Concat(sysFile.FilePath, "/", sysFile.Id + sysFile.Suffix);
        using SSHHelper helper = new(App.Configuration["SSHProvider:Host"],
            App.Configuration["SSHProvider:Port"].ToInt(), App.Configuration["SSHProvider:Username"], App.Configuration["SSHProvider:Password"]);
        helper.DeleteFile(fullPath);
        return Task.CompletedTask;
    }

    public Task<string> DownloadFileBase64Async(SysFile sysFile)
    {
        using SSHHelper helper = new(App.Configuration["SSHProvider:Host"],
            App.Configuration["SSHProvider:Port"].ToInt(), App.Configuration["SSHProvider:Username"], App.Configuration["SSHProvider:Password"]);
        return Task.FromResult(Convert.ToBase64String(helper.ReadAllBytes(sysFile.FilePath)));
    }

    public Task<FileStreamResult> GetFileStreamResultAsync(SysFile sysFile, string fileName)
    {
        var filePath = Path.Combine(sysFile.FilePath ?? "", sysFile.Id + sysFile.Suffix);
        using SSHHelper helper = new(App.Configuration["SSHProvider:Host"],
            App.Configuration["SSHProvider:Port"].ToInt(), App.Configuration["SSHProvider:Username"], App.Configuration["SSHProvider:Password"]);
        return Task.FromResult(new FileStreamResult(helper.OpenRead(filePath), "application/octet-stream") { FileDownloadName = fileName + sysFile.Suffix });
    }

    public Task<SysFile> UploadFileAsync(IFormFile file, SysFile sysFile, string path, string finalName)
    {
        var fullPath = string.Concat(path.StartsWith('/') ? path : "/" + path, "/", finalName);
        using SSHHelper helper = new(App.Configuration["SSHProvider:Host"],
            App.Configuration["SSHProvider:Port"].ToInt(), App.Configuration["SSHProvider:Username"], App.Configuration["SSHProvider:Password"]);
        helper.UploadFile(file.OpenReadStream(), fullPath);
        return Task.FromResult(sysFile);
    }
}