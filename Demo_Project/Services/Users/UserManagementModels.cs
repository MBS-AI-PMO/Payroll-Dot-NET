namespace PayslipMgtSystem.Services.Users
{
    public enum BulkUserStatus
    {
        Created,
        AlreadyExists,
        MissingEmail,
        Failed
    }

    public class BulkUserRecord
    {
        public string? EmployeeName { get; set; }
        public string? Cnic { get; set; }
        public string? Email { get; set; }
        public BulkUserStatus Status { get; set; }
        public string? Reason { get; set; }
        public bool InvitationFailed { get; set; }
    }

    public class BulkUserResult
    {
        public int TotalEmployees { get; set; }
        public int Created { get; set; }
        public int AlreadyExisting { get; set; }
        public int MissingEmail { get; set; }
        public int Failed { get; set; }
        public int InvitationsFailed { get; set; }
        public bool WasCancelled { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<BulkUserRecord> Records { get; set; } = new();
    }

    public record BulkUserProgress(int Processed, int Total, string? CurrentEmployee);

    public class UserOverview
    {
        public int TotalEmployees { get; set; }
        public int EmployeeAccounts { get; set; }
        public int PendingSetup { get; set; }
        public int WithoutAccount => Math.Max(TotalEmployees - EmployeeAccounts, 0);
    }

    public class EmployeeAccount
    {
        public string Id { get; set; } = "";
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Cnic { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool PasswordSet { get; set; }
        public bool IsLockedOut { get; set; }
    }

    public class EmployeeAccountPage
    {
        public List<EmployeeAccount> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
