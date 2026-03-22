namespace LottyAB.Application.Exceptions;

public class UnprocessableEntityException : Exception
{
    public UnprocessableEntityException(string message) : base(message)
    {
    }

    public UnprocessableEntityException(string name, object key) : base($"{name} with id '{key}' isn't processable.")
    {
    }
}