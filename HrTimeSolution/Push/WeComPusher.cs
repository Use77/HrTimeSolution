using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class WeComPusher
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public WeComPusher(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient();
    }

    /// <summary>
    /// 发送企业微信消息（POST JSON）
    /// </summary>
    /// <param name="toUser">接收人（企业微信UserID）</param>
    /// <param name="content">消息内容</param>
    /// <param name="isMarkdown">是否Markdown消息</param>
    /// <param name="onlyToMe">是否只推送给自己（调试用）</param>
    /// <param name="myUserId">调试时接收人UserID（默认“小明”）</param>
    public async Task<(int errcode, string errmsg)> SendAsync(
        string toUser,
        string content,
        bool isMarkdown,
        bool onlyToMe = false,
        string myUserId = "曾宪炎")
    {
        // ---------- Step 1: 归一化 ----------
        string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().Replace("\u3000", " "); // 去全角空格
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return s;
        }

        var originalToUser = Normalize(toUser);
        var toUserUsed = onlyToMe ? myUserId : originalToUser;

        // ---------- Step 2: 组装JSON ----------
        var payload = new
        {
            toUser = toUserUsed,
            toParties = "",
            toTags = "",
            message = content ?? "",
            isMarkdown
        };

        string json = JsonSerializer.Serialize(payload);
        string endpoint = isMarkdown ? "SendMarkdownMessage" : "SendTextMessage";
        string url = $"{_baseUrl}/{endpoint}";

        // ---------- Step 3: 发送请求 ----------
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // 附加调试信息（不会影响推送）
        req.Headers.TryAddWithoutValidation("X-Debug-Original-ToUser", originalToUser);
        if (onlyToMe)
            req.Headers.TryAddWithoutValidation("X-Dry-Run", "1");

        Console.WriteLine($"[DEBUG] POST {url}");
        Console.WriteLine($"[DEBUG] Payload: {json}");

        HttpResponseMessage resp = await _http.SendAsync(req);
        string body = await resp.Content.ReadAsStringAsync();

        Console.WriteLine($"[DEBUG] Response: {body}");

        // ---------- Step 4: 解析返回 ----------
        if (body.Contains("\"errmsg\":\"ok\"") || body.Contains("\\\"errmsg\\\":\\\"ok\\\""))
            return (0, "ok");

        return (-1, body);
    }
}