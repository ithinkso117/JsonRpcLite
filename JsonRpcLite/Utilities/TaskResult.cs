using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace JsonRpcLite.Utilities
{
    internal static class TaskResult
    {
        private static readonly ConcurrentDictionary<Type, Lazy<Func<Task, Task<object>>>> Caches = new();

        private static readonly Func<Type, Lazy<Func<Task, Task<object>>>> Factory = type => new Lazy<Func<Task, Task<object>>>(() => GetFunc(type));

        private static async Task<object> GetTask<T>(Task<T> task) => await task.ConfigureAwait(false);

        private static Func<Task, Task<object>> GetFunc(Type type)
        {
            var resultType = type.GetGenericArguments()[0];
            var method = typeof(TaskResult).GetMethod(nameof(GetTask), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(resultType);
            var task = Expression.Parameter(typeof(Task), "task");
            return Expression.Lambda<Func<Task, Task<object>>>(
                Expression.Call(method, Expression.Convert(task, type)),
                task
            ).Compile();
        }

        public static async Task<object> Get(Task task)
        {
            var type = task.GetType();
            if (type.IsGenericType)
            {
                return await Caches.GetOrAdd(type, Factory).Value(task).ConfigureAwait(false);
            }
            await task.ConfigureAwait(false);
            return null;
        }
    }
}
