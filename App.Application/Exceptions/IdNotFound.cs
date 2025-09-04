namespace App.Application.Exceptions;

public class IdNotFoundException : KeyNotFoundException
{
    public Guid Id { get; }
    
    public IdNotFoundException(Guid id)
    {
        Id = id;
    }

    public IdNotFoundException(string message, Guid id) : base(message)
    {
        Id = id;
    }

    public IdNotFoundException(string message, System.Exception inner, Guid id) : base(message, inner)
    {
        Id = id;
    }
}