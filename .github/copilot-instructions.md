# 我们的固定约束（Furion + SqlSugar + .NET 9）
- 运行环境：.NET 9（`<TargetFramework>net9.0</TargetFramework>`），C# 12。
- 框架：后端采用 Furion（API 模板结构），ORM 统一使用 SqlSugar，不使用 EF。
- 结构：依据fuion框架规则，公共、核心的内容放在core项目中，业务相关功能放在application项目中，配置文件统一放在application下建立的Configuration文件夹下面，以不同的用途进行文件划分，比如数据库连接配置放在Database.json文件中，代码生成的配置放在CodeGen.json文件中。application项目中Entity文件夹中存放数据表实体，Service文件夹中存放类似“LLMTestService”这样的代码文件，不需要建立XXXcontroller代码文件，所有的api接口服务都写在XXXServices这样的文件中，必要的时候可以在Service文件夹下增加Dto文件夹，用于存放所需的操作实体。
- 代码规范：异步优先、可空启用、日志用 Furion Logging；单元测试用 xUnit。
- 依赖：NuGet 首选稳定版，优先添加 `Furion`, `SqlSugarCore` 等必要包。
- 构建/测试指令（允许执行）：`dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`, `dotnet format`.
- 禁止事项：不得引入 EF/AutoMapper；不需要使用controller文件夹和文件。
- 代码书写方式：Service下的所有api代码中都是基于furion框架的方式，类似于如下的代码结构：
[ApiDescriptionSettings(Name = "LLMTest", Description = "LLM测试,不可以切换模型")]
public class LLMTestService : IDynamicApiController, ITransient
每个api类通过继承“IDynamicApiController, ITransient”
每个接口方法按以下格式，不用添加httpget,httppost这种。
  /// <summary>
  /// 获取作业分页列表 ⏰
  /// </summary>
  [DisplayName("获取作业分页列表")]
  public async Task<SqlSugarPagedList<JobDetailOutput>> PageJobDetail(PageJobDetailInput input)
