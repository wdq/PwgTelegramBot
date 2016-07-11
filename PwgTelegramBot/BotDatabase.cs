namespace PwgTelegramBot
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class BotDatabase : DbContext
    {
        public BotDatabase()
            : base("name=BotDatabase")
        {
        }

        public virtual DbSet<HarvestAuth> HarvestAuths { get; set; }
        public virtual DbSet<PivotalAuth> PivotalAuths { get; set; }
        public virtual DbSet<UserState> UserStates { get; set; }
        public virtual DbSet<UserTextEntry> UserTextEntries { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}
