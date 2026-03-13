using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface IEmailTemplateService
    {
        string GenerateEmailConfirmationTemplate(string otp, string firstName, string expiryMinutes, string confirmUrl);
        string GenerateOtpEmailTemplate(string otp, string userName, string expiryMinutes); 

    }
}
