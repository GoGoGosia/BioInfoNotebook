using SQLite;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioInfoNotebook.Models
{
    [SQLite.Table("SesjeAnalityczne")]
    public class SesjaAnalityczna
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string TytulSesji { get; set; } = string.Empty;

        public DateTime DataUtworzenia { get; set; } = DateTime.Now;
    }
}
