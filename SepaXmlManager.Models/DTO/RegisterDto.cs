using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.DTO
{
    /// <summary>
    /// Modelo de dados DTO para o registo de novos utilizadores no sistema.
    /// </summary>
    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Tesouraria"; // Perfil predefinido.
    }
}