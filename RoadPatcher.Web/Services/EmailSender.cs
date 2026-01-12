using RoadPatcher.Application.Entities;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace RoadPatcher.Services;

public class EmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var senderEmail = configuration["EmailSettings:Sender"]!;
        var senderPassword = configuration["EmailSettings:Password"];
        var host = configuration["EmailSettings:Host"];
        var port = int.Parse(configuration["EmailSettings:Port"]!);

        using var client = new SmtpClient(host, port);
        client.EnableSsl = true;
        client.UseDefaultCredentials = false;
        client.Credentials = new NetworkCredential(senderEmail, senderPassword);

        using var mailMessage = new MailMessage(from: senderEmail, to: email, subject, htmlMessage);
        mailMessage.IsBodyHtml = true;

        await client.SendMailAsync(mailMessage);
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        const string subject = "Confirm your email";
        var htmlMessage = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.";
        
        await SendEmailAsync(email, subject, htmlMessage);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        const string subject = "Reset your password";
        var htmlMessage = $"Please reset your password by <a href='{resetLink}'>clicking here</a>.";
        
        await SendEmailAsync(email, subject, htmlMessage);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        const string subject = "Reset your password code";
        var htmlMessage = $"Please reset your password using the following code: <b>{resetCode}</b>";
        
        await SendEmailAsync(email, subject, htmlMessage);
    }
}