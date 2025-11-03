using GovUK.Dfe.CoreLibs.Contracts.Academies.Base;
using GovUK.Dfe.CoreLibs.Contracts.Academies.V4.Establishments;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Contracts.Academies.V4.Trusts;

[Serializable]
[ExcludeFromCodeCoverage]
public class TrustDto
{
    public string Name { get; set; }

    public string Ukprn { get; set; }
    public NameAndCodeDto Type { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public string ReferenceNumber { get; set; }

    public AddressDto Address { get; set; }

}
