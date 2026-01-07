using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Contracts.Academies.V4;

[Serializable]
[ExcludeFromCodeCoverage]
public class AddressDto
{
    public string Street { get; set; }

    public string Town { get; set; }

    public string County { get; set; }

    public string Postcode { get; set; }

    public string Locality { get; set; }

    public string Additional { get; set; }
}
