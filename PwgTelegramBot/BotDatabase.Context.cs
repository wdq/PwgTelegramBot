﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PwgTelegramBot
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class PwgTelegramBotEntities : DbContext
    {
        public PwgTelegramBotEntities()
            : base("name=PwgTelegramBotEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<HandledWebhook> HandledWebhooks { get; set; }
        public virtual DbSet<HarvestAuth> HarvestAuths { get; set; }
        public virtual DbSet<PivotalAuth> PivotalAuths { get; set; }
        public virtual DbSet<UserState> UserStates { get; set; }
        public virtual DbSet<UserTextEntry> UserTextEntries { get; set; }
    }
}
