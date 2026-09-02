namespace Application.Common.Exceptions
{
    public sealed class NotFoundException : QueuelessException
    {
        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
