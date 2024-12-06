using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServer
{
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
                    // ȯ�溰 ������ �ʿ��� ���
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
            // config.AddJsonFile 
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddSingleton<CustomWebSocketHandler>(); �̷������� WebSocketHandler�� ���

            //ASP.NET Core�� ������ ����(DI) �����̳ʿ� MVC ��Ʈ�ѷ��� ���õ� ���񽺸� ����մϴ�. �� ȣ���� ��Ʈ�ѷ� ��� ���ø����̼��� �����ϰ� �����ϱ� ���� �ʼ���
            services.AddControllers();

            // MySettings�� ���� �������κ��� ���ε��Ͽ� ������ �� �ְ� �����մϴ�.
            services.Configure<MySettings>(Configuration.GetSection("MySettings"));

            // MyService�� ���񽺷� �߰�
            services.AddSingleton<MyService>(); // Transient, Scoped, Singleton �� ��Ȳ�� �´� ����������Ŭ�� ����

            // ��Ÿ �ʿ��� ���� �߰�
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // �� �̵���� ����
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }

    public class MySettings
    {
        public string SettingA { get; set; }
        public string SettingB { get; set; }
    }

    public class MyService
    {
        private readonly IOptions<MySettings> _settings;
        public MyService(IOptions<MySettings> settings)
        {
            _settings = settings;
        }
        public void PrintSetting()
        {
            Console.WriteLine($"SettingA: {_settings.Value.SettingA}");
        }
    }
    [ApiController]
    [Route("api/[controller]")]
    public class RoomController : ControllerBase
    {
        public RoomController(MyService myService, IOptions<MySettings> settings)
        {
            var asdf = 1;
        }

        // ��Ʈ�ѷ� �׼� �޼��� �ۼ�
        [HttpGet("test")]
        public IActionResult CreateGameSession()
        {
            //http://localhost:5000/api/room/test
            return StatusCode(200, new { error = "Internal server error" });
        }
    }
}



