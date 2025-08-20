using Furion.DynamicApiController;
using SqlSugar;
using System.Diagnostics;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 数据源实例管理服务
/// </summary>
[ApiDescriptionSettings(Order = 110, Name = "数据源实例管理")]
public class DataSourceInstanceService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly PluginManager _pluginManager;

    public DataSourceInstanceService(ISqlSugarClient db, PluginManager pluginManager)
    {
        _db = db;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// 分页查询数据源实例
    /// </summary>
    [HttpPost]
    public async Task<SqlSugarPagedList<DataSourceInstanceDto>> Page(DataSourceInstancePageInput input)
    {
        var query = _db.Queryable<DataSourceInstance>()
            .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
            .LeftJoin<DataSourceInstance>((i, t, p) => i.ParentId == p.Id)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), (i, t, p) => i.Name.Contains(input.Name))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), (i, t, p) => i.Code.Contains(input.Code))
            .WhereIF(input.TypeId.HasValue, (i, t, p) => i.TypeId == input.TypeId)
            .WhereIF(input.ParentId.HasValue, (i, t, p) => i.ParentId == input.ParentId)
            .WhereIF(input.Status.HasValue, (i, t, p) => i.Status == input.Status)
            .OrderBy((i, t, p) => i.OrderNo)
            .ThenBy((i, t, p) => i.CreateTime)
            .Select((i, t, p) => new DataSourceInstanceDto
            {
                Id = i.Id,
                Name = i.Name,
                Code = i.Code,
                TypeId = i.TypeId,
                TypeName = t.Name,
                TypeCode = t.Code,
                ConfigJson = i.ConfigJson,
                ParentId = i.ParentId,
                ParentName = p.Name,
                ConnectionStatus = i.ConnectionStatus,
                LastConnectTime = i.LastConnectTime,
                Status = i.Status,
                Remark = i.Remark,
                OrderNo = i.OrderNo,
                CreateTime = i.CreateTime
            });

        return await query.ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取数据源实例列表
    /// </summary>
    [HttpGet]
    public async Task<List<DataSourceInstanceDto>> GetList()
    {
        return await _db.Queryable<DataSourceInstance>()
            .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
            .LeftJoin<DataSourceInstance>((i, t, p) => i.ParentId == p.Id)
            .Where((i, t, p) => i.Status == true)
            .OrderBy((i, t, p) => i.OrderNo)
            .ThenBy((i, t, p) => i.CreateTime)
            .Select((i, t, p) => new DataSourceInstanceDto
            {
                Id = i.Id,
                Name = i.Name,
                Code = i.Code,
                TypeId = i.TypeId,
                TypeName = t.Name,
                TypeCode = t.Code,
                ConfigJson = i.ConfigJson,
                ParentId = i.ParentId,
                ParentName = p.Name,
                ConnectionStatus = i.ConnectionStatus,
                LastConnectTime = i.LastConnectTime,
                Status = i.Status,
                Remark = i.Remark,
                OrderNo = i.OrderNo,
                CreateTime = i.CreateTime
            })
            .ToListAsync();
    }

    /// <summary>
    /// 新增数据源实例
    /// </summary>
    [HttpPost]
    public async Task<long> Add(DataSourceInstanceInput input)
    {
        var existCode = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.Code == input.Code)
            .AnyAsync();
        if (existCode)
            throw Oops.Oh("实例编码已存在");

        var existName = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.Name == input.Name)
            .AnyAsync();
        if (existName)
            throw Oops.Oh("实例名称已存在");

        var type = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == input.TypeId)
            .FirstAsync();
        if (type == null)
            throw Oops.Oh("数据源类型不存在");

        var entity = input.Adapt<DataSourceInstance>();
        entity.CreateTime = DateTime.Now;
        entity.ConnectionStatus = false;

        var id = await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
        return id;
    }

    /// <summary>
    /// 更新数据源实例
    /// </summary>
    [HttpPost]
    public async Task Update(DataSourceInstanceUpdateInput input)
    {
        var existCode = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.Code == input.Code && i.Id != input.Id)
            .AnyAsync();
        if (existCode)
            throw Oops.Oh("实例编码已存在");

        var existName = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.Name == input.Name && i.Id != input.Id)
            .AnyAsync();
        if (existName)
            throw Oops.Oh("实例名称已存在");

        var entity = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.Id == input.Id)
            .FirstAsync();
        if (entity == null)
            throw Oops.Oh("数据源实例不存在");

        entity = input.Adapt(entity);
        entity.UpdateTime = DateTime.Now;
        entity.ConnectionStatus = false;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除数据源实例
    /// </summary>
    [HttpPost]
    public async Task Delete(BaseIdInput input)
    {
        var hasChildren = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.ParentId == input.Id)
            .AnyAsync();
        if (hasChildren)
            throw Oops.Oh("该实例下存在子实例，无法删除");

        await _db.Deleteable<DataSourceInstance>().In(input.Id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取数据源实例详情
    /// </summary>
    [HttpGet]
    public async Task<DataSourceInstanceDto> GetDetail(long id)
    {
        var result = await _db.Queryable<DataSourceInstance>()
            .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
            .LeftJoin<DataSourceInstance>((i, t, p) => i.ParentId == p.Id)
            .Where((i, t, p) => i.Id == id)
            .Select((i, t, p) => new DataSourceInstanceDto
            {
                Id = i.Id,
                Name = i.Name,
                Code = i.Code,
                TypeId = i.TypeId,
                TypeName = t.Name,
                TypeCode = t.Code,
                ConfigJson = i.ConfigJson,
                ParentId = i.ParentId,
                ParentName = p.Name,
                ConnectionStatus = i.ConnectionStatus,
                LastConnectTime = i.LastConnectTime,
                Status = i.Status,
                Remark = i.Remark,
                OrderNo = i.OrderNo,
                CreateTime = i.CreateTime
            })
            .FirstAsync();

        if (result == null)
            throw Oops.Oh("数据源实例不存在");

        return result;
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    [HttpPost]
    public async Task<TestConnectionResult> TestConnection(TestConnectionInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TestConnectionResult();

        try
        {
            string configJson;
            DataSourceType type;

            if (input.InstanceId.HasValue)
            {
                var instance = await _db.Queryable<DataSourceInstance>()
                    .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
                    .Where((i, t) => i.Id == input.InstanceId)
                    .Select((i, t) => new { Instance = i, Type = t })
                    .FirstAsync();

                if (instance == null)
                    throw new Exception("数据源实例不存在");

                configJson = instance.Instance.ConfigJson;
                type = instance.Type;
            }
            else
            {
                if (!input.TypeId.HasValue || string.IsNullOrEmpty(input.ConfigJson))
                    throw new Exception("TypeId和ConfigJson不能为空");

                type = await _db.Queryable<DataSourceType>()
                    .Where(t => t.Id == input.TypeId)
                    .FirstAsync();

                if (type == null)
                    throw new Exception("数据源类型不存在");

                configJson = input.ConfigJson;
            }

            var adapter = GetAdapter(type);
            if (adapter == null)
                throw new Exception("无法创建数据源适配器");

            var success = await adapter.TestConnectionAsync(configJson);
            stopwatch.Stop();

            result.Success = success;
            result.Message = success ? "连接成功" : "连接失败";
            result.ResponseTime = stopwatch.ElapsedMilliseconds;

            if (input.InstanceId.HasValue)
            {
                await _db.Updateable<DataSourceInstance>()
                    .SetColumns(i => new DataSourceInstance
                    {
                        ConnectionStatus = success,
                        LastConnectTime = DateTime.Now
                    })
                    .Where(i => i.Id == input.InstanceId)
                    .ExecuteCommandAsync();
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Message = "连接测试异常";
            result.ErrorDetail = ex.Message;
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            
            Log.Error("连接测试失败", ex);
        }

        return result;
    }

    /// <summary>
    /// 获取子实例列表
    /// </summary>
    [HttpGet]
    public async Task<List<DataSourceInstanceDto>> GetChildren(long parentId)
    {
        return await _db.Queryable<DataSourceInstance>()
            .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
            .Where((i, t) => i.ParentId == parentId)
            .OrderBy((i, t) => i.OrderNo)
            .ThenBy((i, t) => i.CreateTime)
            .Select((i, t) => new DataSourceInstanceDto
            {
                Id = i.Id,
                Name = i.Name,
                Code = i.Code,
                TypeId = i.TypeId,
                TypeName = t.Name,
                TypeCode = t.Code,
                ConfigJson = i.ConfigJson,
                ParentId = i.ParentId,
                ConnectionStatus = i.ConnectionStatus,
                LastConnectTime = i.LastConnectTime,
                Status = i.Status,
                Remark = i.Remark,
                OrderNo = i.OrderNo,
                CreateTime = i.CreateTime
            })
            .ToListAsync();
    }

    private IDataSourceAdapter? GetAdapter(DataSourceType type)
    {
        if (type.IsBuiltIn)
        {
            return new SqlSugarDataSourceAdapter();
        }
        else
        {
            if (string.IsNullOrEmpty(type.PluginAssembly) || string.IsNullOrEmpty(type.AdapterClassName))
                return null;

            return _pluginManager.GetAdapter(type.PluginAssembly, type.AdapterClassName);
        }
    }

    /// <summary>
    /// 导入数据源实例
    /// </summary>
    [HttpPost]
    public async Task Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw Oops.Oh("请选择导入文件");

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        
        var instances = JSON.Deserialize<List<DataSourceInstanceInput>>(json);
        if (instances == null || !instances.Any())
            throw Oops.Oh("导入文件格式错误或无数据");

        var insertList = new List<DataSourceInstance>();
        foreach (var instance in instances)
        {
            var existCode = await _db.Queryable<DataSourceInstance>()
                .Where(i => i.Code == instance.Code)
                .AnyAsync();
            var existName = await _db.Queryable<DataSourceInstance>()
                .Where(i => i.Name == instance.Name)
                .AnyAsync();
                
            if (!existCode && !existName)
            {
                var entity = instance.Adapt<DataSourceInstance>();
                entity.CreateTime = DateTime.Now;
                entity.ConnectionStatus = false;
                insertList.Add(entity);
            }
        }

        if (insertList.Any())
        {
            await _db.Insertable(insertList).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 导出数据源实例
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var list = await _db.Queryable<DataSourceInstance>()
            .ToListAsync();

        var exportData = list.Adapt<List<DataSourceInstanceInput>>();
        var json = JSON.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return new FileContentResult(bytes, "application/json")
        {
            FileDownloadName = $"datasource_instances_{DateTime.Now:yyyyMMddHHmmss}.json"
        };
    }
}