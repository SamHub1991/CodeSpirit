using Castle.DynamicProxy;
using System.Reflection;

namespace CodeSpirit.Infrastructure.Aop;

public abstract class AsyncInterceptorBase : IInterceptor
{
    private static readonly MethodInfo ExecuteAsyncWithResultMethod =
        typeof(AsyncInterceptorBase).GetMethod(
            nameof(ExecuteAsyncWithResult),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    public void Intercept(IInvocation invocation)
    {
        if (!ShouldIntercept(invocation))
        {
            invocation.Proceed();
            return;
        }

        var returnType = invocation.Method.ReturnType;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            invocation.ReturnValue = ExecuteAsyncWithResultMethod
                .MakeGenericMethod(resultType)
                .Invoke(this, new object[] { invocation });
        }
        else if (returnType == typeof(Task))
        {
            invocation.ReturnValue = ExecuteAsync(invocation);
        }
        else
        {
            ExecuteSync(invocation);
        }
    }

    protected abstract bool ShouldIntercept(IInvocation invocation);
    protected abstract void ExecuteSync(IInvocation invocation);
    protected abstract Task ExecuteAsync(IInvocation invocation);
    protected abstract Task<T> ExecuteAsyncWithResult<T>(IInvocation invocation);
}
