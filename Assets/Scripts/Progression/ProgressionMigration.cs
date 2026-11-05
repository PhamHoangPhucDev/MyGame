namespace GameplayPrototype
{
    /// <summary>Legacy save steps. Each one is idempotent and keeps the old keys untouched.</summary>
    public static class ProgressionMigration
    {
        public static void Run()
        {
            Farming.MigrateLegacy();
            PendingRewards.MigrateLegacy();
        }
    }
}
