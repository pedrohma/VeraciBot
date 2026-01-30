using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using VeraciLib.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace VeraciBot.Entities
{
    public class Invite : BaseEntity
    {
        /// <summary>
        /// Tag do usuário (@)
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Nome do usuário
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
