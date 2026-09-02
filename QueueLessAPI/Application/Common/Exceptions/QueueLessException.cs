namespace Application.Common.Exceptions;

public abstract class QueuelessException : Exception
{
    protected QueuelessException(string message)
        : base(message)
    {
    }

    protected QueuelessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
