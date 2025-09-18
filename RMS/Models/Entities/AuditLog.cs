namespace RMS.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string EntityName { get; set; } = "";
        public int EntityId { get; set; }
        public string Action { get; set; } = ""; 
        public string? OldValues { get; set; } 
        public string? NewValues { get; set; } 
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
