﻿using DfE.CoreLibs.Security.Enums;

namespace DfE.CoreLibs.Security.Antiforgery
{
    public class CheckerGroup
    {
        public string[] TypeNames { get; set; } = [];
        public CheckerOperator CheckerOperator { get; set; } = CheckerOperator.Or;
    }
}
