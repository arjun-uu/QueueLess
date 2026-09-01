namespace Infrastructure.Email
{
    using Application.Interfaces;
    using MailKit.Net.Smtp;
    using Microsoft.Extensions.Options;
    using MimeKit;

    public class EmailService(IOptions<EmailSettings> _settings) : IEmailService
    {
        public async Task SendEmailAsync(
            string to,
            string subject,
            string body)
        {
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress("QueueLess", _settings.Value.Username));

            email.To.Add(
                MailboxAddress.Parse(to));

            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = body
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _settings.Value.Host,
                _settings.Value.Port,
                MailKit.Security.SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _settings.Value.Username,
                _settings.Value.Password);

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
    }
}
