namespace Application.Common.Exceptions
{
    public sealed class BadRequestException : QueuelessException
    {
        public BadRequestException(string message)
            : base(message)
        {
        }
    }
}
