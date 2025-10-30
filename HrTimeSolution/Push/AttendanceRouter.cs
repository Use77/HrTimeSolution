using HrTime.Data;
using HrTime.Devices;
using HrTime.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HrTime.Push;

public sealed class AttendanceRouter
{
    private readonly IWeComPush _push;
    //private readonly AppDb _db;
    private readonly IServiceScopeFactory _scopeFactory;

    //public AttendanceRouter(IWeComPush push, AppDb db) { _push=push; _db=db; }
    public AttendanceRouter(IWeComPush push, IServiceScopeFactory scopeFactory)
    {
        _push = push;
        _scopeFactory = scopeFactory;
    }

    private static readonly HashSet<string> _recentHashes = new();
    private static readonly object _lock = new();

    public async Task Handle(AttEvent ev)
    {
        // 内存去重：设备+工号+秒级时间戳
        var hash = $"{ev.DeviceId}:{ev.EnrollNumber}:{ev.Time:yyyyMMddHHmmss}";

        lock (_lock)
        {
            if (_recentHashes.Contains(hash)) return;
            _recentHashes.Add(hash);

            // 只保留最近 2000 条，避免内存无限增长
            if (_recentHashes.Count > 2000)
                _recentHashes.Clear();
        }

        using var scope = _scopeFactory.CreateScope();
        var _db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var emp = await _db.USERINFO.FirstOrDefaultAsync(e => e.BADGENUMBER == ev.EnrollNumber);
        if (emp == null) return;
        var device = await _db.Machines.FirstOrDefaultAsync(d => d.ID.ToString() == ev.DeviceId.ToString());

        string location = device?.MachineAlias ?? "未知设备";
        string ip = device?.IP ?? "N/A";
        string markdown =
$"""
**📋 考勤打卡通知**

> 👤 <font color="warning">姓名：</font><font color="info">{emp.NAME}</font>  
> 🆔 <font color="comment">用户识别号：</font>{emp.BADGENUMBER}  
> 🏢 <font color="comment">地点：</font>{location}（{ip}）  
> 🕓 <font color="comment">打卡时间：</font><font color="info">{ev.Time:yyyy-MM-dd HH:mm:ss}</font>  
> 🖐️ <font color="comment">验证方式：</font><font color="info">{VerifyToText(ev.VerifyMethod)}</font>
<font color="comment">请核对打卡信息，如有异常请联系人事。</font>
""";
        //string markdown = $" <font color=\"warning\">名字：{emp.NAME}</font>\n" +
        //                  $"> 时间:><font color=\"info\">{ev.Time:yyyy-MM-dd HH:mm:ss}</font>\n";

        var res= await _push.SendAsync(emp.NAME, markdown, isMarkdown: false);
        var log = new Ori_ChangeItem
        {
            Id = Guid.NewGuid(),
            OrderNo = emp.NAME,
            Dept = emp.NAME,
            Status = ip,
            Remark=res.errmsg,
            IsDeleted=0,
            CreateDate = ev.Time
        };
        //var log = new AttendanceLog
        //{
        //    DeviceId = ev.DeviceId,
        //    EnrollNumber = ev.EnrollNumber,
        //    Time = ev.Time,
        //    VerifyMethod = ev.VerifyMethod,
        //    AttState = ev.AttState,
        //    WorkCode = ev.WorkCode,
        //    HashDedup = hash,
        //    PushedAt = DateTime.Now,
        //    ErrCode = code,
        //    ErrMsg = msg
        //};
        _db.Add(log);
        await _db.SaveChangesAsync();
    }
    public async Task HandleS(AttEvent ev)
    {
        //var device = await _db.Devices.FindAsync(ev.DeviceId);
        //if (device == null) return;

        //var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EnrollNumber == ev.EnrollNumber);
        //if (emp == null) return;
        using var scope = _scopeFactory.CreateScope();
        var _db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var emp = await _db.USERINFO.FirstOrDefaultAsync(e => e.BADGENUMBER == ev.EnrollNumber);

        if (emp == null) return;
        var device = await _db.Machines.FirstOrDefaultAsync(d => d.ID == ev.DeviceId);

        string location = device?.MachineAlias ?? "未知设备";
        string ip = device?.IP ?? "N/A";

        //var hash = Hash($"{device.Sn}:{ev.EnrollNumber}:{ev.Time:O}");
        //if (await _db.AttLogs.AnyAsync(a => a.HashDedup == hash)) return;

        //var verify = VerifyToText(ev.VerifyMethod);
        //var state  = AttStateToText(ev.AttState);
        //var loc    = await _db.Locations.FindAsync(device.LocationId);
        string markdown =
$"""
**🕘 考勤打卡通知**

> 👤 <font color="warning">姓名：</font><font color="info">{emp.NAME}</font>  
> 🆔 <font color="comment">用户识别号：</font>{emp.BADGENUMBER}  
> 🏢 <font color="comment">地点：</font>{location}（{ip}）  
> 🕓 <font color="comment">打卡时间：</font><font color="info">{ev.Time:yyyy-MM-dd HH:mm:ss}</font>  
> 📋 <font color="comment">打卡类型：</font><font color="info">{AttStateToText(ev.AttState)}</font>  
> 🖐️ <font color="comment">验证方式：</font><font color="info">{VerifyToText(ev.VerifyMethod)}</font>
<font color="comment">请核对打卡信息，如有异常请联系人事。</font>
""";
        //string markdown = $"%23 <font color=\\\"warning\\\">名字：{emp.NAME}</font>\\n" +
                          //$"> 时间：\\n><font color=\\\"info\\\">{ev.Time:yyyy-MM-dd HH:mm:ss}</font>\\n";// +
                          //$"> 地点：{(loc?.Name ?? device.Name)}（{verify}/{state}）";

        var (code, msg) = await _push.SendAsync(emp.NAME, markdown, isMarkdown: false);

        var log = new Ori_ChangeItem
        {
            Id =Guid.NewGuid(),
            OrderNo=emp.NAME,
            Dept=emp.NAME,
            Status=ip,         
        };
        //var log = new AttendanceLog
        //{
        //    DeviceId = ev.DeviceId,
        //    EnrollNumber = ev.EnrollNumber,
        //    Time = ev.Time,
        //    VerifyMethod = ev.VerifyMethod,
        //    AttState = ev.AttState,
        //    WorkCode = ev.WorkCode,
        //    HashDedup = hash,
        //    PushedAt = DateTime.Now,
        //    ErrCode = code,
        //    ErrMsg = msg
        //};
        _db.Add(log);
        await _db.SaveChangesAsync();
    }

    private static string Hash(string s)
    { using var sha1 = SHA1.Create(); return Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(s))); }

    private static string VerifyToText(int v) => v switch { 1 => "指纹", 2 => "刷卡", 0 => "密码", _ => $"方式{v}" };
    private static string AttStateToText(int s) => s switch { 0 => "上班打卡", 1 => "下班打卡", 2 => "外出", 3 => "返回", 4 => "加班上", 5 => "加班下", _ => $"状态{s}" };
}
