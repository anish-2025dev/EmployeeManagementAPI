namespace EmployeeManagementApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public DateTime TimeStampUtc { get; set; }
    }
}
