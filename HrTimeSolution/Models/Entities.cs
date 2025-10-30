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
    /// �û�ID
    /// </summary>
    [Key]
    public int USERID { get; set; }
    /// <summary>
    /// �û�ʶ���
    /// </summary>
    public string BADGENUMBER { get; set; }
    /// <summary>
    /// �û���
    /// </summary>
    public string NAME { get; set;}
}
//�����������ݿ� Machines ���Ӧ���ֶ��������������
public sealed class Machine
{
    public int ID { get; set; }                    // ����ID��������
    public string MachineAlias { get; set; } = ""; // �豸����
    public int? ConnectType { get; set; }       // ���ӷ�ʽ���� "TCPIP" / "RS232"��
    public string? IP { get; set; }                // IP��TCPIP ʱʹ�ã�
    public int? SerialPort { get; set; }        // ���������� "COM3"��
    public int? Port { get; set; }                 // �˿ڣ�TCPIP ʱʹ�ã��� 4370��
    public int? Baudrate { get; set; }             // �����ʣ�����ʱʹ�ã�
    public int? MachineNumber { get; set; }        // �豸վ�� / �����ţ�Ĭ�� 1��
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
