namespace Application.Common.Exceptions
{
    public sealed class ConflictException : QueuelessException
    {
        public ConflictException(string message)
            : base(message)
        {
        }
    }
}
