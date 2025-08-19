# Copilot 指令（Furion + SqlSugar + .NET 9；对齐 Admin.NET 风格）

## 目标与技术栈
- 运行环境：.NET 9（C# 12，启用 Nullable）。
- 框架：Furion（API 模板结构）；ORM：SqlSugarCore。
- 参考项目（只读）：`Admin.NET.*`（已加入解决方案仅供参考，严禁修改/移动/提交）。
- 目标项目：`DataProcess.DataSource.*`（进行全部实现与测试）。

## 分层与目录
- 结构遵循：公共/核心放 `*.Core`，业务放 `*.Application`，Web Host 为 `*.Web.Entry`，如需共享契约可置于 `*.Web.Core`。
- Application 层：
  - `Entity/`：SqlSugar 实体（带表映射、索引、约束）。
  - `Service/`：**动态 API 服务类**（不使用传统 Controller），按 `XxxService` 命名，必要时 `Service/Dto/`。
  - `Configuration/Database.json`、`Configuration/CodeGen.json` 等配置使用分文件管理。
- 禁止创建 `Controllers/` 及 Controller 类；统一通过服务类暴露 API。

## 编码规范
- 异步优先；日志用 Furion Logging；统一异常处理中间件。
- DTO 与实体分离；接口返回统一响应模型；开启 Swagger/OpenAPI 并按模块分组。
- 数据访问：仓储与服务均使用 `ISqlSugarClient`；禁止直接使用 ADO.NET；禁止引入 EF/AutoMapper。

## 数据源服务特性（与设计文档一致）
- **模块一：数据源类型管理**：类型以**插件**形式扩展，支持上传 ZIP → 解压 → 动态发现/注册/卸载；维护类型元信息与参数模板。
- **模块二：数据源管理**：基于类型创建“数据源实例”，支持增删改查、连接测试、导入导出、父子继承等。
- **统一查询/条件 DSL**：所有数据操作走统一参数体系（如 gt、lt、and、or、join 等），屏蔽底层差异。
- **CodeFirst**：按 `Configuration/Database.json` 指定数据库，启动时自动建表并**注入初始数据**（包含 SqlSugar 支持的所有数据源类型作为内置类型）。
- **热插拔**：新 ZIP 上传后即可用（重载插件目录 & 反射加载），不需重启服务。

## 命令白名单（Agent 仅能运行这些）
- `dotnet restore`
- `dotnet build -c Release`
- `dotnet test -c Release`
- `dotnet format`

## 路径/修改范围
- 允许修改：`DataProcess.DataSource.*`、其下 `Configuration/`、`plugins/datasource/**`（运行时目录）。
- 只读且禁止修改：`Admin.NET.*`、`/Common/**`。

## 交付验收（最小）
- 服务启动后：
  - `GET /api/datasource/types` 返回 200，含内置类型（来自种子数据）。
  - 上传一个模拟 ZIP（空实现的适配器）后，`/api/datasource/types` 能看到新类型。
  - 创建数据源实例并点击“连接
