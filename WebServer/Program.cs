﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Common;
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
            // config.AddJsonFile 
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddSingleton<CustomWebSocketHandler>(); 이런식으로 WebSocketHandler를 등록

            //ASP.NET Core의 의존성 주입(DI) 컨테이너에 MVC 컨트롤러와 관련된 서비스를 등록합니다. 이 호출은 컨트롤러 기반 애플리케이션을 설정하고 실행하기 위해 필수적
            services.AddControllers();

            // MySettings을 구성 섹션으로부터 바인딩하여 주입할 수 있게 설정합니다.
            services.Configure<MySqlSettings>(Configuration.GetSection("MySqlSettings"));

            // MyService를 서비스로 추가
            //services.AddSingleton<MyService>(); // Transient, Scoped, Singleton 중 상황에 맞는 라이프사이클을 선택

            // 기타 필요한 서비스 추가
            //services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 앱 미들웨어 설정
            if (env.IsDevelopment())
            {
                app.UseSwagger(options =>
                {
                    //options.SerializeAsV2 = true;
                });
                app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                    options.RoutePrefix = string.Empty;
                });

                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }

    public class MySqlSettings
    {
        public string? ConnectString {  get; set; }
        public string? SettingA { get; set; }
        public string? SettingB { get; set; }
    }

    public class MyService
    {
        private readonly IOptions<MySqlSettings> _settings;
        public MyService(IOptions<MySqlSettings> settings)
        {
            _settings = settings;
        }
        public void PrintSetting()
        {
            Console.WriteLine($"SettingA: {_settings.Value.SettingA}");
        }
    }

    [ApiController]
    [Route("[controller]")]
    public partial class RoomController : ControllerBase
    {
        private readonly MySqlModule _testDb;
        public RoomController(/*MyService myService, */IOptions<MySqlSettings> settings)
        {
            string connectString = settings.Value.ConnectString ?? throw new Exception("settings.Value.MySqlSetting");
            _testDb = new MySqlModule(connectString);
        }

        // 컨트롤러 액션 메서드 작성
        [HttpGet("test")]
        public IActionResult CreateGameSession()
        {
            //http://localhost:5000/api/room/test
            return StatusCode(200, new { error = "Internal server error" });
        }
    }


    public partial class RoomController : ControllerBase
    {
        public class GetArgs
        {
            public int P1 { get; set; }
            public int P2 { get; set; }
        }

        [HttpGet("get_{id}")]
        public async Task<IActionResult> CreateGameSession([FromRoute] string id, [FromQuery] GetArgs args)
        {
            //var dba = new MySqlModule(_connectString);
            await _testDb.QueryMultiAsync<string>("show tables;");
            //http://localhost:5000/room/get_{id}?P1=1&P2=4
            return StatusCode(200, new { error = "Internal server error" });
        }

        [HttpPost("set_{id}")]
        public IActionResult CreateGameSession([FromRoute] string id, [FromBody] int name)
        {
            //http://localhost:5000/room/set_{id}
            return StatusCode(200, new { error = "Internal server error" });
        }
    }

    
}



