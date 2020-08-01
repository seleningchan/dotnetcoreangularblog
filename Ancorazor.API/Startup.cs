#region

using AspectCore.Extensions.DependencyInjection;
using AutoMapper;
using Ancorazor.API.Authentication;
using Ancorazor.API.AutoMapper;
using Ancorazor.API.Common.Constants;
using Ancorazor.API.Filters;
using Ancorazor.Entity;
using EasyCaching.Core;
using EasyCaching.InMemory;
using EasyCaching.Interceptor.AspectCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Exceptions;
using Siegrain.Common.FileSystem;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Ancorazor.API.Services;
using Siegrain.Common;

#endregion

namespace Ancorazor.API
{
    public class Startup
    {
        private const string _ServiceName = "Ancorazor.API";
        private const string _CacheProviderName = "default";

        private readonly IConfiguration _configuration;
        private readonly ILogger<Startup> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment, ILogger<Startup> logger)
        {
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _logger.LogInformation("hello logger");
            SetupLogger();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            RegisterMapper(services);
            RegisterAppSettings(services);
            RegisterDynamicProxy(services);
            RegisterEntityFramework(services);
            RegisterCaching(services);
            RegisterUtilities(services);
            RegisterMvc(services);
            RegisterService(services);
            RegisterSwagger(services);
            RegisterCors(services);
            RegisterAuthentication(services);
            RegisterSpa(services);

            return services.ConfigureAspectCoreInterceptor(options =>
            {
                options.CacheProviderName = _CacheProviderName;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                ConfigureSwagger(app);
            }

            app.UseCors();
            ConfigureAuthentication(app);
            ConfigureEntityFramework(app, env);
            app.UseHttpsRedirection();
            ConfigureMvc(app);
            ConfigureSpa(app, env);
        }

        private void SetupLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .WriteTo.File("Logs/log.txt")
            .CreateLogger();
        }

        #region Services

        private void RegisterCaching(IServiceCollection services)
        {
            services.AddEasyCaching(option =>
            {
                // use memory cache
                option.UseInMemory(options =>
                {
                    options.EnableLogging = true;
                }, _CacheProviderName);
            });
        }

        private void RegisterUtilities(IServiceCollection services)
        {
            services.AddSingleton<IFileSystem>(new LocalDiskFileSystem(Path.Combine(_hostingEnvironment.ContentRootPath, "Upload")));
            services.AddSingleton<ImageProcessor>();
            services.AddScoped<ISpaPrerenderingService, SpaPrerenderingService>();
        }

        private void RegisterMapper(IServiceCollection services)
        {
            var mappingConfig = new MapperConfiguration(x =>
            {
                x.AddProfile<MappingProfile>();
                x.CreateMissingTypeMaps = true; // use dynamic map
                x.ValidateInlineMaps = false;   // ignore unmapped properties
            });

            var mapper = mappingConfig.CreateMapper();

            services.AddSingleton(mapper);
        }

        private void RegisterAppSettings(IServiceCollection services)
        {
            services.Configure<SEOConfiguration>(x => _configuration.GetSection(nameof(SEOConfiguration)).Bind(x));
            services.Configure<DbConfiguration>(x => _configuration.GetSection(nameof(DbConfiguration)).Bind(x));
        }

        private void RegisterDynamicProxy(IServiceCollection services)
        {
            /*
             MARK: AOP
             ref: https://github.com/dotnetcore/AspectCore-Framework/blob/master/docs/1.%E4%BD%BF%E7%94%A8%E6%8C%87%E5%8D%97.md
             */
            services.ConfigureDynamicProxy();
        }

        private void RegisterEntityFramework(IServiceCollection services)
        {
            /*
             * MARK: Parallel async method of ef core.
             * https://stackoverflow.com/questions/44063832/what-is-the-best-practice-in-ef-core-for-using-parallel-async-calls-with-an-inje
             */
            services.AddScoped<BlogContext, BlogContext>();
            services.AddDbContext<BlogContext>(options =>
            {
                options.UseSqlServer(
                    _configuration[$"{nameof(DbConfiguration)}:{nameof(DbConfiguration.ConnectionString)}"]);
            });
        }

