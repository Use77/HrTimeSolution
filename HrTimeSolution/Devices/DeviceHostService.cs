using HrTime.Push;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HrTime.Devices;

public sealed class DeviceHostService : BackgroundService
{
    private readonly ZkDeviceManager _mgr;
    private readonly AttendanceRouter _router;
    private readonly ILogger<DeviceHostService> _log;

    public DeviceHostService(ZkDeviceManager mgr, AttendanceRouter router, ILogger<DeviceHostService> log)
    { _mgr=mgr; _router=router; _log=log; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var dev in _mgr.All())
        {
            Task.Run(() =>
            {
                try
                {
                    if (!dev.Connect()) { _log.LogWarning("连接失败"); return; }
                    dev.OnAttendance += async (_, ev) => await _router.Handle(ev);
                }
                catch (Exception ex) { _log.LogError(ex, "设备线程异常"); }
            }, stoppingToken);
        }
        return Task.CompletedTask;
    }
}
