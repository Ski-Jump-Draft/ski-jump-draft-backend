namespace App.Application.Draft;

public class DraftException : System.Exception
{
    public Domain.Draft.Error Error { get; }
    public DraftException(Domain.Draft.Error code)
        : base(code.ToString()) 
    {
        Error = code;
    }
}