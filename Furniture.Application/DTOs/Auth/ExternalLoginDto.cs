using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.DTOs.Auth
{
    public class ExternalLoginDto
    {
        public string Provider { get; set; }   
        public string IdToken { get; set; }  
    }
}
