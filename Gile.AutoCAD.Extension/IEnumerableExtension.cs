﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Gile.AutoCAD.Extension
{
    /// <summary>
    /// Defines methods to add or removes items from a sequence of disposable objects.
    /// </summary>
    /// <typeparam name="T">Type of the items.</typeparam>
    public interface IDisposableCollection<T> : ICollection<T>, IDisposable
        where T : IDisposable
    {
        /// <summary>
        /// Adds items to the sequence.
        /// </summary>
        /// <param name="items">Items to add.</param>
        void AddRange(IEnumerable<T> items);

        /// <summary>
        /// Removes items from the sequence.
        /// </summary>
        /// <param name="items">Items to remove.</param>
        /// <returns>The sequence of removed items.</returns>
        IEnumerable<T> RemoveRange(IEnumerable<T> items);
    }

    /// <summary>
    /// Provides extension methods for the IEnumerable(T) type.
    /// </summary>
    public static class IEnumerableExtension
    {
        /// <summary>
        /// Opens the objects which type matches to the given one, and return them.
        /// </summary>
        /// <typeparam name="T">Type of object to return.</typeparam>
        /// <param name="source">Sequence of ObjectIds.</param>
        /// <param name="mode">Open mode to obtain in.</param>
        /// <param name="openErased">Value indicating whether to obtain erased objects.</param>
        /// <param name="forceOpenOnLockedLayers">Value indicating if locked layers should be opened.</param>
        /// <returns>The sequence of opened objects.</returns>
        /// <exception cref="System.ArgumentNullException">Throw if <c>source</c> is null.</exception>
        /// <exception cref="Autodesk.AutoCAD.Runtime.Exception">eNoActiveTransactions is thrown if there is no active transaction.</exception>
        public static IEnumerable<T> GetObjects<T>(
          this IEnumerable<ObjectId> source,
          OpenMode mode = OpenMode.ForRead,
          bool openErased = false,
          bool forceOpenOnLockedLayers = false) where T : DBObject
        {
            Assert.IsNotNull(source, nameof(source));

            if (source.Any())
            {
                var tr = source.First().Database.GetTopTransaction();
                var rxClass = RXObject.GetClass(typeof(T));
                foreach (ObjectId id in source)
                {
                    if (id.ObjectClass.IsDerivedFrom(rxClass))
                    {
                        if (!id.IsErased || openErased)
                            yield return (T)tr.GetObject(id, mode, openErased, forceOpenOnLockedLayers);
                    }
                }
            }
        }

        /// <summary>
        /// Upgrades the open mode of all objects in the sequence.
        /// </summary>
        /// <typeparam name="T">Type of objects.</typeparam>
        /// <param name="source">Sequence of DBObjects to upgrade.</param>
        /// <returns>The sequence of opened for write objects (objets on locked layers are discared).</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        /// <exception cref="Autodesk.AutoCAD.Runtime.Exception">eNoActiveTransactions is thrown if there's no active transaction.</exception>
        public static IEnumerable<T> UpgradeOpen<T>(this IEnumerable<T> source) where T : DBObject
        {
            Assert.IsNotNull(source, nameof(source));
            foreach (T item in source)
            {
                try
                {
                    item.OpenForWrite();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    if (ex.ErrorStatus != ErrorStatus.OnLockedLayer)
                        throw;
                    continue;
                }
                yield return item;
            }
        }

        /// <summary>
        /// Disposes of all items of the sequence.
        /// </summary>
        /// <typeparam name="T">Type of the items.</typeparam>
        /// <param name="source">Sequence of disposable objects.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        public static void DisposeAll<T>(this IEnumerable<T> source) where T : IDisposable
        {
            Assert.IsNotNull(source, nameof(source));
            if (source.Any())
            {
                System.Exception last = null;
                foreach (T item in source)
                {
                    if (item != null)
                    {
                        try
                        {
                            item.Dispose();
                        }
                        catch (System.Exception ex)
                        {
                            last = last ?? ex;
                        }
                    }
                }
                if (last != null)
                    throw last;
            }
        }

        /// <summary>
        /// Runs the action for each item of the collection.
        /// </summary>
        /// <typeparam name="T">Type of the items.</typeparam>
        /// <param name="source">Sequence to process.</param>
        /// <param name="action">Action to run.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>action</c> is null.</exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            Assert.IsNotNull(source, nameof(source));
            Assert.IsNotNull(action, nameof(action));
            foreach (T item in source) action(item);
        }

        /// <summary>
        /// Runs the indexed action for each item of the collection.
        /// </summary>
        /// <typeparam name="T">Type of the items.</typeparam>
        /// <param name="source">Sequence to process.</param>
        /// <param name="action">Indexed action to run.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>action</c> is null.</exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            Assert.IsNotNull(source, nameof(source));
            Assert.IsNotNull(action, nameof(action));
            int i = 0;
            foreach (T item in source) action(item, i++);
        }

        /// <summary>
        /// Gets the greatest item of the sequence using the default comparer with the <c>selector</c> function returned values.
        /// </summary>
        /// <typeparam name="TSource">Type the items.</typeparam>
        /// <typeparam name="TKey">Type of the returned value of <c>selector</c> function.</typeparam>
        /// <param name="source">Sequence to which the method applies.</param>
        /// <param name="selector">Mapping function from <c>TSource</c> to <c>TKey</c>.</param>
        /// <returns>The greatest item in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>selector</c> is null.</exception>
        public static TSource MaxBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> selector)
        {
            return source.MaxBy(selector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Gets the greatest item of the sequence using <c>comparer</c> with the <c>selector</c> function returned values.
        /// </summary>
        /// <typeparam name="TSource">Type the items.</typeparam>
        /// <typeparam name="TKey">Type of the returned value of <c>selector</c> function.</typeparam>
        /// <param name="source">Sequence to which the method applies.</param>
        /// <param name="selector">Mapping function from <c>TSource</c> to <c>TKey</c>.</param>
        /// <param name="comparer">Comparer used fot the <c>TKey</c> type.</param>
        /// <returns>The greatest item in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>selector</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>comparer</c> is null.</exception>
        public static TSource MaxBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> selector,
            IComparer<TKey> comparer)
        {
            Assert.IsNotNull(source, nameof(source));
            Assert.IsNotNull(selector, nameof(selector));
            Assert.IsNotNull(comparer, nameof(comparer));
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                    throw new InvalidOperationException("Séquence vide");

                var max = iterator.Current;
                var maxKey = selector(max);
                while (iterator.MoveNext())
                {
                    var current = iterator.Current;
                    var currentKey = selector(current);
                    if (comparer.Compare(currentKey, maxKey) > 0)
                    {
                        max = current;
                        maxKey = currentKey;
                    }
                }
                return max;
            }
        }

        /// <summary>
        /// Gets the smallest item of the sequence using the default comparer with the <c>selector</c> function returned values.
        /// </summary>
        /// <typeparam name="TSource">Type the items.</typeparam>
        /// <typeparam name="TKey">Type of the returned value of <c>selector</c> function.</typeparam>
        /// <param name="source">Sequence to which the method applies.</param>
        /// <param name="selector">Mapping function from <c>TSource</c> to <c>TKey</c>.</param>
        /// <returns>The smallest item in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>selector</c> is null.</exception>
        public static TSource MinBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Gets the smallest item of the sequence using the <c>comparer</c> with the <c>selector</c> function returned values.
        /// </summary>
        /// <typeparam name="TSource">Type the items.</typeparam>
        /// <typeparam name="TKey">Type of the returned value of <c>selector</c> function.</typeparam>
        /// <param name="source">Sequence to which the method applies.</param>
        /// <param name="selector">Mapping function from <c>TSource</c> to <c>TKey</c>.</param>
        /// <param name="comparer">Comparateur utilisé pour le type <c>TKey</c>.</param>
        /// <returns>The smallest item in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>source</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>selector</c> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>selector</c> is null.</exception>
        public static TSource MinBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> selector,
            IComparer<TKey> comparer)
        {
            Assert.IsNotNull(source, nameof(source));
            Assert.IsNotNull(selector, nameof(selector));
            Assert.IsNotNull(comparer, nameof(comparer));
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                    throw new InvalidOperationException("Séquence vide");

                var min = iterator.Current;
                var minKey = selector(min);
                while (iterator.MoveNext())
                {
                    var current = iterator.Current;
                    var currentKey = selector(current);
                    if (comparer.Compare(currentKey, minKey) < 0)
                    {
                        min = current;
                        minKey = currentKey;
                    }
                }
                return min;
            }
        }
    }
}
