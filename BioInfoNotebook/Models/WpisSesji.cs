using SQLite;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioInfoNotebook.Models
{
    [SQLite.Table("WpisySesji")]
    public class WpisSesji
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Klucz obcy łączący wpis z konkretną sesją
        [Indexed]
        public int SesjaId { get; set; }

        public string TrescWpisu { get; set; } = string.Empty;

        public DateTime DataDodania { get; set; } = DateTime.Now;
    }
}