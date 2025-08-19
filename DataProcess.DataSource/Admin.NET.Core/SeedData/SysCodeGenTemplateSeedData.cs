// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.SeedData;

/// <summary>
/// 代码生成模板配置表 表种子数据
/// </summary>
[IgnoreUpdateSeed]
public class SysCodeGenTemplateSeedData : ISqlSugarEntitySeedData<SysCodeGenTemplate>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysCodeGenTemplate> HasData()
    {
        string recordList = @"
            [
			  {
			    ""Name"": ""web_api.ts.vm"",
			    ""Type"": 1,
			    ""SysFlag"": 1,
			    ""IsDefault"": false,
			    ""OutputFile"": ""api/{PagePath}/{TableNameLower}.ts"",
			    ""Describe"": ""(WEB)接口请求"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000101
			  },
			  {
			    ""Name"": ""web_views_index.vue.vm"",
			    ""Type"": 1,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""views/{PagePath}/{TableNameLower}/index.vue"",
			    ""Describe"": ""(WEB)前端页面"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000111
			  },
			  {
			    ""Name"": ""web_views_List.vue.vm"",
			    ""Type"": 1,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""views/{PagePath}/{TableNameLower}/component/list.vue"",
			    ""Describe"": ""(WEB)表格组件"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 21:25:43"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000121
			  },
			  {
			    ""Name"": ""web_views_editDialog.vue.vm"",
			    ""Type"": 1,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""views/{PagePath}/{TableNameLower}/component/edit.vue"",
			    ""Describe"": ""(WEB)编辑对话框"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000131
			  },
			  {
			    ""Name"": ""service_Service.cs.vm"",
			    ""Type"": 2,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""Service/{TableName}/{TableName}Service.cs"",
			    ""Describe"": ""(服务端)接口服务"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000141
			  },
			  {
			    ""Name"": ""service_InputDto.cs.vm"",
			    ""Type"": 2,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""Service/{TableName}/Dto/{TableName}Input.cs"",
			    ""Describe"": ""(服务端)输入参数"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000151
			  },
			  {
			    ""Name"": ""service_OutputDto.cs.vm"",
			    ""Type"": 2,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""Service/{TableName}/Dto/{TableName}Output.cs"",
			    ""Describe"": ""(服务端)输出参数"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000161
			  },
			  {
			    ""Name"": ""sys_menu_seed_data.cs.vm"",
			    ""Type"": 3,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""SeedData/{TableName}MenuSeedData.cs"",
			    ""Describe"": ""(服务端)菜单种子数据"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000171
			  },
			  {
			    ""Name"": ""web_views_el_table_index.vue.vm"",
			    ""Type"": 1,
			    ""SysFlag"": 1,
			    ""IsDefault"": false,
			    ""OutputFile"": ""views/{PagePath}/{TableNameLower}/index.vue"",
			    ""Describe"": ""(WEB)前端页面,基于el-table（和默认的vxetable互斥）"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000181
			  },
			  {
			    ""Name"": ""PartialEntity_Entity.cs.vm"",
			    ""Type"": 2,
			    ""SysFlag"": 1,
			    ""IsDefault"": false,
			    ""OutputFile"": ""PartialEntity/{TableName}Entity.cs"",
			    ""Describe"": ""(实体扩展)Partial实体"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 21:25:43"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000191
			  },
			  {
			    ""Name"": ""PartialService_Service.cs.vm"",
			    ""Type"": 2,
			    ""SysFlag"": 1,
			    ""IsDefault"": false,
			    ""OutputFile"": ""PartialService/{TableName}/{TableName}Service.cs"",
			    ""Describe"": ""(服务扩展)Partial服务"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 21:25:43"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000201
			  },
			  {
			    ""Name"": ""web_views_Tree.vue.vm"",
			    ""Type"": 1,
			    ""SysFlag"": 1,
			    ""IsDefault"": false,
			    ""OutputFile"": ""views/{PagePath}/{TableNameLower}/component/{TableNameLower}Tree.vue"",
			    ""Describe"": ""(WEB)左边树型组件"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 21:25:43"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000211
			  },
			  {
			    ""Name"": ""service_Mid.cs.vm"",
			    ""Type"": 2,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""Service/{TableName}/Dto/{TableName}Mid.cs"",
			    ""Describe"": ""(服务端)中间件"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 21:25:43"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000215
			  },
				{
			    ""Name"": ""web_views_language.ts.vm"",
			    ""Type"": 1,
			    ""SysFlag"": 1,
			    ""IsDefault"": true,
			    ""OutputFile"": ""i18n/pages/{TableNameLower}/zh-CN.ts"",
			    ""Describe"": ""(WEB)多语言模板,默认数据库中文备注,其他语言自己用AI替换"",
			    ""OrderNo"": 100,
			    ""CreateTime"": ""1900-01-01 00:00:00"",
			    ""UpdateTime"": ""2025-01-13 23:02:29"",
			    ""CreateUserId"": null,
			    ""CreateUserName"": null,
			    ""UpdateUserId"": null,
			    ""UpdateUserName"": null,
			    ""IsDelete"": false,
			    ""Id"": 1300000000216
			  },
			]
        ";
        var records = JSON.Deserialize<List<SysCodeGenTemplate>>(recordList);
        return records;
    }
}