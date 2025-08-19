// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Ai.Service;

public class LLMOptionService : ITransient
{
    private readonly ILogger<LLMOptionService> _logger;
    private readonly UserManager _userManager;
    private readonly IOptions<LLMOptions> _llmOptions;
    private readonly SysCacheService _sysCacheService;

    public LLMOptionService(ILogger<LLMOptionService> logger,
        UserManager userManager,
        IOptions<LLMOptions> llmOptions,
        SysCacheService sysCacheService)
    {
        _logger = logger;
        _userManager = userManager;
        _llmOptions = llmOptions;
        _sysCacheService = sysCacheService;
    }

    /// <summary>
    /// 获取模型列表
    /// 步骤:
    /// 1. 先检查此用户在cache中是否存在模型列表
    /// 2. 如果存在，则校验此用户的模型列表是否与配置文件中的配置的厂商是否一致
    /// 3. 如果一致，则返回此用户的模型列表（配置文件中的模型列表没有更新，则不会影响此用户）
    /// 4. 如果不一致(因为可能配置文件中的模型列表有更新)，则在cache中删除此用户的模型列表，并重新生成模型列表，并返回新的模型列表
    /// 5. 如果cache中不存在此用户的模型列表，则直接返回配置文件中的默认模型列表
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<ModelListOutput> GetModelListAsync()
    {
        var userId = _userManager.UserId;
        if (userId == default)
        {
            throw new Exception("用户不存在");
        }
        var key = $"admin_net_llm_chat_config:{userId}";

        var cache = _sysCacheService.Get<ModelListOutput>(key);
        if (cache != null)
        {
            if (cache.ProviderName == _llmOptions.Value.ModelProvider)
            {
                return await _CheckAndUpdateModelList(cache, key);
            }
            else
            {
                _sysCacheService.Remove(key);
            }
        }
        var output = _GetInitModelList();
        _sysCacheService.Set(key, output);
        return await Task.FromResult(output);
    }

    /// <summary>
    /// 可能存在用户手动修改了配置文件，如删除、添加、修改模型，此时需要更新缓存中的模型列表
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private Task<ModelListOutput> _CheckAndUpdateModelList(ModelListOutput cache, string key)
    {
        //检查cache中的当前模型是否在配置文件中存在
        var currentProvider = _llmOptions.Value.Providers.FirstOrDefault(it => it.ProductName == cache.ProviderName);
        if (currentProvider == null)
        {
            var output = _GetInitModelList();
            _sysCacheService.Set(key, output);
            return Task.FromResult(output);
        }
        if (!currentProvider.ChatCompletion.SupportModelIds.Contains(cache.CurrentModel))
        {
            var output = _GetInitModelList();
            _sysCacheService.Set(key, output);
            return Task.FromResult(output);
        }

        cache.Models = currentProvider.ChatCompletion.SupportModelIds.Select(it => new ModelListOutputItem
        {
            ModelName = it,
            ProviderName = cache.ProviderName,
        }).ToList();
        _sysCacheService.Set(key, cache);

        return Task.FromResult(cache);
    }

    /// <summary>
    /// 得到初始化模型列表
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private ModelListOutput _GetInitModelList()
    {
        var currentModel = _llmOptions.Value.Providers.FirstOrDefault(it => it.ProductName == _llmOptions.Value.ModelProvider);
        if (currentModel == null)
        {
            throw new Exception("当前模型不存在,或者配置文件有错误！");
        }
        return new ModelListOutput
        {
            ProviderName = _llmOptions.Value.ModelProvider,
            CurrentModel = currentModel.ChatCompletion.ModelId,
            UserCanSwitchLLM = _llmOptions.Value.UserCanSwitchLLM,
            Models = currentModel.ChatCompletion.SupportModelIds.Select(it => new ModelListOutputItem
            {
                ModelName = it,
                ProviderName = _llmOptions.Value.ModelProvider,
            }).ToList(),
        };
    }

    /// <summary>
    /// 切换模型
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> ChangeModelAsync(ModelListChangeInput input)
    {
        var userId = _userManager.UserId;
        if (userId == default)
        {
            throw new Exception("用户不存在");
        }
        var key = $"admin_net_llm_chat_config:{userId}";
        var cache = _sysCacheService.Get<ModelListOutput>(key);
        if (cache == null)
        {
            return await _SetNewModelList(input, key);
        }
        return await _UpdateModelList(input, cache, key);
    }

    private async Task<bool> _SetNewModelList(ModelListChangeInput input, string key)
    {
        var currentModel = _llmOptions.Value.Providers.FirstOrDefault(it => it.ProductName == input.ProviderName);
        if (currentModel == null)
        {
            throw new Exception("当前模型不存在,或者配置文件有错误！");
        }
        var model = new ModelListOutput
        {
            ProviderName = input.ProviderName,
            CurrentModel = input.ModelName,
            Models = currentModel.ChatCompletion.SupportModelIds.Select(it => new ModelListOutputItem
            {
                ModelName = it,
                ProviderName = input.ProviderName,
            }).ToList(),
        };
        _sysCacheService.Set(key, model);
        return await Task.FromResult(true);
    }

    private async Task<bool> _UpdateModelList(ModelListChangeInput input, ModelListOutput cache, string key)
    {
        var currentModel = _llmOptions.Value.Providers.FirstOrDefault(it => it.ProductName == input.ProviderName);
        if (currentModel == null)
        {
            throw new Exception("当前模型不存在,或者配置文件有错误！");
        }
        if (!currentModel.ChatCompletion.SupportModelIds.Contains(input.ModelName))
        {
            throw new Exception("模型不存在");
        }
        cache.CurrentModel = input.ModelName;
        _sysCacheService.Set(key, cache);
        return await Task.FromResult(true);
    }
}