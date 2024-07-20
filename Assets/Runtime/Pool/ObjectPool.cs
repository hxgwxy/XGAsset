using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XGAsset.Runtime.Pool
{
    public static class ObjectPool
    {
        private static Dictionary<Type, Queue<object>> QueuePool = new Dictionary<Type, Queue<object>>();

        public static T Get<T>() where T : new()
        {
            var queue = GetOrAddQueue<T>();
            return queue.Count > 0 ? (T)queue.Dequeue() : new T();
        }

        public static void Put<T>(T value, Action<T> recycleAction = null) where T : new()
        {
            if (value == null)
                return;
            (value as IDictionary)?.Clear();
            (value as ICollection<T>)?.Clear();
            recycleAction?.Invoke(value);
            var queue = GetOrAddQueue<T>();
            if (!queue.Contains(value))
                queue.Enqueue(value);
            else
                Debug.LogError($"重复回收对象");
        }

        // public static void Put<T>(T obj) where T : IPoolObject, new()
        // {
        //     obj.Clear();
        //     var queue = GetOrAddQueue<T>();
        //     if (!queue.Contains(obj))
        //         queue.Enqueue(obj);
        //     else
        //         Debug.LogError($"重复回收对象 IPoolObject");
        // }

        public static void Release<T>()
        {
            var queue = GetQueue(typeof(Queue<T>));
            if (queue != null)
                queue.Clear();
        }

        public static void Release(Type type)
        {
            if (type == null)
                return;
            var queueType = typeof(Queue<>).MakeGenericType(type);
            var queue = GetQueue(queueType);
            if (queue != null)
                queue.Clear();
        }

        private static Queue<object> GetOrAddQueue<T>() where T : new()
        {
            var queue = GetQueue<T>();
            if (queue == null)
            {
                queue = new Queue<object>();
                QueuePool.Add(typeof(Queue<T>), queue);
            }

            return queue;
        }

        private static Queue<object> GetQueue<T>() where T : new()
        {
            return GetQueue(typeof(Queue<T>));
        }

        private static Queue<object> GetQueue(Type type)
        {
            QueuePool.TryGetValue(type, out var queue);
            return queue;
        }
    }
}