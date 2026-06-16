using SQLite;
using System.ComponentModel.DataAnnotations.Schema;

namespace lab12_BioInfoNotebook.Models
{
    [SQLite.Table("ZalacznikiPlikow")]
    public class ZalacznikPliku
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Klucz obcy łączący załącznik z konkretnym wpisem tekstowym
        [Indexed]
        public int WpisId { get; set; }

        public string NazwaPliku { get; set; } = string.Empty;

        // Ścieżka do miejsca, gdzie aplikacja zapisała kopię załącznika
        public string SciezkaKopii { get; set; } = string.Empty;

        // Typ pliku: FASTA, CSV, PNG
        public string TypRozszerzenia { get; set; } = string.Empty;
    }
}