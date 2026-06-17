using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Reflection;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Infrastructure.Aop;

public class TransactionInterceptor : IInterceptor
{
    private readonly ILogger<TransactionInterceptor> _logger;

    public TransactionInterceptor(ILogger<TransactionInterceptor> logger)
    {
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var attr = invocation.Method.GetCustomAttribute<TransactionalAttribute>();
        if (attr == null)
        {
            invocation.Proceed();
            return;
        }

        _logger.LogInformation("Transaction started for method: {MethodName}", invocation.Method.Name);

        try
        {
            invocation.Proceed();
            _logger.LogInformation("Transaction committed for method: {MethodName}", invocation.Method.Name);
        }
        catch
        {
            _logger.LogError("Transaction rolled back for method: {MethodName}", invocation.Method.Name);
            throw;
        }
    }
}
