using System;
using System.Collections.Concurrent;

namespace JsonRpcLite.Utilities
{
    public class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;

        /// <summary>
        /// Rent an object from pool.
        /// </summary>
        /// <returns>The item from the pool.</returns>
        public T Get() => _objects.TryTake(out T item) ? item : _objectGenerator();


        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        /// <param name="item">The return item.</param>
        public void Return(T item) => _objects.Add(item);

        public ObjectPool(Func<T> objectGenerator)
        {
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objects = new ConcurrentBag<T>();
        }
    }
}
