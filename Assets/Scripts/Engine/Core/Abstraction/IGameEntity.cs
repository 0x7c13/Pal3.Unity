// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    using System;

    /// <summary>
    /// Represents a game entity that can be added to the game world.
    /// </summary>
    public interface IGameEntity : IManagedObject
    {
        /// <summary>
        /// Gets or sets the name of the game entity.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the game entity is static.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets the layer of the game entity.
        /// </summary>
        public int Layer { get; set; }

        /// <summary>
        /// Gets the transform of the game entity.
        /// </summary>
        public ITransform Transform { get; }

        /// <summary>
        /// Sets the parent of the game entity.
        /// </summary>
        /// <param name="parent">The parent game entity.</param>
        /// <param name="worldPositionStays">Whether to keep the world position of the game entity.</param>
        public void SetParent(IGameEntity parent, bool worldPositionStays);

        /// <summary>
        /// Adds a component of the specified type to the game entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to add.</typeparam>
        /// <returns>The added component.</returns>
        public T AddComponent<T>() where T : class;

        /// <summary>
        /// Adds a component of the specified type to the game entity.
        /// </summary>
        /// <param name="type">The type of the component to add.</param>
        /// <returns>The added component.</returns>
        public object AddComponent(Type type);

        /// <summary>
        /// Gets the component of the specified type from the game entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <returns>The component of the specified type.</returns>
        public T GetComponent<T>() where T : class;

        /// <summary>
        /// Gets the component of the specified type from the children of the game entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <returns>The component of the specified type.</returns>
        public T GetComponentInChildren<T>() where T : class;

        /// <summary>
        /// Gets all the components of the specified type from the children of the game entity.
        /// </summary>
        /// <typeparam name="T">The type of the components to get.</typeparam>
        /// <returns>All the components of the specified type.</returns>
        public T[] GetComponentsInChildren<T>() where T : class;

        /// <summary>
        /// Gets the component of the specified type from the game entity, or adds it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of the component to get or add.</typeparam>
        /// <returns>The component of the specified type.</returns>
        public T GetOrAddComponent<T>() where T : class;

        /// <summary>
        /// Finds the child game entity with the specified name.
        /// </summary>
        /// <param name="name">The name of the child game entity.</param>
        /// <returns>The child game entity with the specified name.</returns>
        public IGameEntity FindChild(string name);

        /// <summary>
        /// Destroys the game entity.
        /// </summary>
        public void Destroy();
    }
}