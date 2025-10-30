using System.ComponentModel.DataAnnotations;

namespace HrTime.Models;

//public sealed class Device
//{
//    public int Id { get; set; }
//    public string Name { get; set; } = "";
//    public string IP { get; set; } = "";
//    public int Port { get; set; }
//    public int MachineNo { get; set; } = 1;
//    public string Sn { get; set; } = "";
//    public int LocationId { get; set; }
//    public DateTime? LastSeen { get; set; }
//    public bool IsOnline { get; set; }
//    public string? CommPassword { get; set; }
//}

//public sealed class Location
//{
//    public int Id { get; set; }
//    public string Name { get; set; } = "";
//    public string Address { get; set; } = "";
//}

public sealed class USERINFO
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Key]
    public int USERID { get; set; }
    /// <summary>
    /// 用户识别号
    /// </summary>
    public string BADGENUMBER { get; set; }
    /// <summary>
    /// 用户名
    /// </summary>
    public string NAME { get; set;}
}
//新增：与数据库 Machines 表对应（字段名按你的描述）
public sealed class Machine
{
    public int ID { get; set; }                    // 机器ID（主键）
    public string MachineAlias { get; set; } = ""; // 设备描述
    public int? ConnectType { get; set; }       // 连接方式（如 "TCPIP" / "RS232"）
    public string? IP { get; set; }                // IP（TCPIP 时使用）
    public int? SerialPort { get; set; }        // 串口名（如 "COM3"）
    public int? Port { get; set; }                 // 端口（TCPIP 时使用，如 4370）
    public int? Baudrate { get; set; }             // 波特率（串口时使用）
    public int? MachineNumber { get; set; }        // 设备站号 / 机器号（默认 1）
}

public sealed class Ori_ChangeItem
{
    public Guid Id { get; set; }

    public string OrderNo { get; set; }
    public string Dept { get; set; }
    public string Remark { get; set; }
    public string Status { get; set; }
    public DateTime CreateDate { get; set; }
    public int IsDeleted { get; set; }
}
//public sealed class Employee
//{
//    public int Id { get; set; }
//    public string EnrollNumber { get; set; } = "";
//    public string Name { get; set; } = "";
//    public string WeComUserId { get; set; } = "";
//    public string? CardNo { get; set; }
//    public int? DeptId { get; set; }
//}

//public sealed class AttendanceLog
//{
//    public int Id { get; set; }
//    public int DeviceId { get; set; }
//    public string EnrollNumber { get; set; } = "";
//    public DateTime Time { get; set; }
//    public int VerifyMethod { get; set; }
//    public int AttState { get; set; }
//    public int WorkCode { get; set; }
//    public string HashDedup { get; set; } = "";
//    public DateTime? PushedAt { get; set; }
//    public int ErrCode { get; set; }
//    public string ErrMsg { get; set; } = "";
//}
