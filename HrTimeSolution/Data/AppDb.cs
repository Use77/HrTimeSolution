using HrTime.Models;
using Microsoft.EntityFrameworkCore;

namespace HrTime.Data;

public sealed class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> o) : base(o) { }

    //public DbSet<Device> Devices => Set<Device>();
    //public DbSet<Location> Locations => Set<Location>();
    public DbSet<USERINFO> USERINFO => Set<USERINFO>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<Ori_ChangeItem> Ori_ChangeItem => Set<Ori_ChangeItem>();
    //public DbSet<Employee> Employees => Set<Employee>();
    //public DbSet<AttendanceLog> AttLogs => Set<AttendanceLog>();

    //protected override void OnModelCreating(ModelBuilder b)
    //{
    //    b.Entity<AttendanceLog>().HasIndex(x => x.HashDedup).IsUnique();
    //    b.Entity<Employee>().HasIndex(x => x.EnrollNumber);
    //    b.Entity<Device>().HasIndex(x => x.IP);
    //}
}
