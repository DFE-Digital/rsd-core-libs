using GovUK.Dfe.CoreLibs.Security.Enums;

namespace GovUK.Dfe.CoreLibs.Security.Antiforgery
{
    public class CheckerGroup
    {
        public string[] TypeNames { get; set; } = [];
        public CheckerOperator CheckerOperator { get; set; } = CheckerOperator.Or;
    }
}
