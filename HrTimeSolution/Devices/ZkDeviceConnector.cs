using System.Timers;
using zkemkeeper;

namespace HrTime.Devices;

public sealed record AttEvent
{
    public int DeviceId { get; init; }
    public string EnrollNumber { get; init; } = "";
    public DateTime Time { get; init; }
    public int VerifyMethod { get; init; }
    public int AttState { get; init; }
    public int WorkCode { get; init; }
}

public sealed class ZkDeviceConnector : IDisposable
{
    private readonly int _id;
    private readonly string _ip;
    private readonly int _port;
    private readonly int _machineNo;
    private readonly string? _commPassword;
    private readonly CZKEM _zk = new();
    private System.Timers.Timer? _rtTimer;

    public event EventHandler<AttEvent>? OnAttendance;

    public ZkDeviceConnector(int id, string ip, int port, int machineNo, string? commPassword)
    {
        _id = id; _ip = ip; _port = port; _machineNo = machineNo; _commPassword = commPassword;
        _zk.OnAttTransactionEx += Zk_OnAttTransactionEx;
    }

    public bool Connect()
    {
        if (!string.IsNullOrEmpty(_commPassword))
            _zk.SetCommPasswordEx(_commPassword);

        var ok = _zk.Connect_Net(_ip, _port);
        if (!ok) return false;
        _zk.RegEvent(_machineNo, 65535);
        StartRtLoop();
        return true;
    }

    private void StartRtLoop()
    {
        _rtTimer = new System.Timers.Timer(200);
        _rtTimer.Elapsed += (_, __) =>
        {
            try { _zk.ReadRTLog(_machineNo); _zk.GetRTLog(_machineNo); } catch { }
        };
        _rtTimer.AutoReset = true;
        _rtTimer.Start();
    }

    private void Zk_OnAttTransactionEx(string enrollNumber, int isInValid, int attState,
        int verifyMethod, int year, int month, int day, int hour, int minute, int second, int workCode)
    {
        var ts = new DateTime(year, month, day, hour, minute, second);
        if (ts < DateTime.Now.AddMinutes(-2))
            return;
        OnAttendance?.Invoke(this, new AttEvent
        {
            DeviceId = _id,
            EnrollNumber = enrollNumber,
            Time = ts,
            VerifyMethod = verifyMethod,
            AttState = attState,
            WorkCode = workCode
        });
    }

    public IEnumerable<AttEvent> DownloadRange(DateTime s, DateTime e)
    {
        if (!_zk.ReadTimeGLogData(_machineNo, s.ToString("yyyy-MM-dd HH:mm:ss"),
                                             e.ToString("yyyy-MM-dd HH:mm:ss")))
            yield break;

        string enNo = "";
        int vMode=0, iState=0, y=0,m=0,d=0, h=0,mi=0,se=0, w=0;
        while (_zk.SSR_GetGeneralLogData(_machineNo, out enNo, out iState,
               out vMode, out y, out m, out d, out h, out mi, out se, ref w))
        {
            yield return new AttEvent {
                DeviceId=_id, EnrollNumber=enNo, AttState=iState, VerifyMethod=vMode,
                Time=new DateTime(y,m,d,h,mi,se), WorkCode=w
            };
        }
    }

    public void Disconnect()
    {
        try { _rtTimer?.Stop(); } catch { }
        try { _zk.Disconnect(); } catch { }
    }
    public bool TestConnect()
    {
        try
        {
            var zk = new zkemkeeper.CZKEM();
            return zk.Connect_Net(_ip, _port);
        }
        catch(Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 测试读取普通考勤数据
    /// </summary>
    public bool TestReadGeneralLog()
    {
        try
        {
            return _zk.ReadGeneralLogData(_machineNo);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 测试获取一条考勤记录，判断是否能获取数据
    /// </summary>
    public bool TestGetOneLog(out string? record)
    {
        record = null;
        try
        {
            string enroll = "";
            int verify = 0, state = 0, year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0, workCode = 0;
            if (_zk.SSR_GetGeneralLogData(_machineNo, out enroll, out state, out verify,
                                          out year, out month, out day, out hour, out minute, out second, ref workCode))
            {
                record = $"{enroll} {year}-{month}-{day} {hour}:{minute}:{second}";
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
    public void Dispose() => Disconnect();
}
