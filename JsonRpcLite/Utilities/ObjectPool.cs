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
        public T Rent() => _objects.TryTake(out T item) ? item : _objectGenerator();


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


    public class ObjectPool<T,TParam>
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<TParam,T> _objectGenerator;
        private readonly Action<T,TParam> _objectUpdater;

        /// <summary>
        /// Rent an object from pool.
        /// </summary>
        /// <returns>The item from the pool.</returns>
        public T Rent(TParam parameter)
        {
            if (_objects.TryTake(out var item))
            {
                _objectUpdater(item, parameter);
            }
            else
            {
                item = _objectGenerator(parameter);
            }
            return item;
        }


        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        /// <param name="item">The return item.</param>
        public void Return(T item) => _objects.Add(item);

        public ObjectPool(Func<TParam, T> objectGenerator, Action<T, TParam> objectUpdater)
        {
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objectUpdater = objectUpdater ?? throw new ArgumentNullException(nameof(objectUpdater));
            _objects = new ConcurrentBag<T>();
        }
    }
}
