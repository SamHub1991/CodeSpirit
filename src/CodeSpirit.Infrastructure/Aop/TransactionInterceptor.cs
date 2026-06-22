using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Infrastructure.Aop;

public class TransactionInterceptor : AsyncInterceptorBase
{
    private readonly ILogger<TransactionInterceptor> _logger;
    private static readonly ConcurrentDictionary<Type, FieldInfo?> _dbContextFieldCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _dbContextPropCache = new();

    public TransactionInterceptor(ILogger<TransactionInterceptor> logger)
    {
        _logger = logger;
    }

    protected override bool ShouldIntercept(IInvocation invocation) =>
        invocation.Method.GetCustomAttribute<TransactionalAttribute>() != null;

    protected override void ExecuteSync(IInvocation invocation)
    {
        var dbContext = ResolveDbContext(invocation);
        IDbContextTransaction? tx = null;

        try
        {
            if (dbContext != null)
            {
                tx = dbContext.Database.BeginTransaction();
                _logger.LogInformation("Transaction started: {Method}", invocation.Method.Name);
            }

            invocation.Proceed();

            if (dbContext != null)
            {
                dbContext.SaveChanges();
                tx?.Commit();
                _logger.LogInformation("Transaction committed: {Method}", invocation.Method.Name);
            }
        }
        catch
        {
            tx?.Rollback();
            _logger.LogError("Transaction rolled back: {Method}", invocation.Method.Name);
            throw;
        }
        finally
        {
            tx?.Dispose();
        }
    }

    protected override async Task ExecuteAsync(IInvocation invocation)
    {
        var dbContext = ResolveDbContext(invocation);
        IDbContextTransaction? tx = null;

        try
        {
            if (dbContext != null)
            {
                tx = await dbContext.Database.BeginTransactionAsync();
                _logger.LogInformation("Transaction started: {Method}", invocation.Method.Name);
            }

            invocation.Proceed();
            await (Task)invocation.ReturnValue!;

            if (dbContext != null)
            {
                await dbContext.SaveChangesAsync();
                if (tx != null) await tx.CommitAsync();
                _logger.LogInformation("Transaction committed: {Method}", invocation.Method.Name);
            }
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync();
                _logger.LogError("Transaction rolled back: {Method}", invocation.Method.Name);
            }
            throw;
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    protected override async Task<T> ExecuteAsyncWithResult<T>(IInvocation invocation)
    {
        var dbContext = ResolveDbContext(invocation);
        IDbContextTransaction? tx = null;

        try
        {
            if (dbContext != null)
            {
                tx = await dbContext.Database.BeginTransactionAsync();
                _logger.LogInformation("Transaction started: {Method}", invocation.Method.Name);
            }

            invocation.Proceed();
            var result = await (Task<T>)invocation.ReturnValue!;

            if (dbContext != null)
            {
                await dbContext.SaveChangesAsync();
                if (tx != null) await tx.CommitAsync();
                _logger.LogInformation("Transaction committed: {Method}", invocation.Method.Name);
            }

            return result;
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync();
                _logger.LogError("Transaction rolled back: {Method}", invocation.Method.Name);
            }
            throw;
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    private static DbContext? ResolveDbContext(IInvocation invocation)
    {
        object? target;

        if (invocation.Proxy is IProxyTargetAccessor accessor)
            target = accessor.DynProxyGetTarget();
        else
            target = invocation.InvocationTarget;

        if (target == null) return null;

        var targetType = target.GetType();

        var field = _dbContextFieldCache.GetOrAdd(targetType, static t =>
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (typeof(DbContext).IsAssignableFrom(f.FieldType))
                    return f;
            }
            return null;
        });

        if (field != null)
            return field.GetValue(target) as DbContext;

        var prop = _dbContextPropCache.GetOrAdd(targetType, static t =>
        {
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (typeof(DbContext).IsAssignableFrom(p.PropertyType) && p.CanRead)
                    return p;
            }
            return null;
        });

        return prop?.GetValue(target) as DbContext;
    }
}
