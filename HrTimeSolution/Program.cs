using HrTime.Data;
using HrTime.Devices;
using HrTime.Push;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// COM interop for zkemkeeper (�����)
AppContext.SetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", true);

// EF Core DbContext (Scoped ��������)
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Options ���ð�
// ��ʽ��ȡ���� section ���󶨵� List<DeviceOptions>
//var devices = builder.Configuration.GetSection("DeviceOptions").Get<List<DeviceOptions>>()
//              ?? new List<DeviceOptions>();

// ��ʵ����ʽע�ᣬ���� IOptions<> ���� List ������
//builder.Services.AddSingleton(new DeviceListOptions(devices));
//builder.Services.Configure<DeviceListOptions>(builder.Configuration.GetSection("DeviceOptions"));
builder.Services.Configure<PushSettings>(builder.Configuration.GetSection("Push"));
builder.Services.Configure<WeComInternalOptions>(builder.Configuration.GetSection("Push:Internal"));
builder.Services.Configure<WeComDirectOptions>(builder.Configuration.GetSection("Push:Direct"));

// Push ģʽ�л���Internal / Direct��
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

// ���ķ���
builder.Services.AddSingleton<AttendanceRouter>();   // Router ��Ϊ Singleton + IServiceScopeFactory ��� Scoped ����
builder.Services.AddSingleton<ZkDeviceManager>();
builder.Services.AddHostedService<DeviceHostService>();
//builder.Services.AddHostedService<BackfillService>();

// API Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Swagger UI (��������������)
if (app.Environment.IsDevelopment()||app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map Controllers
app.MapControllers();

app.Run();