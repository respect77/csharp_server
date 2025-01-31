
using TcpServer.Context;

namespace TcpServer
{
    public class ServerSettings
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
    }
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    // 환경별 설정이 필요한 경우
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ServerSettings>(Configuration.GetSection("ServerSettings"));
            services.AddHostedService<ServerContext>();
            //services.AddHostedService<ServerHostedService>();
        }
        public void Configure()
        {
        }
    }
    /*
    public class ServerHostedService : IHostedService
    {
        private readonly ServerContext _serverContext;

        public ServerHostedService(ServerContext serverContext)
        {
            _serverContext = serverContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _serverContext.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _serverContext.Stop();
            return Task.CompletedTask;
        }
    }
    */
}



