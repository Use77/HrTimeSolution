using System.Text;
using Microsoft.Extensions.Options;

namespace HrTime.Push;

public sealed class WeComInternalOptions
{
    public string BaseUrl { get; set; } = "http://10.0.0.28/api/WorkChat";
    public bool UseMarkdown { get; set; } = true;
}

public sealed class WeComInternalClient : IWeComPush
{
    private readonly HttpClient _http = new();
    private readonly WeComInternalOptions _opt;

    public WeComInternalClient(IOptions<WeComInternalOptions> opt) { _opt = opt.Value; }

    public async Task<(int errcode, string errmsg)> SendAsync(
    string toUser, string content, bool isMarkdown,
    bool onlyToMe = false,            // 调试期：true 表示无论传什么都只推给自己
    string myUserId = "曾宪炎")        // 你的企业微信UserID/账号（能正常收到的那个）
    {
        // 1) 归一化原始入参（去空格/全角空格/看不见字符）
        string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().Replace("\u3000", " ");                    // 全角空格
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return s;
        }
        var originalToUser = Normalize(toUser);

        // 2) 真正用于推送的 toUser（调试时强制覆盖为自己，避免打扰别人）
        var toUserUsed = onlyToMe ? myUserId : originalToUser;

        var endpoint = isMarkdown ? "SendMarkdownMessage" : "SendTextMessage";
        var url = $"{_opt.BaseUrl.TrimEnd('/')}/{endpoint}" +
                  $"?toUser={Uri.EscapeDataString(toUserUsed)}&toParties=&toTags=&message={Uri.EscapeDataString(content ?? "")}";

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        // 3) 带上原始 toUser 供服务端日志对比（不会真正推给他人）
        //req.Headers.TryAddWithoutValidation("X-Debug-Original-ToUser", originalToUser);
        //if (onlyToMe) req.Headers.TryAddWithoutValidation("X-Dry-Run", "1"); // 若服务端愿意识别，可据此仅记录

        // 4) 本地调试输出，方便你核对
        //Console.WriteLine($"[DEBUG] originalToUser='{originalToUser}' -> toUserUsed='{toUserUsed}'");
        //Console.WriteLine($"[DEBUG] URL={url}");

        var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        // 保留你的判断逻辑
        if (body.Contains("\\\"errmsg\\\":\\\"ok\\\"") || body.Contains("\"errmsg\":\"ok\""))
            return (0, "ok");

        return (-1, body);
    }

    public async Task<(int errcode, string errmsg)> SendAsyncs(string toUser, string content, bool isMarkdown)
    {
        if(toUser!="曾宪炎")
            return (0, "ok");
        var endpoint = isMarkdown ? "SendMarkdownMessage" : "SendTextMessage";
        var url = $"{_opt.BaseUrl.TrimEnd('/')}/{endpoint}" +
                  $"?toUser={toUser}&toParties=&toTags=&message={Uri.EscapeDataString(content)}";
                  //$"?toUser={Uri.EscapeDataString(toUser)}&toParties=&toTags=&message={Uri.EscapeDataString(content)}";
                 //$"?toUser=曾宪炎&toParties=&toTags=&message={Uri.EscapeDataString(content)}";
        var resp = await _http.PostAsync(url, new StringContent("", Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        if (body.Contains("\\\"errmsg\\\":\\\"ok\\\"") || body.Contains("\"errmsg\":\"ok\""))
            return (0, "ok");
        return (-1, body);
    }
}
