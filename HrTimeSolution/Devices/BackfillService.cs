namespace HrTime.Devices;

public sealed class BackfillService : BackgroundService
{
    private readonly ZkDeviceManager _mgr;
    private readonly HrTime.Push.AttendanceRouter _router;

    public BackfillService(ZkDeviceManager mgr, HrTime.Push.AttendanceRouter router)
    { _mgr=mgr; _router=router; }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var since = DateTime.Now.AddMinutes(-30);
            foreach (var dev in _mgr.All())
            {
                foreach (var ev in dev.DownloadRange(since, DateTime.Now))
                    await _router.Handle(ev);
            }
            await Task.Delay(TimeSpan.FromMinutes(5), token);
        }
    }
}
