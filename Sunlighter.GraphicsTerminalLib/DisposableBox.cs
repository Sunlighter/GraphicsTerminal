using Sunlighter.OptionLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunlighter.GraphicsTerminalLib
{
    /// <summary>
    /// Stores a disposable item which can be replaced.
    /// </summary>
    public sealed class DisposableBox<T> : IDisposable
        where T : IDisposable
    {
        private Option<T> content;

        public DisposableBox()
        {
            content = Option<T>.None;
        }

        public bool IsEmpty { get { return !content.HasValue; } }

        /// <summary>
        /// Sets the content, disposing the previous content if there was any.
        /// </summary>
        /// <param name="newItem">The new content</param>
        public void Set(T newItem)
        {
            if (content.HasValue)
            {
                content.Value.Dispose();
            }
            content = Option<T>.Some(newItem);
        }

        /// <summary>
        /// Returns the content, without giving up ownership. Throws if there is no content.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if there is no content.</exception>
        public T Get()
        {
            if (content.HasValue)
            {
                return content.Value;
            }
            else
            {
                throw new InvalidOperationException("DisposableBox is empty");
            }
        }

        /// <summary>
        /// Guarantees box is empty. If there is content, disposes it.
        /// </summary>
        public void Clear()
        {
            if (content.HasValue)
            {
                content.Value.Dispose();
            }
            content = Option<T>.None;
        }

        /// <summary>
        /// Guarantees box is empty. If there is content, does not dispose it. Suitable for when content has been obtained and will be disposed by some other means.
        /// </summary>
        public void ClearNoDispose()
        {
            content = Option<T>.None;
        }

        /// <summary>
        /// Returns the content (without disposing it) and becomes empty.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if there is no content.</exception>
        public T Remove()
        {
            if (content.HasValue)
            {
                T result = content.Value;
                content = Option<T>.None;
                return result;
            }
            else
            {
                throw new InvalidOperationException("DisposableBox is empty");
            }
        }

        /// <summary>
        /// Sets the content, returning the previous content, which caller must now dispose.
        /// </summary>
        /// <param name="newItem">Thw new content.</param>
        /// <returns>The old content.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no content.</exception>
        public T Swap(T newItem)
        {
            if (content.HasValue)
            {
                T oldContent = content.Value;
                content = Option<T>.Some(newItem);
                return oldContent;
            }
            else
            {
                throw new InvalidOperationException("DisposableBox is empty");
            }
        }

        public void Dispose()
        {
            if (content.HasValue)
            {
                content.Value.Dispose();
            }
        }
    }

}
