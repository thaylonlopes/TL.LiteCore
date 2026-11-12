using MediatR;
using TL.BaseContracts;

namespace TL.LiteCore.Application.Messaging;

public interface ICommand : MediatR.IRequest<Result>;
public interface ICommand<TResponse> : MediatR.IRequest<Result<TResponse>>;
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result> where TCommand : ICommand;
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>> where TCommand : ICommand<TResponse>;

public interface IQuery<TResponse> : MediatR.IRequest<Result<TResponse>>;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>;
