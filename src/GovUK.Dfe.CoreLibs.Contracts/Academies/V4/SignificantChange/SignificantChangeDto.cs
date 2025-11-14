namespace GovUK.Dfe.CoreLibs.Contracts.Academies.V4.SignificantChange;

public class SignificantChangeDto
{
    public int? SigChangeId { get; set; }
    public int? URN { get; set; }
    public int? TypeofGiasChangeId { get; set; }
    public string? TypeofSigChange { get; set; }
    public string? typeOfSigChangeMapped { get; set; }
    public string? CreatedUserName { get; set; }
    public string? EditedUserName { get; set; }
    public string? ApplicationType { get; set; }
    public string? DecisionDate { get; set; }
    public string? DeliveryLead { get; set; }
    public string? ChangeCreationDate { get; set; }
    public string? ChangeEditDate { get; set; }
    public bool? AllActionsCompleted { get; set; }
    public bool? Withdrawn { get; set; }
    public string? LocalAuthority { get; set; }
    public string? Region { get; set; }
    public string? TrustName { get; set; }
    public string? AcademyName { get; set; }
    public string? MetaIngestionDateTime { get; set; }
    public string? MetaSourceSystem { get; set; }
}
