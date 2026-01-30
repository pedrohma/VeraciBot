using System.ComponentModel.DataAnnotations;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using VeraciLib.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace VeraciBot.Entities
{
    public enum AuthorizationStatus
    {
        NotAuthorized = 0,
        Authorized = 1,
        Invited = 2,
    }

    public class AuthorizedUser : BaseEntity
    {
        /// <summary>
        /// Quem autorizou o usuário
        /// </summary>
        public string AuthorizedById { get; set; } = string.Empty;

        /// <summary>
        /// Quando o usuário foi autorizado (UTC)
        /// </summary>
        public DateTime AuthorizationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Autorizado mesmo?
        /// </summary>
        public AuthorizationStatus Status { get; set; } = AuthorizationStatus.NotAuthorized; // 0 = Não, 1 = Sim, 2 = Convidado

        /// <summary>
        /// Autorização máxima por usuário
        /// </summary>
        static public int MaxAuthorizationsPerUser = 5;

        /// <summary>
        /// Número de autorizações disponíveis
        /// </summary>
        public int NumberOfAuthorizations { get; set; } = MaxAuthorizationsPerUser;
    }
}
