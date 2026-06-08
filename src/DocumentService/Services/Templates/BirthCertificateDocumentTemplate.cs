using DocumentService.Services;

namespace DocumentService.Services.Templates;

public static class BirthCertificateDocumentTemplate
{
    public static List<(string Label, string Value)> GetFields(CitizenData citizen)
    {
        return new()
        {
            ("Full Name", citizen.FullName),
            ("Date of Birth", citizen.DateOfBirth.ToString("MMMM d, yyyy")),
            ("Place of Birth", citizen.City + ", Democria"),
            ("Gender", citizen.Gender),
            ("National ID", citizen.NationalId),
            ("", ""),
            ("Father", "[On Record]"),
            ("Mother", "[On Record]"),
        };
    }
}
