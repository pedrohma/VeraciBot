using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using VeraciLib.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace VeraciBot.Entities
{
    public class TweetAuthor : BaseEntity
    {
        /// <summary>
        /// Tag do usuário (@)
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Nome do usuário
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Resultado do openai
        /// </summary>
        public int Value { get; set; } = 100;

        /// <summary>
        /// Retorna a descrição do autor do tweet
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return $"{Name} (@{UserName}) : {Value}";
        }
    }
}
