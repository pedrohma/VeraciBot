using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VeraciBot;
using VeraciInfra.Data;
using VeraciInfra.Services.API;
using VeraciInfra.Services.Data;
using VeraciLib.Interfaces.API;
using VeraciLib.Interfaces.Infra;
using VeraciLib.Interfaces.Infra.Base;
using VeraciLib.Settings;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        (context, services) =>
        {
            services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

            services.AddDbContext<VeraciDbContext>(options =>
                options.UseSqlServer(
                    context
                        .Configuration.GetSection("AppSettings:DatabaseSettings:ConnectionString")
                        .Value
                )
            );

            services.AddLogging();

            services.AddTransient(typeof(IRepository<>), typeof(RepositoryService<>));

            services.AddSingleton<ITwitterActions, TwitterServices>();
            services.AddSingleton<IOpenAiActions, OpenAiServices>();
            services.AddSingleton<ITweetService, TweetService>();
            services.AddSingleton<IConfigService, ConfigService>();
            services.AddTransient<Phrases>();

            services.AddHostedService<TwitterBotWorker>();
        }
    )
    .Build();

await host.RunAsync();
