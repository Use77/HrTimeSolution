using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;

namespace HrTime.Push;

public sealed class WeComDirectOptions
{
    public string CorpId { get; set; } = "";
    public string CorpSecret { get; set; } = "";
    public int AgentId { get; set; }
}

public sealed class WeComDirectClient : IWeComPush
{
    private readonly HttpClient _http = new();
    private readonly WeComDirectOptions _opt;
    private string? _token; private DateTime _tokenExp = DateTime.MinValue;

    public WeComDirectClient(IOptions<WeComDirectOptions> opt) { _opt = opt.Value; }

    private async Task<string> GetToken()
    {
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExp) return _token!;
        var url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={_opt.CorpId}&corpsecret={_opt.CorpSecret}";
        var r = await _http.GetFromJsonAsync<TokenResp>(url);
        if (r == null || r.errcode != 0) throw new Exception($"gettoken failed: {r?.errmsg}");
        _token = r.access_token;
        _tokenExp = DateTime.UtcNow.AddSeconds(Math.Max(60, r.expires_in - 120));
        return _token!;
    }
    public async Task<(int errcode, string errmsg)> SendAsync(
    string toUser, string content, bool isMarkdown,
    bool onlyToMe = false,            // 调试期：true 表示无论传什么都只推给自己
    string myUserId = "曾宪炎")        // 你的企业微信UserID/账号（能正常收到的那个）
    {
        return (-1, "OK");
    }

    public async Task<(int errcode, string errmsg)> SendAsyncs(string toUser, string content, bool isMarkdown)
    {
        var token = await GetToken();
        var url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}";
        object body;
        if (isMarkdown)
        {
            body = new {
               touser = toUser,
               agentid = _opt.AgentId,
               msgtype = "markdown",
               markdown = new { content },
               duplicate_check_interval = 600
            };
        }
        else
        {
            body = new {
               touser = toUser,
               agentid = _opt.AgentId,
               msgtype = "text",
               text = new { content },
               duplicate_check_interval = 600
            };
        }
        var resp = await (await _http.PostAsJsonAsync(url, body)).Content.ReadFromJsonAsync<WxResp>();
        return resp is null ? (-1, "empty") : (resp.errcode, resp.errmsg);
    }

    private sealed record TokenResp(int errcode, string errmsg, string access_token, int expires_in);
    private sealed record WxResp(int errcode, string errmsg);
}
