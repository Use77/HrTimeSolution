namespace HrTime.Push;

public sealed class PushSettings
{
    public string Mode { get; set; } = "Internal";
}

public interface IWeComPush
{
    Task<(int errcode, string errmsg)> SendAsync(string toUser, string content, bool isMarkdown, bool onlyToMe = false, string myUserId = "ÔøÏÜÑ×");
    Task<(int errcode, string errmsg)> SendAsyncs(string toUser, string content, bool isMarkdown);
}
