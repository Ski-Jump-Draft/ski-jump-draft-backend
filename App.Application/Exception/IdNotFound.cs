namespace App.Application.Exception;

public class IdNotFoundException<TId> : KeyNotFoundException
{
    public TId Id { get; }
    
    public IdNotFoundException(TId id)
    {
        Id = id;
    }

    public IdNotFoundException(string message, TId id) : base(message)
    {
        Id = id;
    }

    public IdNotFoundException(string message, System.Exception inner, TId id) : base(message, inner)
    {
        Id = id;
    }
}