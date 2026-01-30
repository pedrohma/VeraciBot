using VeraciLib.Entities;

namespace VeraciBot.Entities
{
    public class Config : BaseEntity
    {
        /// <summary>
        /// Nome do usuário
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
