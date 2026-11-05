using System;
using System.Collections.Generic;
using GrapeCity.Forguncy.ServerApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace XyLink.Server
{
    public class XyLinkMiddlewareInjector : MiddlewareInjector
    {
        public override List<ServiceItem> ConfigureServices(List<ServiceItem> serviceItems, IServiceCollection services)
        {
            serviceItems.Insert(0, new ServiceItem()
            {
                Id = "f73a1f53-4182-4648-9a2b-566e868d41e3",
                ConfigureServiceAction = () =>
                {
                    // 这里可以注册中间件需要的服务，相当于 Asp.net 中的 public void ConfigureServices(IServiceCollection services) 方法
                    //services.AddXXXService();
                },
                Description = "我的自定义中间件服务"
            });
            return base.ConfigureServices(serviceItems, services);
        }
        public override List<MiddlewareItem> Configure(List<MiddlewareItem> middlewareItems, IApplicationBuilder app)
        {
            middlewareItems.Insert(0, new MiddlewareItem()
            {
                Id = "f73a1f53-4182-4648-9a2b-566e868d41e3",
                ConfigureMiddleWareAction = () =>
                {
                    app.UseMiddleware<XyLinkMiddleware>();
                },
                Description = "我的自定义中间件"
            });
            return base.Configure(middlewareItems, app);
        }
    }
}
