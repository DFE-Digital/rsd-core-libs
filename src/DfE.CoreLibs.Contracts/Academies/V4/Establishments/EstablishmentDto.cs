using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Contracts.Academies.V4.Establishments;

[Serializable]
[ExcludeFromCodeCoverage]
public class EstablishmentDto
{

    public string Ukprn { get; set; }
    public string Urn { get; set; }
    public string Name { get; set; }
    public string LocalAuthorityCode { get; set; }
    public string LocalAuthorityName { get; set; }
    public string OfstedRating { get; set; }
    public string OfstedLastInspection { get; set; }
    public string StatutoryLowAge { get; set; }
    public string StatutoryHighAge { get; set; }
    public string SchoolCapacity { get; set; }
    public string Pfi { get; set; }
    public string EstablishmentNumber { get; set; }
    public string Pan { get; set; }
    public string Deficit { get; set; }
    public string ViabilityIssue { get; set; }
    public string GiasLastChangedDate { get; set; }
    public string NoOfBoys { get; set; }
    public string NoOfGirls { get; set; }
    public string SenUnitCapacity { get; set; }
    public string SenUnitOnRoll { get; set; }
    public string ReligousEthos { get; set; }

    public string HeadteacherTitle { get; set; }
    public string HeadteacherFirstName { get; set; }
    public string HeadteacherLastName { get; set; }
    public string HeadteacherPreferredJobTitle { get; set; }

    public NameAndCodeDto Diocese { get; set; }
    public NameAndCodeDto EstablishmentType { get; set; }
    public NameAndCodeDto Gor { get; set; }
    public NameAndCodeDto PhaseOfEducation { get; set; }
    public NameAndCodeDto ReligiousCharacter { get; set; }
    public NameAndCodeDto ParliamentaryConstituency { get; set; }
    public CensusDto Census { get; set; }
    public MisEstablishmentDto MISEstablishment { get; set; }
    public AddressDto Address { get; set; }
    public PreviousEstablishmentDto PreviousEstablishment { get; set; }   
}

[Serializable]
public class NameAndCodeDto
{
    public string Name { get; set; }
    public string Code { get; set; }
}


[Serializable]
public class MisEstablishmentDto
{
    public string DateOfLatestSection8Inspection { get; set; }
    public string InspectionEndDate { get; set; }

    public string OverallEffectiveness { get; set; }
    public string QualityOfEducation { get; set; }
    public string BehaviourAndAttitudes { get; set; }
    public string PersonalDevelopment { get; set; }
    public string EffectivenessOfLeadershipAndManagement { get; set; }

    public string EarlyYearsProvision { get; set; }
    public string SixthFormProvision { get; set; }
    public string Weblink { get; set; }
}

[Serializable]
public class CensusDto
{
    public string NumberOfPupils { get; set; }
    public string PercentageFsm { get; set; }
    public string PercentageFsmLastSixYears { get; set; }
    public string PercentageEnglishAsSecondLanguage { get; set; }
    public string PercentageSen { get; set; }
}

[Serializable]
public class PreviousEstablishmentDto
{
    public string Urn { get; set; }
}
