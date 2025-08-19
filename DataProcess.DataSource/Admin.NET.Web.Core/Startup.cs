// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Admin.NET.Core;
using Admin.NET.Core.Service;
using AspNetCoreRateLimit;
using Furion;
using Furion.Logging;
using Furion.SpecificationDocument;
using Furion.VirtualFileServer;
using IGeekFan.AspNetCore.Knife4jUI;
using IPTools.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OnceMi.AspNetCore.OSS;
using RabbitMQ.Client;
using SixLabors.ImageSharp.Web.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Admin.NET.Web.Core;

[AppStartup(int.MaxValue)]
public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 配置选项
        services.AddProjectOptions();

        // 缓存注册
        services.AddCache();

        // JWT
        services.AddJwt<JwtHandler>(enableGlobalAuthorize: true, jwtBearerConfigure: options =>
        {
            // 实现 JWT 身份验证过程控制
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var httpContext = context.HttpContext;
                    // 若请求 Url 包含 token 参数，则设置 Token 值
                    if (httpContext.Request.Query.ContainsKey("token"))
                        context.Token = httpContext.Request.Query["token"];
                    // 若请求 Url 包含 access_token 参数，则设置 Token 值
                    // 兼容 SignalR 通过 accessTokenFactory 传入的 token 值 .withUrl("/hubs/xxx", { accessTokenFactory: () => this.loginToken })
                    if (httpContext.Request.Query.ContainsKey("access_token"))
                        context.Token = httpContext.Request.Query["access_token"];
                    return Task.CompletedTask;
                }
            };
        }).AddSignatureAuthentication(options => // 添加 Signature 身份验证
        {
            options.Events = SysOpenAccessService.GetSignatureAuthenticationEventImpl();
        });

        // 允许跨域
        services.AddCorsAccessor();

        // 远程请求
        services.AddHttpRemote();

        // 任务队列
        services.AddTaskQueue(builder =>
        {
            builder.NumRetries = 0; // 默认重试 0 次
                                    //builder.RetryTimeout = 1000; // 每次重试间隔 1000ms

            // 订阅 TaskQueue 意外未捕获异常
            builder.UnobservedTaskExceptionHandler = (obj, args) =>
            {
                Log.Error($"任务队列异常：{args.Exception?.Message}", args.Exception);
            };
        });

        // 作业调度
        services.AddSchedule(options =>
        {
            if (App.GetConfig<bool>("JobSchedule:Enabled", true))
            {
                options.LogEnabled = false; // 是否输出作业调度器日志
                options.AddPersistence<DbJobPersistence>(); // 添加作业持久化器
                options.AddMonitor<JobMonitor>(); // 添加作业执行监视器
                //// 定义未捕获的异常
                //options.UnobservedTaskExceptionHandler = (obj, args) =>
                //{
                //    Log.Error($"作业调度异常：{args.Exception?.Message}", args.Exception);
                //};
            }
        });

        // 脱敏检测
        services.AddSensitiveDetection();

        // 动态 WebAPI
        services.AddControllersWithViews()
            .AddAppLocalization(settings => { services.AddJsonLocalization(options => options.ResourcesPath = settings.ResourcesPath); }) // 集成第三方多语言配置
            .AddNewtonsoftJson(options => SetNewtonsoftJsonSetting(options.SerializerSettings))
            //.AddXmlSerializerFormatters()
            //.AddXmlDataContractSerializerFormatters()
            .AddInjectWithUnifyResult<AdminNETResultProvider>()
            .AddJsonOptions(options =>
            {
                //options.JsonSerializerOptions.PropertyNamingPolicy = null; // 序列化属性名大写（属性原样输出）
                options.JsonSerializerOptions.Converters.AddDateTimeTypeConverters("yyyy-MM-dd HH:mm:ss", localized: true); // 时间格式化
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // 忽略循环引用
                options.JsonSerializerOptions.IncludeFields = true; // 包含成员字段序列化
                options.JsonSerializerOptions.AllowTrailingCommas = true; // 允许尾随逗号
                options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip; // 允许注释
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All); // 禁止 Unicode 转码
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping; // 处理（中文）乱码问题
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // 不区分大小写
                //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull; // 忽略所有 null 属性
                //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault; // 忽略所有默认值属性
                options.JsonSerializerOptions.WriteIndented = true; // JSON 字符串缩进
                options.JsonSerializerOptions.Converters.AddLongTypeConverters(overMaxLengthOf17: true); // long 类型序列化时转 string（超过 17 位再转换）
                //options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString; // String 转 Number
                //options.JsonSerializerOptions.Converters.Add(new StringJsonConverter()); // Number 和 Boolean 转 String
                options.JsonSerializerOptions.Converters.AddDateOnlyConverters(); // DateOnly
                options.JsonSerializerOptions.Converters.AddTimeOnlyConverters(); // TimeOnly
                options.JsonSerializerOptions.Converters.AddClayConverters(); // 粘土对象 Clay 类型序列化支持
            });

        // NewtonsoftJson 序列化设置
        static void SetNewtonsoftJsonSetting(JsonSerializerSettings setting)
        {
            //setting.ContractResolver = new DefaultContractResolver(); // 序列化属性名大写（属性原样输出）
            setting.DateFormatString = "yyyy-MM-dd HH:mm:ss"; // 时间格式化
            //setting.Converters.AddDateTimeTypeConverters("yyyy-MM-dd HH:mm:ss", localized: true); // 时间格式化
            setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; // 忽略循环引用
            //setting.NullValueHandling = NullValueHandling.Ignore; // 忽略所有 null 属性
            //setting.DefaultValueHandling = DefaultValueHandling.Ignore; // 忽略所有默认值属性
            setting.ContractResolver = new CamelCasePropertyNamesContractResolver(); // 解决动态对象属性名大写问题
            setting.Formatting = Formatting.Indented; // JSON 字符串缩进
            setting.Converters.AddLongTypeConverters(overMaxLengthOf17: true); // long 类型序列化时转 string（超过 17 位再转换）
            setting.Converters.AddDateOnlyConverters(); // DateOnly
            setting.Converters.AddTimeOnlyConverters(); // TimeOnly
            setting.Converters.AddClayConverters(); // 粘土对象 Clay 类型序列化支持
            // DateTimeOffset 反序列化异常
            setting.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
            setting.DateParseHandling = DateParseHandling.None;
            setting.Converters.Add(new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal });
        }

        // 增加字典Key小写处理序列化配置选项
        var camelCaseDictionaryKeyContractJsonSetting = new JsonSerializerSettings();
        SetNewtonsoftJsonSetting(camelCaseDictionaryKeyContractJsonSetting);
        camelCaseDictionaryKeyContractJsonSetting.ContractResolver = new CamelCasePropertyNamesContractResolver(); // 首字母小写（驼峰样式），字典 Key 也会转换为驼峰样式
        services.AddUnifyJsonOptions("CamelCaseDictionaryKey", camelCaseDictionaryKeyContractJsonSetting);

        // SqlSugar
        services.AddSqlSugar();

        // 注册LLM大模型
        services.AddLLMFactory(); // 注册LLM模型工厂,如果需要切换模型，请引入ILLMFactory接口，调用CreateKernel方法获取Kernel实例
        services.AddLLM();  // 注册LLM大模型,注意：此扩展不能切换模型，只能使用一个模型

        // 三方授权登录OAuth
        services.AddOAuth();

        // ElasticSearch
        services.AddElasticSearch();

        // 配置Nginx转发获取客户端真实IP
        // 注1：如果负载均衡不是在本机通过 Loopback 地址转发请求的，一定要加上options.KnownNetworks.Clear()和options.KnownProxies.Clear()
        // 注2：如果设置环境变量 ASPNETCORE_FORWARDEDHEADERS_ENABLED 为 True，则不需要下面的配置代码
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // 限流服务
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        // 事件总线
        services.AddEventBus(options =>
        {
            options.UseUtcTimestamp = false;
            // 不启用事件日志
            options.LogEnabled = false;
            // 事件执行器（失败重试）
            options.AddExecutor<RetryEventHandlerExecutor>();
            // 订阅事件意外未捕获异常
            options.UnobservedTaskExceptionHandler = (obj, args) =>
            {
                if (args.Exception?.Message != null)
                    Log.Error($"事件总线未处理异常 ：{args.Exception?.Message} ", args.Exception);
            };
            //// 事件执行监视器（执行之前、执行之后、执行异常）
            //options.AddMonitor<EventHandlerMonitor>();

            // 自定义事件源存储器
            var eventBusOpt = App.GetConfig<EventBusOptions>("EventBus", true);
            if (eventBusOpt.EventSourceType == "Redis")
            {
                // Redis消息队列
                if (App.GetConfig<CacheOptions>("Cache", true).CacheType == CacheTypeEnum.Redis.ToString())
                {
                    options.ReplaceStorerOrFallback(serviceProvider =>
                    {
                        var cacheProvider = serviceProvider.GetRequiredService<NewLife.Caching.ICacheProvider>();
                        return new RedisEventSourceStorer(cacheProvider, "adminnet_eventsource_queue", 3000);
                    });
                }
            }
            else if (eventBusOpt.EventSourceType == "RabbitMQ")
            {
                // RabbitMQ消息队列
                var rbmqEventSourceStorer = new RabbitMQEventSourceStore(new ConnectionFactory
                {
                    UserName = eventBusOpt.RabbitMQ.UserName,
                    Password = eventBusOpt.RabbitMQ.Password,
                    HostName = eventBusOpt.RabbitMQ.HostName,
                    Port = eventBusOpt.RabbitMQ.Port
                }, "adminnet_eventsource_queue", 3000);
                options.ReplaceStorerOrFallback(serviceProvider => { return rbmqEventSourceStorer; });
            }
        });

        // 图像处理
        services.AddImageSharp();

        // OSS对象存储
        var ossOpt = App.GetConfig<OSSProviderOptions>("OSSProvider", true);
        services.AddOSSService(Enum.GetName(ossOpt.Provider), "OSSProvider");

        // 模板引擎
        services.AddViewEngine();

        // 即时通讯
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15); // 服务器端向客户端ping的间隔
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // 客户端向服务器端ping的间隔
            options.MaximumReceiveMessageSize = 1024 * 1014 * 10; // 数据包大小10M，默认最大为32K
        }).AddNewtonsoftJsonProtocol(options => SetNewtonsoftJsonSetting(options.PayloadSerializerSettings));

        // 系统日志
        services.AddLoggingSetup();

        // 验证码
        services.AddCaptcha();

        // 控制台logo
        services.AddConsoleLogo();

        // Swagger 时间格式化
        services.AddSwaggerGen(u =>
        {
            u.MapType<DateTime>(() => new Microsoft.OpenApi.Models.OpenApiSchema
            {
                Type = "string",
                Format = "date-time",
                Example = new Microsoft.OpenApi.Any.OpenApiString(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            });
        });

        // 将IP地址数据库文件完全加载到内存，提升查询速度（以空间换时间，内存将会增加60-70M）
        IpToolSettings.LoadInternationalDbToMemory = true;
        // 设置默认查询器China和International
        //IpToolSettings.DefalutSearcherType = IpSearcherType.China;
        IpToolSettings.DefalutSearcherType = IpSearcherType.International;

        // 配置gzip与br的压缩等级为最优
        //services.Configure<BrotliCompressionProviderOptions>(options =>
        //{
        //    options.Level = CompressionLevel.Optimal;
        //});
        //services.Configure<GzipCompressionProviderOptions>(options =>
        //{
        //    options.Level = CompressionLevel.Optimal;
        //});
        // 注册压缩响应
        services.AddResponseCompression((options) =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "text/html; charset=utf-8",
                "application/xhtml+xml",
                "application/atom+xml",
                "image/svg+xml"
            ]);
        });

        // 注册虚拟文件系统服务
        services.AddVirtualFileServer();

        // 注册启动执行任务
        services.AddHostedService<StartHostedService>();
        services.AddHostedService<MqttHostedService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 响应压缩
        app.UseResponseCompression();

        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            // 隐藏服务器信息
            context.Response.Headers.Append("Server", "none");
            // 防止浏览器 MIME 类型嗅探，确保内容按照声明的类型处理
            //context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            // 防止点击劫持，确保页面内容不被其他页面覆盖
            // DENY：表示该页面不允许在 frame 中展示，即便是在相同域名的页面中嵌套也不允许
            // SAMEORIGIN：表示该页面可以在相同域名页面的 frame 中展示
            // ALLOW-FROM uri：表示该页面可以在指定来源的 frame 中展示
            //context.Response.Headers.Append("X-Frame-Options", "ALLOW-FROM " + App.GetConfig<string>("Urls", true));
            // 启用 XSS 保护，防止跨站脚本注入
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            // 控制在请求中发送的来源信息，减少潜在的隐私泄露
            context.Response.Headers.Append("Referrer-Policy", "no-referrer");
            // 防止 Internet Explorer 在下载文件时自动打开，降低恶意文件执行的风险
            context.Response.Headers.Append("X-Download-Options", "noopen");
            // 限制 Flash 和其他插件的跨域访问，防止数据泄露
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
            // 限制可执行的脚本和样式，降低 XSS 攻击的风险
            context.Response.Headers.Append("Content-Security-Policy", "style-src 'self' 'unsafe-inline';");
            // 允许浏览器使用地理位置 API，但仅限于当前站点
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(self)");
            // 强制使用 HTTPS，防止中间人攻击
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
            // 隐藏服务器端技术栈
            context.Response.Headers.Append("X-Powered-By", "Admin.NET");
            // 移除特性响应头
            context.Response.Headers.Remove("Furion");
            // 添加自定义响应头
            context.Response.Headers.Append("Admin.NET", "v2.0.0");
            await next();
        });

        // 图像处理
        app.UseImageSharp();

        // 特定文件类型（文件后缀）处理
        var contentTypeProvider = FS.GetFileExtensionContentTypeProvider();
        // contentTypeProvider.Mappings[".文件后缀"] = "MIME 类型";
        var cpMappings = App.GetConfig<Dictionary<string, string>>("StaticContentTypeMappings");
        if (cpMappings != null)
        {
            if (cpMappings.TryGetValue(".*", out string value))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "wwwroot")),
                    // RequestPath = "/static",
                    ServeUnknownFileTypes = true, // 允许服务未知文件类型，以便能处理.dll这种非默认的静态文件类型
                    // DefaultContentType = "application/octet-stream" // 为未知文件类型设置一个通用的内容类型
                    DefaultContentType = value
                });
            }
            else
            {
                foreach (var key in cpMappings.Keys)
                    contentTypeProvider.Mappings[key] = cpMappings[key];
                app.UseStaticFiles(new StaticFileOptions
                {
                    ContentTypeProvider = contentTypeProvider
                });
            }
        }
        else
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = contentTypeProvider
            });
        }

        //// 二级目录文件路径解析
        //if (!string.IsNullOrEmpty(App.Settings.VirtualPath))
        //{
        //    app.UseStaticFiles(new StaticFileOptions
        //    {
        //        RequestPath = App.Settings.VirtualPath,
        //        FileProvider = App.WebHostEnvironment.WebRootFileProvider
        //    });
        //}

        //// 启用HTTPS
        //app.UseHttpsRedirection();

        // 启用OAuth
        app.UseOAuth();

        // 添加状态码拦截中间件
        app.UseUnifyResultStatusCodes();

        // 启用多语言，必须在 UseRouting 之前
        app.UseAppLocalization();

        // 路由注册
        app.UseRouting();

        // 启用跨域，必须在 UseRouting 和 UseAuthentication 之间注册
        app.UseCorsAccessor();

        // 启用鉴权授权
        app.UseAuthentication();
        app.UseAuthorization();

        // 限流组件（在跨域之后）
        app.UseIpRateLimiting();
        app.UseClientRateLimiting();
        app.UsePolicyRateLimit();

        // 任务调度看板
        app.UseScheduleUI(options =>
        {
            options.RequestPath = "/schedule"; // 必须以 / 开头且不以 / 结尾
            options.DisableOnProduction = false; // 是否在生产环境中关闭
            options.DisplayEmptyTriggerJobs = true; // 是否显示空作业触发器的作业
            options.DisplayHead = false; // 是否显示页头
            options.DefaultExpandAllJobs = false; // 是否默认展开所有作业
            options.EnableDirectoryBrowsing = false; // 是否启用目录浏览
            options.Title = "定时任务看板"; // 自定义看板标题

            options.LoginConfig.OnLoging = async (username, password, httpContext) =>
            {
                var res = await httpContext.RequestServices.GetRequiredService<SysAuthService>().SwaggerSubmitUrl(new SpecificationAuth { UserName = username, Password = password });
                return res == 200;
            };
            options.LoginConfig.DefaultUsername = "";
            options.LoginConfig.DefaultPassword = "";
            options.LoginConfig.SessionKey = "schedule_session_key"; // 登录客户端存储的 Session 键
        });

        // 配置Swagger-Knife4UI（路由前缀一致代表独立，不同则代表共存）
        app.UseKnife4UI(options =>
        {
            options.RoutePrefix = "kapi";
            options.ConfigObject.DisplayOperationId = true;
            options.ConfigObject.DisplayRequestDuration = true;
            foreach (var groupInfo in SpecificationDocumentBuilder.GetOpenApiGroups())
            {
                // 兼容二级虚拟目录转发（配置二级域名转发，需要 Swagger.json 的 ServerDir 配置项）
                options.SwaggerEndpoint(string.Concat("..", groupInfo.RouteTemplate.AsSpan(groupInfo.RouteTemplate.IndexOf("/swagger/"))), groupInfo.Title);
            }
        });

        app.UseInject(string.Empty, options =>
        {
            // 控制模型（Schema）的默认展开深度，-1完全折叠
            options.ConfigureSwaggerUI(ui =>
            {
                ui.DefaultModelExpandDepth(10);
            });

            foreach (var groupInfo in SpecificationDocumentBuilder.GetOpenApiGroups())
            {
                var warning = "不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！";
                groupInfo.Description += $"<br/><u><b><font color='FF0000'> 👮{warning}</font></b></u>";
            }
        });

        var mqttOptions = App.GetConfig<MqttOptions>("Mqtt", true);
        app.UseEndpoints(endpoints =>
        {
            // 注册集线器
            endpoints.MapHubs();
            // 注册路由
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            // 注册 MQTT 支持 WebSocket
            if (mqttOptions.Enabled)
            {
                endpoints.MapConnectionHandler<MqttConnectionHandler>("/mqtt",
                    httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                        protocolList => protocolList.FirstOrDefault() ?? string.Empty);
            }
        });
    }
}