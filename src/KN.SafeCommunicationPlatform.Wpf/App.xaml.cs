using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using KN.SafeCommunicationPlatform.EF;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Microsoft.Extensions.Hosting;

namespace KN.SafeCommunicationPlatform.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = default!;
        public IConfigurationRoot Configuration { get; private set; } = default!;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            var services = new ServiceCollection();
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.production.json", true, true)
                .Build();
            services.AddSingleton<IConfigurationRoot>(Configuration);
            services.AddSingleton<IConfiguration>(Configuration);
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
            });

            services.AddDbContextFactory<AppDbContext>((p, builder) =>
            {
                var configuration = p.GetRequiredService<IConfiguration>();
                var dbType = configuration["DbType"];
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (dbType.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    builder.UseSqlite(connectionString);
                }
                else if (dbType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    builder.UseSqlServer(connectionString);
                }
                else if (dbType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
                {
                    builder.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                }

            }, ServiceLifetime.Scoped);

            ServiceProvider = services.BuildServiceProvider(true);
        }
    }
}
