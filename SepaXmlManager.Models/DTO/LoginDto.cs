using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.DTO
{
    /// <summary>
    /// Modelo de dados DTO para o processo de autenticação (Login) de utilizadores.
    /// </summary>
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
