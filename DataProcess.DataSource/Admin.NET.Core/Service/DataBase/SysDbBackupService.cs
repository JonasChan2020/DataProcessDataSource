// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.IO.Compression;
using DbType = SqlSugar.DbType;

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统数据库备份服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 255, Description = "数据库备份")]
public class SysDbBackupService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly SysDatabaseService _databaseService;
    private readonly string _backupDir;

    public SysDbBackupService(ISqlSugarClient db, SysDatabaseService databaseService)
    {
        _db = db;
        _databaseService = databaseService;
        _backupDir = Path.Combine(App.WebHostEnvironment.WebRootPath, "DbBackup");
    }

    /// <summary>
    /// 获取备份列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取备份列表")]
    public List<DbBackupOutput> GetList()
    {
        try
        {
            if (!Directory.Exists(_backupDir))
                Directory.CreateDirectory(_backupDir);
            var fileList = Directory.GetFiles(_backupDir);

            var dbBackupList = new List<DbBackupOutput>();
            foreach (var item in fileList)
            {
                var info = new FileInfo(item);
                dbBackupList.Add(new DbBackupOutput
                {
                    FileName = info.Name,
                    Size = info.Length,
                    CreateTime = info.CreationTime
                });
            }

            dbBackupList = dbBackupList.OrderByDescending(u => u.CreateTime).ToList();
            return dbBackupList;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 备份数据库 🔖
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("备份数据库")]
    public async Task AddBackup([FromQuery] string configId)
    {
        await Backup(configId);
    }

    /// <summary>
    /// 删除备份 🔖
    /// </summary>
    /// <param name="fileName"></param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除备份")]
    public void DeleteBackup([FromQuery] string fileName)
    {
        var path = Path.Combine(_backupDir, fileName);
        File.Delete(path);
    }

    /// <summary>
    /// 备份数据库
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task Backup(string configId)
    {
        var options = App.GetOptions<DbConnectionOptions>();
        var option = options.ConnectionConfigs.FirstOrDefault(u => u.ConfigId + "" == configId) ?? throw Oops.Bah(ErrorCodeEnum.D1002);
        var db = _db.AsTenant().GetConnectionScope(configId);

        // 扩展名
        var ext = "bak";
        switch (option.DbType)
        {
            case DbType.MySql:
                ext = "sql";
                break;

            case DbType.SqlServer:
                ext = "bak";
                break;

            case DbType.Sqlite:
                ext = "db";
                break;

            case DbType.PostgreSQL:
                ext = "sql";
                break;
        }

        // 生成临时文件路径
        var tempPath = Path.Combine(Path.GetTempPath(), $"{db.Ado.Connection.Database}_{DateTime.Now:yyyyMMddHHmmss}.{ext}");
        var finalPath = Path.Combine(_backupDir, $"{db.Ado.Connection.Database}_{DateTime.Now:yyyyMMddHHmmss}.{ext}");

        // 备份数据库
        switch (option.DbType)
        {
            case DbType.MySql or DbType.Sqlite or DbType.SqlServer:
                {
                    // 使用临时路径进行备份
                    await Task.Run(() => { db.DbMaintenance.BackupDataBase(db.Ado.Connection.Database, tempPath); });
                    // 备份成功后，将临时文件移动到目标路径
                    File.Move(tempPath, finalPath);
                    break;
                }

            case DbType.PostgreSQL:
                {
                    var fileStreamResult = (FileStreamResult)(await _databaseService.BackupDatabase());
                    // 将 fileStreamResult 保存为临时文件
                    await using var fileStream = new FileStream(tempPath, FileMode.Create);
                    await fileStreamResult.FileStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();

                    // 备份成功后，将临时文件移动到目标路径
                    File.Move(tempPath, finalPath);
                    break;
                }
            default:
                throw Oops.Bah(ErrorCodeEnum.db1004, option.DbType);
        }
    }

    /// <summary>
    /// 删除过期备份文件
    /// </summary>
    /// <param name="day">过期天数</param>
    [NonAction]
    public void DeleteExpiredDbFile(int day = 7)
    {
        var list = Directory.GetFiles(_backupDir);
        foreach (var item in list)
        {
            var info = new FileInfo(item);
            if (info.CreationTime.AddDays(day) < DateTime.Now)
            {
                try
                {
                    File.Delete(item);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    /// <summary>
    /// 下载备份 🔖
    /// </summary>
    /// <param name="fileName">备份文件名</param>
    [ApiDescriptionSettings(Name = "Download"), HttpGet]
    [DisplayName("下载备份")]
    [AllowAnonymous]
    [NonUnify]
    public IActionResult Download(string fileName)
    {
        var path = Path.Combine(_backupDir, fileName);
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new FileStreamResult(fs, "application/octet-stream") { FileDownloadName = fileName };
    }

    /// <summary>
    /// 压缩备份 🔖
    /// </summary>
    /// <param name="fileName">备份文件名</param>
    /// <returns>压缩后的文件路径</returns>
    [ApiDescriptionSettings(Name = "Compress"), HttpPost]
    [DisplayName("压缩备份文件")]
    public async Task Compress([FromQuery] string fileName)
    {
        var sourcePath = Path.Combine(_backupDir, fileName);
        if (!File.Exists(sourcePath)) return;

        // 生成临时文件路径
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}.zip");
        var finalPath = Path.Combine(_backupDir, $"{Path.GetFileNameWithoutExtension(fileName)}.zip");

        try
        {
            // 使用临时路径进行压缩
            await Task.Run(() =>
            {
                using var zip = new ZipArchive(new FileStream(tempPath, FileMode.Create), ZipArchiveMode.Create);
                var entry = zip.CreateEntry(fileName);
                using var entryStream = entry.Open();
                using var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                fileStream.CopyTo(entryStream);
            });

            // 压缩成功后，将临时文件移动到目标路径
            File.Move(tempPath, finalPath);
        }
        catch
        {
            // 清理临时文件
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            throw;
        }
    }
}