        private void RegisterMvc(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
                options.Filters.Add<GlobalValidateModelFilter>();
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
            })
            .SetCompatibilityVersion(CompatibilityVersion.Latest)
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
        }

        private void RegisterCors(IServiceCollection services)
        {
            services.AddCors(c =>
            {
                c.AddDefaultPolicy(policy =>
                {
                    policy
                        .WithOrigins("http://localhost:59964")
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        private void RegisterAuthentication(IServiceCollection services)
        {
            /**
             * MARK: Cookie based authentication
             * https://docs.microsoft.com/zh-cn/aspnet/core/security/authentication/cookie?view=aspnetcore-2.0&tabs=aspnetcore2x#persistent-cookies
             */
            services.AddSingleton<SGCookieAuthenticationEvents>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.EventsType = typeof(SGCookieAuthenticationEvents);
            });

            /**
             * MARK: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core
             * https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-2.2
             */
            services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; });
        }

        private void RegisterService(IServiceCollection services)
        {
            var assembly = Assembly.Load("Ancorazor.Service");
            var allTypes = assembly.GetTypes();
            foreach (var type in allTypes) services.AddScoped(type);
        }

        private void RegisterSpa(IServiceCollection services)
        {
            var section = _configuration.GetSection("Client");
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = $"{section["ClientPath"]}/dist";
            });
        }

        private void RegisterSwagger(IServiceCollection services)
        {
            // TODO: ����֤��ʽ������
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = _ServiceName,
                    Version = "v1",
                    Description = "https://github.com/Seanwong933/siegrain.blog"
                });
                c.CustomSchemaIds(type => type.FullName);
                var filePath = Path.Combine(AppContext.BaseDirectory, $"{_ServiceName}.xml");
                if (File.Exists(filePath)) c.IncludeXmlComments(filePath);

                var security = new Dictionary<string, IEnumerable<string>> { { _ServiceName, new string[] { } } };
                c.AddSecurityRequirement(security);
                c.AddSecurityDefinition(_ServiceName, new ApiKeyScheme
                {
                    Description = "���� Bearer {token}",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
            });
        }

        #endregion

        #region Configurations

        private void ConfigureAuthentication(IApplicationBuilder app)
        {
            app.Use(next => context =>
            {
                var contentType = context.Request.ContentType;
                if (!string.IsNullOrEmpty(contentType) &&
                    contentType.ToLower().Contains("application/x-www-form-urlencoded"))
                {
                    _logger.LogInformation(" Form submitting detected.");
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return context.Response.WriteAsync("Bad request.");
                }

                return next(context);
            });

            app.UseAuthentication();
        }

        private void ConfigureEntityFramework(IApplicationBuilder app, IHostingEnvironment env)
        {
            // disable auto migration in production
            //if (!env.IsDevelopment()) return;

            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<BlogContext>();
                context.Database.Migrate();
            }
        }

        private void ConfigureMvc(IApplicationBuilder app)
        {
            // serve files for Upload folder
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/upload", StringComparison.OrdinalIgnoreCase),
                config => config.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(
            Path.Combine(_hostingEnvironment.ContentRootPath, "Upload")),
                    RequestPath = "/Upload"
                }));

            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"),
                apiApp =>
                {
                    apiApp.UseMvc(routes =>
                    {
                        routes.MapRoute("default", "{controller}/{action=Index}/{id?}");
                    });
                });
        }

        /**
         * MARK: Angular 7 + .NET Core Server side rendering
         * https://github.com/joshberry/dotnetcore-angular-ssr
         */
        private void ConfigureSpa(IApplicationBuilder app, IHostingEnvironment env)
        {
            // now the static files will be served by new request URL
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            // add route prefix for SSR
            app.Use((context, next) =>
            {
                // you can have different conditions to add different prefixes
                context.Request.Path = "/client" + context.Request.Path;
                return next.Invoke();
            });

            // MARK: �� SPA ������https://stackoverflow.com/questions/48216929/how-to-configure-asp-net-core-server-routing-for-multiple-spas-hosted-with-spase
            var section = _configuration.GetSection("Client");
            // map spa to /client and remove the prefix
            app.Map("/client", client =>
            {
                client.UseSpa(spa =>
                {
                    // TODO: ���Գ�������ʱ��cookieƾ�ݴ��ݸ�nodejs��Ҫ����SSR��CSRƾ��һ�¡�
                    spa.Options.SourcePath = section["ClientPath"];
                    spa.UseSpaPrerendering(options =>
                    {
                        options.BootModulePath = $"{spa.Options.SourcePath}/dist-server/main.js";
                        options.BootModuleBuilder = env.IsDevelopment()
                            ? new AngularCliBuilder("build:ssr")
                            : null;
                        options.ExcludeUrls = new[] { "/sockjs-node" };
                        options.SupplyData = SpaPrerenderingServiceLocator.GetProcessor(client);
                    });

                    if (env.IsDevelopment())
                    {
                        spa.UseAngularCliServer("start");
                    }
                });
            });
        }

        private void ConfigureSwagger(IApplicationBuilder app)
        {
            app.UseSwagger(c => { });
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", _ServiceName); });
        }

        #endregion

        #region Deprecated

        //private void RegisterAuthenticationForJwt(IServiceCollection services)
        //{
        //    /*
        //     MARK: JWT for session �����ŵ�����
        //     ���ţ�
        //        - https://stackoverflow.com/questions/42036810/asp-net-core-jwt-mapping-role-claims-to-claimsidentity/50523668#50523668
        //        - Refresh token: https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/
        //        - How can I validate a JWT passed via cookies? https://stackoverflow.com/a/39386631
        //     �������Σ�
        //        - Where to store JWT in browser? How to protect against CSRF? https://stackoverflow.com/a/37396572
        //        - Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-2.2
        //     ������
        //        Stop using JWT for sessions
        //        - http://cryto.net/~joepie91/blog/2016/06/13/stop-using-jwt-for-sessions/
        //        - http://cryto.net/~joepie91/blog/2016/06/19/stop-using-jwt-for-sessions-part-2-why-your-solution-doesnt-work/

        //     �ܽ᣺
        //        �䱾�����ʺ������� Session��Session ע���޷���֤��״̬���޷����ú� JWT ���ŵ㣬Ҫǿ����ֻ��ÿ�δ���֤��������� refresh token �Ƿ���Ч��
        //        �ܶ����еĽ����������ÿ������ʱ��� refresh token ��䷢һ���µ� access token��Ȼ���ɵ� access token ������Ч���ڣ���� access_token ����һ��������ȥ˵ʵ��ͦ2b�ģ�����Ϊ�����䡰���ڡ�������Ҫά��һ�� blacklist ���� whitelist���ټ���ˢ�·����Դ��Ĳ������⣬˵ʵ������ JWT session ʵ�������һ���Ѿ���

        //        ��������� access token��refresh token ȫ��ʵ����������ᷢ���������紫ͳ�� session �������������κ�һ�����˲�ǡ����ʵ�ַ�ʽ�������������İ�ȫ©����
        //     */
        //    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        //    var jwtSettings = Configuration.GetSection("Jwt");
        //    services
        //        .AddAuthentication(options =>
        //        {
        //            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        //            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //        })
        //        .AddJwtBearer(cfg =>
        //        {
        //            cfg.RequireHttpsMetadata = false;
        //            cfg.SaveToken = true;
        //            var rsa = RSACryptography.CreateRsaFromPrivateKey(Constants.RSAForToken.PrivateKey);
        //            cfg.TokenValidationParameters = new TokenValidationParameters
        //            {
        //                ClockSkew = TimeSpan.Zero, // remove delay of token when expire

        //                ValidIssuer = jwtSettings["JwtIssuer"],
        //                ValidAudience = jwtSettings["JwtIssuer"],
        //                IssuerSigningKey = new RsaSecurityKey(rsa),

        //                RequireExpirationTime = true,
        //                ValidateLifetime = true
        //            };
        //        });

        //    /**
        //     * MARK: ���� JWT Ԥ�� XSRF �� XSS ����
        //     *  
        //     * - ��ƾ�ݣ�JWT������� HttpOnly���޷����ű����ʣ���SameSite=Strict���ύԴ����ʱ��Я����Cookie����Secure����HTTPS��Я����Cookie�� �� Cookie �У������� LocalStorage һ��ĵط�����Ϊ Local Storage��Session Storage ���� XSS �ķ��գ������� chrome extension һ��Ķ������������ȡ������洢���� Cookie ��Ȼ�� XSRF �ķ��գ�������ͨ��˫�ύ Cookie ��Ԥ�������Խ�ƾ֤����� Cookie ��Ȼ�����ȷ�����
        //     * - ��ֹ Form ���ύ����Ϊ���ύ���Կ���
        //     * - ʹ�� HTTPS
        //     * - ����Ĺ��ڻ���
        //     * - �����û���������ֹ XSS
        //     * - ���û�ƾ�ݱ����ˢ�� XSRF Token��ˢ�½ӿ��� UserController -> GetXSRFToken��
        //     * - ��ֹ HTTP TRACE ��ֹ XST ������������һ�º���Ĭ�Ͼ��ǽ�ֹ�ģ�
        //     * - ���� JWT Authentication �м���ǲ��� Header Authorization �ڽ�����֤��������Ҫ��Authentication ǰ����һ���м���ж��Ƿ��� access token���еĻ��ֶ��� Header �в��� Authorization ����֧�� JWT ��֤��
        //     * 
        //     * - refs:
        //     *  Where to store JWT in browser? How to protect against CSRF? https://stackoverflow.com/a/37396572
        //     *  ʵ��һ�����׵�Web��֤��https://www.jianshu.com/p/805dc2a0f49e
        //     *  How can I validate a JWT passed via cookies? https://stackoverflow.com/a/39386631
        //     *  Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-2.2
        //     *  2 ¥���������� refresh token �Ƿ������壬�в���Ĳο���ֵ��https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/
        //     *  
        //     */
        //    services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; });
        //}
        #endregion
    }
}