using DLAttendance.Models;

namespace DLAttendance.ViewModels;

public class DeviceIndexViewModel
{
    public string? Search { get; set; }

    public PagedResult<Device> PagedDevices { get; set; } = new();
}
