﻿namespace DfE.CoreLibs.Security.Configurations
{
    public class ClaimDefinition
    {
        public required string Type { get; set; }
        public required List<string> Values { get; set; }
    }
}
