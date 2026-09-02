namespace Application.Common.Exceptions
{
    public sealed class UnauthorizedException : QueuelessException
    {
        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
}
