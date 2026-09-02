namespace Application.Common.Exceptions
{
    public sealed class ForbiddenException : QueuelessException
    {
        public ForbiddenException(string message)
            : base(message)
        {
        }
    }
}
