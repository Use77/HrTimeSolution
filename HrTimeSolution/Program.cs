using HrTime.Data;
using HrTime.Devices;
using HrTime.Push;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

if (OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = builder.Configuration["ServiceName"] ?? "HrTime Device Service";
    });
}

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// COM interop for zkemkeeper (必需打开)
AppContext.SetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", true);

// EF Core DbContext (Scoped 生命周期)
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Options 配置绑定
// 显式读取数组 section 并绑定到 List<DeviceOptions>
//var devices = builder.Configuration.GetSection("DeviceOptions").Get<List<DeviceOptions>>()
//              ?? new List<DeviceOptions>();

// 以实例方式注册，避免 IOptions<> 派生 List 绑定问题
//builder.Services.AddSingleton(new DeviceListOptions(devices));
//builder.Services.Configure<DeviceListOptions>(builder.Configuration.GetSection("DeviceOptions"));
builder.Services.Configure<PushSettings>(builder.Configuration.GetSection("Push"));
builder.Services.Configure<WeComInternalOptions>(builder.Configuration.GetSection("Push:Internal"));
builder.Services.Configure<WeComDirectOptions>(builder.Configuration.GetSection("Push:Direct"));

// Push 模式切换（Internal / Direct）
builder.Services.AddSingleton<IWeComPush>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var mode = cfg["Push:Mode"] ?? "Internal";
    return mode switch
    {
        "Direct" => ActivatorUtilities.CreateInstance<WeComDirectClient>(sp),
        _ => ActivatorUtilities.CreateInstance<WeComInternalClient>(sp),
    };
});

// 核心服务
builder.Services.AddSingleton<AttendanceRouter>();   // Router 改为 Singleton + IServiceScopeFactory 解决 Scoped 问题
builder.Services.AddSingleton<ZkDeviceManager>();
builder.Services.AddHostedService<DeviceHostService>();
//builder.Services.AddHostedService<BackfillService>();

// API Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Swagger UI (仅开发环境启用)
if (app.Environment.IsDevelopment()||app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map Controllers
app.MapControllers();

app.Run();
