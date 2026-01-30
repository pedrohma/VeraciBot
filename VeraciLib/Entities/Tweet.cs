using VeraciLib.Entities;

namespace VeraciBot.Entities
{
    public class Tweet : BaseEntity
    {
        /// <summary>
        /// Texto verificado
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Author que chamou o veracibot
        /// </summary>
        public string AuthorId { get; set; } = string.Empty;

        /// <summary>
        /// Id do tweet original
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;

        /// <summary>
        /// Author que chamou o veracibot
        /// </summary>
        public string OriginalAuthorId { get; set; } = string.Empty;

        /// <summary>
        /// Texto verificado
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// Resultado do openai
        /// </summary>
        public int Result { get; set; } = 0;

        /// <summary>
        /// Data e hora do registro
        /// </summary>
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
