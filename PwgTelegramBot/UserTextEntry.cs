namespace PwgTelegramBot
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("UserTextEntry")]
    public partial class UserTextEntry
    {
        public Guid Id { get; set; }

        public int UserId { get; set; }

        public int EntryIndex { get; set; }

        [Required]
        public string EntryText { get; set; }
    }
}
