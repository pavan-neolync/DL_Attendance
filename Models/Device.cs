using System.ComponentModel.DataAnnotations;

namespace DLAttendance.Models;

public enum DeviceStatus
{
    Active = 1,
    Inactive = 2
}

public class Device
{
    public int Id { get; set; }

    [Required, StringLength(100), Display(Name = "Device Name")]
    public string DeviceName { get; set; } = string.Empty;

    [Required, StringLength(45), Display(Name = "IP Address")]
    public string IpAddress { get; set; } = string.Empty;

    [Required, StringLength(80), Display(Name = "Serial Number")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Location { get; set; } = string.Empty;

    public DeviceStatus Status { get; set; } = DeviceStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
