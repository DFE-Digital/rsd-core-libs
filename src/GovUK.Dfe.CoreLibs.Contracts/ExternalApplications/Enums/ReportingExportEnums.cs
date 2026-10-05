namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

/// <summary>Whether a field's answers are exported to reporting.</summary>
public enum ReportingExportStatus
{
    /// <summary>No decision, and the default holds undecided fields back.</summary>
    Unclassified,
    Allowed,
    Denied,

    /// <summary>No decision, but exported because the default is <see cref="ReportingExportMode.ExportAll"/>.</summary>
    AllowedByDefault
}

/// <summary>An explicit decision about one field.</summary>
public enum ReportingExportDecision
{
    Allowed,
    Denied
}

/// <summary>What happens to fields nobody has made a decision about.</summary>
public enum ReportingExportMode
{
    /// <summary>Held back until someone allows them.</summary>
    ApproveFirst,

    /// <summary>Exported unless someone denies them.</summary>
    ExportAll
}

/// <summary>The default as set at one level. <see cref="Inherit"/> clears it so the next level applies.</summary>
public enum ReportingExportModeSetting
{
    Inherit,
    ApproveFirst,
    ExportAll
}

/// <summary>Where the default in force comes from.</summary>
public enum ReportingExportDefaultSource
{
    BuiltIn,
    Tenant,
    Template
}

public enum ReportingExportChangeStatus
{
    Applied,

    /// <summary>The request matched what was already in place, so nothing was saved and no refresh started.</summary>
    Unchanged
}

/// <summary>Progress of the refresh that re-exports a tenant's applications after a change.</summary>
public enum ReportingExportRefreshStatus
{
    Pending,
    Running,
    Completed,
    Cancelled,
    Failed
}
