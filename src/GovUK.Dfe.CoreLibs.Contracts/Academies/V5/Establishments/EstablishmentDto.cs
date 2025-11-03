using GovUK.Dfe.CoreLibs.Contracts.Academies.Base;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Contracts.Academies.V5.Establishments;

[Serializable]
[ExcludeFromCodeCoverage]
public class EstablishmentDto: EstablishmentBaseDto
{
    public ReportCardDto? ReportCard { get; set; }
} 

[Serializable]
public class ReportCardDto
{
	public string? WebLink { get; set; }
	public string? LatestInspectionDate { get; set; }
	public string? LatestCurriculumAndTeaching { get; set; }
	public string? LatestAttendanceAndBehaviour { get; set; }
	public string? LatestPersonalDevelopmentAndWellbeing { get; set; }
	public string? LatestLeadershipAndGovernance { get; set; }
	public string? LatestInclusion { get; set; }
	public string? LatestAchievement { get; set; }
	public string? LatestEarlyYearsProvision { get; set; }
	public string? LatestSafeguarding { get; set; }
	public string? PreviousInspectionDate { get; set; }
	public string? PreviousCurriculumAndTeaching { get; set; }
	public string? PreviousAttendanceAndBehaviour { get; set; }
	public string? PreviousPersonalDevelopmentAndWellbeing { get; set; }
	public string? PreviousLeadershipAndGovernance { get; set; }
	public string? PreviousInclusion { get; set; }
	public string? PreviousAchievement { get; set; }
	public string? PreviousEarlyYearsProvision { get; set; }
	public string? PreviousSafeguarding { get; set; }
}