namespace DemoTransactions;

// Dedup/"inbox" marker living in the DESTINATION database (ShopDatabaseBackup). Id matches the
// source OutboxMessage.Id it corresponds to. Written in the SAME local transaction as the actual
// replicated data, so re-processing the same source message becomes a safe no-op.
public class ProcessedOutboxMessage
{
    public long Id { get; set; }
    public DateTime ProcessedAt { get; set; }
}
