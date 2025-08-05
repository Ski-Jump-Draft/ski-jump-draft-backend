namespace App.Application.Abstractions;

public record CommandEnvelope<TCommand>(TCommand Command, MessageContext MessageContext)
    where TCommand : ICommand;
    
public record CommandEnvelope<TCommand, TResponse>(TCommand Command, MessageContext Context)
    where TCommand : ICommand<TResponse>;