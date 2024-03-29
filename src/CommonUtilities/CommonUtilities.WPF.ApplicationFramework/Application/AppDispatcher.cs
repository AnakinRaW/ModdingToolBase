using System;
using System.Windows.Threading;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;

internal class AppDispatcher(Dispatcher dispatcher) : IAppDispatcher
{
    public void Invoke(Action action)
    {
        InvokeInternal(action);
    }

    public T Invoke<T>(Func<T> func)
    {
        return (T)InvokeInternal(func);
    }

    public void BeginInvoke(DispatcherPriority priority, Action action)
    {
        dispatcher.InvokeAsync(action, priority);
    }

    private object InvokeInternal(Delegate @delegate)
    {
        return !dispatcher.CheckAccess() ? dispatcher.Invoke(@delegate) : @delegate.DynamicInvoke();
    }
}