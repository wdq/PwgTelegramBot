namespace PwgTelegramBot
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("UserState")]
    public partial class UserState
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        public string State { get; set; }

        public bool? Approved { get; set; }

        public string Notes { get; set; }

        public bool? IsAdmin { get; set; }

        public bool? IsStateTextEntry { get; set; }

        public int? ChatId { get; set; }
    }
}
