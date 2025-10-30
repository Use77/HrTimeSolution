using HrTime.Data;
using HrTime.Devices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HrTime.Controllers;

[ApiController]
[Route("api/att")]
public sealed class AttendanceController : ControllerBase
{
    private readonly AppDb _db;
    public AttendanceController(AppDb db) { _db = db; }

    //[HttpGet("logs")]
    //public async Task<IActionResult> Logs([FromQuery] string? enroll, [FromQuery] DateTime? s, [FromQuery] DateTime? e)
    //{
    //    //var q = _db.AttLogs.AsQueryable();
    //    //if (!string.IsNullOrEmpty(enroll)) q = q.Where(x => x.EnrollNumber == enroll);
    //    //if (s.HasValue) q = q.Where(x => x.Time >= s);
    //    //if (e.HasValue) q = q.Where(x => x.Time <= e);
    //    //return Ok(await q.OrderByDescending(x => x.Time).Take(300).ToListAsync());
    //    return Ok(enroll);
    //}

    [HttpGet("device/test-connect/{id}")]
    public IActionResult TestConnect([FromServices] ZkDeviceManager mgr, int id)
    {
        var dev = mgr.Get(id);
        if (dev == null) return NotFound("设备不存在");
        var ok = dev.TestConnect();
        return Ok(new { DeviceId = id, Success = ok });
    }

    [HttpGet("device/test-readlog/{id}")]
    public IActionResult TestReadLog([FromServices] ZkDeviceManager mgr, int id)
    {
        var dev = mgr.Get(id);
        if (dev == null) return NotFound("设备不存在");
        var ok = dev.TestReadGeneralLog();
        return Ok(new { DeviceId = id, CanRead = ok });
    }

    [HttpGet("device/test-getone/{id}")]
    public IActionResult TestGetOne([FromServices] ZkDeviceManager mgr, int id)
    {
        var dev = mgr.Get(id);
        if (dev == null) return NotFound("设备不存在");
        var ok = dev.TestGetOneLog(out var rec);
        return Ok(new { DeviceId = id, GotRecord = ok, Record = rec });
    }

    [HttpGet("device/test-all")]
    public IActionResult TestAll([FromServices] ZkDeviceManager mgr)
    {
        var list = new List<object>();
        var e = mgr.All();
        foreach (var dev in mgr.All())
        {
            bool okConn = dev.TestConnect();
            bool okRead = dev.TestReadGeneralLog();
            bool okGet = dev.TestGetOneLog(out var rec);

            list.Add(new
            {
                devId = dev.GetHashCode(), // 这里建议换成你配置里的 DeviceId
                Connect = okConn,
                CanRead = okRead,
                GotRecord = okGet,
                Record = rec
            });
        }
        return Ok(list);
    }
    //[HttpGet("device/config")]
    //public IActionResult DeviceConfig([FromServices] DeviceListOptions opts)
    //{
    //    var list = opts.Select(o => new {
    //        o.Id,
    //        o.Name,
    //        o.IP,
    //        o.Port,
    //        o.MachineNo,
    //        o.LocationId
    //    });
    //    return Ok(list);
    //}

    [HttpPost("device/reload")]
    public IActionResult ReloadDevices([FromServices] ZkDeviceManager mgr)
    {
        mgr.Reload();
        return Ok(new { message = "Devices reloaded from database." });
    }

    [HttpPost("diagnose/send")]
    public async Task<IActionResult> DiagnoseSend(
    [FromBody] PushTestRequest req,
    [FromServices] IHttpClientFactory f,
    [FromServices] IConfiguration cfg)
    {
        if (req == null)
            return BadRequest("请求体不能为空");

        var baseUrl = cfg["WeCom:BaseUrl"]?.TrimEnd('/');
        var endpoint = req.IsMarkdown ? "SendMarkdownMessage" : "SendTextMessage";
        var client = f.CreateClient();

        string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().Replace("\u3000", " ");
            return System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        }

        var originalToUser = Normalize(req.ToUser);
        var toUserUsed = req.OnlyToMe == true
            ? (cfg["WeCom:MyUserId"] ?? "小明")
            : originalToUser;

        // -------- 拼接 QueryString URL ------------
        var url = $"{baseUrl}/{endpoint}" +
                  $"?toUser={Uri.EscapeDataString(toUserUsed)}" +
                  $"&toParties={Uri.EscapeDataString(req.ToParty ?? "")}" +
                  $"&toTags=&message={Uri.EscapeDataString(req.Content ?? "")}";

        // -------- 发送请求 ------------
        using var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        // Base64 调试头（ASCII 安全）
        string safeDebug = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalToUser ?? ""));
        httpReq.Headers.TryAddWithoutValidation("X-Debug-Original-ToUser", safeDebug);
        httpReq.Headers.TryAddWithoutValidation("X-Dry-Run", (req.OnlyToMe ?? false) ? "1" : "0");

        var resp = await client.SendAsync(httpReq);
        var body = await resp.Content.ReadAsStringAsync();

        // -------- 返回完整诊断结果 ------------
        return Ok(new
        {
            env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            baseUrl,
            endpoint,
            url,
            toUserUsed,
            originalToUser,
            onlyToMe = req.OnlyToMe,
            raw = body
        });
    }

    public class PushTestRequest
    {
        public string ToUser { get; set; }
        public string ToParty { get; set; }
        public string Content { get; set; }
        public bool IsMarkdown { get; set; }
        public bool? OnlyToMe { get; set; }
    }
}
