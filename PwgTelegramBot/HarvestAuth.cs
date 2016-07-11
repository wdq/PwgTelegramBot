namespace PwgTelegramBot
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("HarvestAuth")]
    public partial class HarvestAuth
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        [Required]
        public string HarvestCode { get; set; }

        public string HarvestToken { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? HarvestTokenExpiration { get; set; }

        public string HarvestRefreshToken { get; set; }
    }
}
