using Blasphemous.Framework.UI;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Framework.Pooling;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Components;

/// <summary>
/// Reuses UI objects in pools scoped to a canvas, consuming mod, and prefab.
/// </summary>
public sealed class UIObjectPoolManager : MonoBehaviour
{
    private readonly Dictionary<BlasMod, Dictionary<GameObject, Queue<ObjectInstance>>> _poolDictionary =
        new(new ReferenceComparer<BlasMod>());

    /// <summary>
    /// Gets the pool manager attached to the standard pixel-perfect UI canvas.
    /// </summary>
    public static UIObjectPoolManager? PixelPerfect => GetOrAddCanvasManager(UIModder.Parents.CanvasStandard, nameof(PixelPerfect));

    /// <summary>
    /// Gets the pool manager attached to the high-resolution UI canvas.
    /// </summary>
    public static UIObjectPoolManager? HighRes => GetOrAddCanvasManager(UIModder.Parents.CanvasHighRes, nameof(HighRes));

    /// <summary>
    /// Reuses an object owned by the mod whose assembly called this method.
    /// </summary>
    /// <param name="prefab">The prefab whose pool should be used.</param>
    /// <param name="position">The world position for the reused object.</param>
    /// <param name="rotation">The world rotation for the reused object.</param>
    /// <param name="createPoolIfNeeded">Whether to create a missing pool.</param>
    /// <param name="poolSize">The number of objects to create when creating a pool.</param>
    /// <returns>The reused object wrapper, or <see langword="null"/> when no pool exists or the owner cannot be resolved.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ObjectInstance? ReuseObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        bool createPoolIfNeeded = false,
        int poolSize = 1)
    {
        return ReuseObject(
            prefab,
            position,
            rotation,
            modType: null,
            callingAssembly: Assembly.GetCallingAssembly(),
            createPoolIfNeeded,
            poolSize);
    }

    /// <summary>
    /// Reuses an object owned by the specified loaded mod.
    /// </summary>
    /// <param name="prefab">The prefab whose pool should be used.</param>
    /// <param name="position">The world position for the reused object.</param>
    /// <param name="rotation">The world rotation for the reused object.</param>
    /// <param name="owner">The loaded mod that owns the pool.</param>
    /// <param name="createPoolIfNeeded">Whether to create a missing pool.</param>
    /// <param name="poolSize">The number of objects to create when creating a pool.</param>
    /// <returns>The reused object wrapper, or <see langword="null"/> when no pool exists or the owner cannot be resolved.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ObjectInstance? ReuseObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        BlasMod owner,
        bool createPoolIfNeeded = false,
        int poolSize = 1)
    {
        if (owner == null)
        {
            ModLog.Error("Cannot reuse a UI pool object because the explicit owner is null.");
            return null;
        }

        if (!ModFlagsManager.TryResolveMod(owner, out BlasMod? resolvedOwner) || resolvedOwner == null)
            return null;

        return ReuseObjectForMod(prefab, position, rotation, resolvedOwner, createPoolIfNeeded, poolSize);
    }

    private static UIObjectPoolManager? GetOrAddCanvasManager(Transform? canvas, string canvasName)
    {
        if (canvas == null)
        {
            ModLog.Error($"Cannot access the {canvasName} UI object pool because its canvas is unavailable.");
            return null;
        }

        return canvas.gameObject.GetOrElseAddComponent<UIObjectPoolManager>();
    }

    private ObjectInstance? ReuseObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Type? modType,
        Assembly callingAssembly,
        bool createPoolIfNeeded,
        int poolSize)
    {
        if (!ModFlagsManager.TryResolveMod(modType, callingAssembly, out BlasMod? owner) || owner == null)
            return null;

        return ReuseObjectForMod(prefab, position, rotation, owner, createPoolIfNeeded, poolSize);
    }

    private ObjectInstance? ReuseObjectForMod(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        BlasMod owner,
        bool createPoolIfNeeded,
        int poolSize)
    {
        if (prefab == null)
        {
            ModLog.Error("Cannot reuse a UI pool object because the prefab is null.");
            return null;
        }

        if (owner == null)
        {
            ModLog.Error("Cannot reuse a UI pool object because the owner mod is null.");
            return null;
        }

        if (!_poolDictionary.TryGetValue(owner, out Dictionary<GameObject, Queue<ObjectInstance>>? prefabPools))
        {
            if (!createPoolIfNeeded)
                return null;

            prefabPools = new Dictionary<GameObject, Queue<ObjectInstance>>(new ReferenceComparer<GameObject>());
            _poolDictionary.Add(owner, prefabPools);
        }

        if (!prefabPools.TryGetValue(prefab, out Queue<ObjectInstance>? pool))
        {
            if (!createPoolIfNeeded)
                return null;

            if (poolSize <= 0)
            {
                ModLog.Error($"Cannot create a UI object pool with a non-positive pool size ({poolSize}).");
                return null;
            }

            pool = CreatePool(prefab, owner, poolSize);
            prefabPools.Add(prefab, pool);
        }

        if (pool.Count == 0)
            return null;

        ObjectInstance instance = pool.Dequeue();
        pool.Enqueue(instance);
        instance.Reuse(position, rotation);
        return instance;
    }

    private Queue<ObjectInstance> CreatePool(GameObject prefab, BlasMod owner, int poolSize)
    {
        Queue<ObjectInstance> pool = new(poolSize);
        GameObject poolRoot = new(prefab.name + " pool (" + owner.GetType().Name + ")");
        poolRoot.transform.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            ObjectInstance instance = new(UnityEngine.Object.Instantiate<GameObject>(prefab));
            pool.Enqueue(instance);
            instance.SetParent(poolRoot.transform);
        }

        return pool;
    }

    /// <summary>
    /// Wraps a pooled object and applies the standard activation and reuse callback behavior.
    /// </summary>
    public sealed class ObjectInstance
    {
        private readonly Transform _transform;
        private readonly PoolObject[] _poolObjectScripts;

        /// <summary>
        /// Creates a wrapper for a pooled game object.
        /// </summary>
        /// <param name="gameObject">The object represented by this wrapper.</param>
        /// <exception cref="ArgumentNullException"><paramref name="gameObject"/> is <see langword="null"/>.</exception>
        public ObjectInstance(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            GameObject = gameObject;
            _transform = gameObject.transform;
            gameObject.SetActive(false);
            _poolObjectScripts = gameObject.GetComponent<PoolObject>() == null
                ? new PoolObject[0]
                : gameObject.GetComponents<PoolObject>();
        }

        /// <summary>
        /// Gets the game object represented by this wrapper.
        /// </summary>
        public GameObject GameObject { get; }

        /// <summary>
        /// Activates the object, updates its world transform, and notifies its pool components.
        /// </summary>
        /// <param name="position">The world position for the object.</param>
        /// <param name="rotation">The world rotation for the object.</param>
        public void Reuse(Vector3 position, Quaternion rotation)
        {
            if (GameObject == null)
                return;

            _transform.position = position;
            _transform.rotation = rotation;
            GameObject.SetActive(true);

            foreach (PoolObject poolObject in _poolObjectScripts)
                poolObject.OnObjectReuse();
        }

        /// <summary>
        /// Sets the object's parent transform.
        /// </summary>
        /// <param name="parent">The new parent transform.</param>
        public void SetParent(Transform parent)
        {
            _transform.SetParent(parent);
        }
    }

    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        /// <summary>
        /// Compares two values by object identity.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><see langword="true"/> when both references identify the same object.</returns>
        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        /// <summary>
        /// Gets a hash code based on object identity.
        /// </summary>
        /// <param name="obj">The value to hash.</param>
        /// <returns>The runtime identity hash code for <paramref name="obj"/>.</returns>
        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
