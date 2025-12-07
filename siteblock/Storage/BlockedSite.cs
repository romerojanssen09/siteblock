using SQLite;

namespace siteblock.Storage
{
    [Table("blocked_sites")]
    public class BlockedSite
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed, NotNull]
        public string Domain { get; set; } = string.Empty;

        public string? Category { get; set; }

        public DateTime AddedDate { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }
    }
}
