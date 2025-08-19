// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

public class DbFileProvider : ICustomFileProvider, ITransient
{
    private readonly SqlSugarRepository<SysFileContent> _sysFileContentRep;

    public DbFileProvider(SqlSugarRepository<SysFileContent> sysFileContentRep)
    {
        _sysFileContentRep = sysFileContentRep;
    }

    public async Task DeleteFileAsync(SysFile sysFile)
    {
        // 从数据库中删除文件内容
        await _sysFileContentRep.DeleteAsync(u => u.SysFileId == sysFile.Id);
    }

    public async Task<string> DownloadFileBase64Async(SysFile sysFile)
    {
        // 若先前配置成保存到本地文件
        if (string.IsNullOrEmpty(sysFile.Provider) || sysFile.Provider == "Local")
        {
            var provider = App.GetService<DefaultFileProvider>();
            return await provider.DownloadFileBase64Async(sysFile);
        }
        else
        {
            // 从数据库获取文件内容
            var fileContent = await _sysFileContentRep.CopyNew().GetFirstAsync(u => u.SysFileId == sysFile.Id);
            if (fileContent == null || fileContent.Content == null)
            {
                Log.Error($"DbFileProvider.DownloadFileBase64:文件[{sysFile.Id},{sysFile.Url}]内容不存在");
                throw Oops.Oh($"文件[{sysFile.FilePath}]内容不存在");
            }
            return Convert.ToBase64String(fileContent.Content);
        }
    }

    public async Task<FileStreamResult> GetFileStreamResultAsync(SysFile sysFile, string fileName)
    {
        // 若先前配置成保存到本地文件
        if (string.IsNullOrEmpty(sysFile.Provider) || sysFile.Provider == "Local")
        {
            var provider = App.GetService<DefaultFileProvider>();
            return await provider.GetFileStreamResultAsync(sysFile, fileName);
        }
        else
        {
            // 从数据库获取文件内容
            var fileContent = await _sysFileContentRep.GetFirstAsync(u => u.SysFileId == sysFile.Id);
            if (fileContent == null || fileContent.Content == null)
            {
                Log.Error($"DbFileProvider.GetFileStreamResultAsync:文件[{sysFile.Id},{sysFile.Url}]内容不存在");
                throw Oops.Oh($"文件[{sysFile.FilePath}]内容不存在");
            }
            // 创建内存流
            var memoryStream = new MemoryStream(fileContent.Content);
            return new FileStreamResult(memoryStream, "application/octet-stream") { FileDownloadName = fileName + sysFile.Suffix };
        }
    }

    public async Task<SysFile> UploadFileAsync(IFormFile file, SysFile newFile, string path, string finalName)
    {
        newFile.Provider = "Database"; // 数据库存储 Provider 显示为Database

        // 读取文件内容到字节数组
        byte[] fileContent;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            fileContent = memoryStream.ToArray();
        }

        // 保存文件内容到数据库
        var sysFileContent = new SysFileContent
        {
            SysFileId = newFile.Id,
            Content = fileContent
        };
        await _sysFileContentRep.InsertAsync(sysFileContent);

        // 设置文件URL
        newFile.Url = $"upload/downloadfile?fileMd5={newFile.FileMd5}&id={newFile.Id}&fileName=tmp{newFile.Suffix}";
        return newFile;
    }
}