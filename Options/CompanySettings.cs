namespace DLAttendance.Options;

public class CompanySettings
{
    public const string SectionName = "Company";

    public string Name { get; set; } = "Neolync Tele Communications Private Limited";

    public string AddressLine1 { get; set; } = "Survey No 228, 229 & 230, IT Park";

    public string AddressLine2 { get; set; } = "Kurukalva Village, Renigunta Mandal";

    public string City { get; set; } = "Tirupati - Andhra Pradesh";

    public string Pincode { get; set; } = "517520";

    public string FullAddress =>
        $"{AddressLine1}, {AddressLine2}, {City} - {Pincode}";
}
