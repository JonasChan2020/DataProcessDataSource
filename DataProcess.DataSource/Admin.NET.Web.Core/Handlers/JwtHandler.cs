// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Admin.NET.Core;
using Admin.NET.Core.Service;
using Furion;
using Furion.Authorization;
using Furion.DataEncryption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.NET.Web.Core
{
    public class JwtHandler : AppAuthorizeHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public JwtHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 令牌/Token校验核心逻辑
        /// </summary>
        /// <param name="context"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public override async Task HandleAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
        {
            using var serviceScope = _serviceProvider.CreateScope();
            var sysCacheService = serviceScope.ServiceProvider.GetRequiredService<SysCacheService>();

            var sysConfigService = serviceScope.ServiceProvider.GetRequiredService<SysConfigService>();
            var tokenExpire = await sysConfigService.GetTokenExpire();
            var refreshTokenExpire = await sysConfigService.GetRefreshTokenExpire();
            if (JWTEncryption.AutoRefreshToken(context, context.GetCurrentHttpContext(), tokenExpire, refreshTokenExpire))
            {
                await AuthorizeHandleAsync(context);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, "登录已过期，请重新登录。"));
                var currentHttpContext = context.GetCurrentHttpContext();
                // 跳过签名 SignatureAuthentication 引发的失败
                if (currentHttpContext.Items.ContainsKey(SignatureAuthenticationDefaults.AuthenticateFailMsgKey)) return;
                currentHttpContext.SignoutToSwagger();
                return;
            }

            // 验证Token黑名单
            var accessToken = httpContext.Request.Headers.Authorization.ToString();
            if (sysCacheService.ExistKey($"{CacheConst.KeyTokenBlacklist}:{accessToken}"))
            {
                context.Fail(new AuthorizationFailureReason(this, "令牌已失效，请重新登录。"));
                context.GetCurrentHttpContext().SignoutToSwagger();
                return;
            }

            // 验证Token版本号
            var userId = httpContext.User.FindFirst(ClaimConst.UserId)?.Value;
            var tokenVersion1 = httpContext.User.FindFirst(ClaimConst.TokenVersion)?.Value;
            var tokenVersion2 = sysCacheService.Get<string>($"{CacheConst.KeyUserToken}{userId}");
            if (string.IsNullOrWhiteSpace(tokenVersion2) && !string.IsNullOrWhiteSpace(userId))
            {
                // 查库并缓存用户Token版本
                var user = await serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>().Queryable<SysUser>().FirstAsync(u => u.Id == long.Parse(userId));
                if (user == null || user.Status == StatusEnum.Disable)
                {
                    context.Fail(new AuthorizationFailureReason(this, "账号不存在或已被停用，请联系管理员。"));
                    context.GetCurrentHttpContext().SignoutToSwagger();
                    return;
                }
                sysCacheService.Set($"{CacheConst.KeyUserToken}{user.Id}", $"{user.TokenVersion}");
                tokenVersion2 = user.TokenVersion.ToString();
            }
            if (string.IsNullOrWhiteSpace(tokenVersion1) || tokenVersion1 != tokenVersion2)
            {
                context.Fail(new AuthorizationFailureReason(this, "令牌已失效，请重新登录。"));
                context.GetCurrentHttpContext().SignoutToSwagger();
                return;
            }

            // 验证租户有效期
            var tenantId = httpContext.User.FindFirst(ClaimConst.TenantId)?.Value;
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var tenant = sysCacheService.Get<List<SysTenant>>(CacheConst.KeyTenant)?.FirstOrDefault(u => u.Id == long.Parse(tenantId));
                if (tenant != null && tenant.ExpirationTime != null && DateTime.Now > tenant.ExpirationTime)
                {
                    context.Fail(new AuthorizationFailureReason(this, "租户已过期，请联系管理员。"));
                    context.GetCurrentHttpContext().SignoutToSwagger();
                }
            }
        }

        public override async Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
        {
            // 已自动验证 Jwt Token 有效性
            return await CheckAuthorizeAsync(httpContext);
        }

        /// <summary>
        /// 权限校验核心逻辑
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        private static async Task<bool> CheckAuthorizeAsync(DefaultHttpContext httpContext)
        {
            // 排除超管权限判断
            if (App.User.FindFirst(ClaimConst.AccountType)?.Value == ((int)AccountTypeEnum.SuperAdmin).ToString())
                return true;

            var serviceScope = httpContext.RequestServices.CreateScope();

            // 当前接口路由
            var path = httpContext.Request.Path.ToString();

            // 移动端接口权限判断
            if (App.User.FindFirst(ClaimConst.LoginMode)?.Value == ((int)LoginModeEnum.APP).ToString())
            {
                var appApiList = serviceScope.ServiceProvider.GetRequiredService<SysCommonService>().GetAppApiList();
                return appApiList.Exists(u => path.EndsWith(u, StringComparison.CurrentCultureIgnoreCase));
            }

            // 获取当前用户按钮权限集合和接口黑名单
            var sysRoleService = serviceScope.ServiceProvider.GetRequiredService<SysRoleService>();
            var roleApis = await sysRoleService.GetUserApiList();

            // 若当前路由在按钮权限集合里面则放行
            if (roleApis[0].Exists(u => path.EndsWith(u, StringComparison.CurrentCultureIgnoreCase)))
                return true;

            // 若当前路由在已接口黑名单里面则禁止
            return roleApis[1].TrueForAll(u => !path.EndsWith(u, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}