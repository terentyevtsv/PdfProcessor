using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Db
{
    /// <summary>
    /// Сущность документа
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// Название файла
        /// </summary>
        [Column("file_name")]
        public string FileName { get; set; }

        /// <summary>
        /// Статус
        /// </summary>
        [Column("status")]
        public Status Status { get; set; }

        /// <summary>
        /// Извлеченный текст
        /// </summary>
        [Column("extracted_text")]
        public string? ExtractedText { get; set; }
    }

    public enum Status
    {
        // Ожидание
        Pending,

        // Завершен
        Completed,

        // Провален
        Failed
    }
}
