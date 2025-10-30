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
    bool onlyToMe = false,            // �����ڣ�true ��ʾ���۴�ʲô��ֻ�Ƹ��Լ�
    string myUserId = "������")        // �����ҵ΢��UserID/�˺ţ��������յ����Ǹ���
    {
        // 1) ��һ��ԭʼ��Σ�ȥ�ո�/ȫ�ǿո�/�������ַ���
        string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().Replace("\u3000", " ");                    // ȫ�ǿո�
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return s;
        }
        var originalToUser = Normalize(toUser);

        // 2) �����������͵� toUser������ʱǿ�Ƹ���Ϊ�Լ���������ű��ˣ�
        var toUserUsed = onlyToMe ? myUserId : originalToUser;

        var endpoint = isMarkdown ? "SendMarkdownMessage" : "SendTextMessage";
        var url = $"{_opt.BaseUrl.TrimEnd('/')}/{endpoint}" +
                  $"?toUser={Uri.EscapeDataString(toUserUsed)}&toParties=&toTags=&message={Uri.EscapeDataString(content ?? "")}";

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        // 3) ����ԭʼ toUser ���������־�Աȣ����������Ƹ����ˣ�
        //req.Headers.TryAddWithoutValidation("X-Debug-Original-ToUser", originalToUser);
        //if (onlyToMe) req.Headers.TryAddWithoutValidation("X-Dry-Run", "1"); // �������Ը��ʶ�𣬿ɾݴ˽���¼

        // 4) ���ص��������������˶�
        //Console.WriteLine($"[DEBUG] originalToUser='{originalToUser}' -> toUserUsed='{toUserUsed}'");
        //Console.WriteLine($"[DEBUG] URL={url}");

        var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        // ��������ж��߼�
        if (body.Contains("\\\"errmsg\\\":\\\"ok\\\"") || body.Contains("\"errmsg\":\"ok\""))
            return (0, "ok");

        return (-1, body);
    }

    public async Task<(int errcode, string errmsg)> SendAsyncs(string toUser, string content, bool isMarkdown)
    {
        if(toUser!="������")
            return (0, "ok");
        var endpoint = isMarkdown ? "SendMarkdownMessage" : "SendTextMessage";
        var url = $"{_opt.BaseUrl.TrimEnd('/')}/{endpoint}" +
                  $"?toUser={toUser}&toParties=&toTags=&message={Uri.EscapeDataString(content)}";
                  //$"?toUser={Uri.EscapeDataString(toUser)}&toParties=&toTags=&message={Uri.EscapeDataString(content)}";
                 //$"?toUser=������&toParties=&toTags=&message={Uri.EscapeDataString(content)}";
        var resp = await _http.PostAsync(url, new StringContent("", Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        if (body.Contains("\\\"errmsg\\\":\\\"ok\\\"") || body.Contains("\"errmsg\":\"ok\""))
            return (0, "ok");
        return (-1, body);
    }
}
