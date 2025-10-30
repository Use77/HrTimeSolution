#region
//using Microsoft.Extensions.Options;

//namespace HrTime.Devices;

//public sealed class DeviceListOptions : List<DeviceOptions>
//{
//    public DeviceListOptions() { }
//    public DeviceListOptions(IEnumerable<DeviceOptions> items) : base(items) { }
//}

//public sealed class DeviceOptions
//{
//    public int Id { get; set; }
//    public string Name { get; set; } = "";
//    public string IP { get; set; } = "";
//    public int Port { get; set; }
//    public int MachineNo { get; set; } = 1;
//    public int LocationId { get; set; }
//    public string? CommPassword { get; set; }
//}

//public sealed class ZkDeviceManager
//{
//    private readonly Dictionary<int, ZkDeviceConnector> _map = new();

//    public ZkDeviceManager(DeviceListOptions opts, ILogger<ZkDeviceManager> log)
//    {
//        foreach (var o in opts)
//        {
//            _map[o.Id] = new ZkDeviceConnector(o.Id, o.IP, o.Port, o.MachineNo, o.CommPassword);
//            log.LogInformation("Loaded device: Id={Id}, {IP}:{Port}, MNo={MNo}", o.Id, o.IP, o.Port, o.MachineNo);
//        }
//        if (_map.Count == 0)
//            log.LogWarning("No devices loaded from configuration 'DeviceOptions'.");
//    }

//    public IEnumerable<ZkDeviceConnector> All() => _map.Values;
//    public ZkDeviceConnector? Get(int id) => _map.TryGetValue(id, out var c) ? c : null;
//}
#endregion

using HrTime.Data;
using HrTime.Models;
using Microsoft.Extensions.Logging;

namespace HrTime.Devices;

public sealed class ZkDeviceManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZkDeviceManager> _log;
    private readonly Dictionary<int, ZkDeviceConnector> _map = new();
    private readonly object _lock = new();

    public ZkDeviceManager(IServiceScopeFactory scopeFactory, ILogger<ZkDeviceManager> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
        Reload(); // 启动时加载一次
    }

    /// <summary>从数据库 Machines 表重载设备清单</summary>
    public void Reload()
    {
        lock (_lock)
        {
            // 断开并清空旧连接器
            foreach (var kv in _map) { try { kv.Value.Disconnect(); } catch { } }
            _map.Clear();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            // 只加载 TCP/IP 设备；如需串口，在此分支拓展
            var items = db.Machines
                .Where(m => m.ConnectType == 1
                         && !string.IsNullOrEmpty(m.IP)
                         && m.Port.HasValue)
                .Select(m => new {
                    Id = m.ID,
                    Name = m.MachineAlias ?? $"Device-{m.ID}",
                    IP = m.IP!,
                    Port = m.Port!.Value,
                    MachineNo = m.MachineNumber ?? 1
                })
                .ToList();

            foreach (var m in items)
            {
                var conn = new ZkDeviceConnector(m.Id, m.IP, m.Port, m.MachineNo, commPassword: null);
                _map[m.Id] = conn;
                _log.LogInformation("Loaded device from DB: Id={Id}, {IP}:{Port}, MachineNo={MNo}, Alias={Name}",
                    m.Id, m.IP, m.Port, m.MachineNo, m.Name);
            }

            if (_map.Count == 0)
                _log.LogWarning("No TCP/IP devices loaded from Machines table.");
        }
    }

    public IEnumerable<ZkDeviceConnector> All() => _map.Values;

    public ZkDeviceConnector? Get(int id) => _map.TryGetValue(id, out var c) ? c : null;
}
