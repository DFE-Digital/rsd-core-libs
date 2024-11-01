namespace DfE.CoreLibs.Testing.Results
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? Message { get; }

        private ValidationResult(bool isValid, string? message = null)
        {
            IsValid = isValid;
            Message = message;
        }

        public static ValidationResult Success() => new ValidationResult(true);
        public static ValidationResult Failed(string message) => new ValidationResult(false, message);

        public override string ToString() => IsValid ? "Validation passed" : $"Validation failed: {Message}";
    }
}
