namespace LottyAB.Application.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string resource, string field, object value)
        : base($"{resource} with {field} '{value}' already exists.")
    {
    }
}