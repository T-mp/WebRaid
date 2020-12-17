using System;
using System.Threading.Tasks;

namespace WebRaid.Abstraction.Speicher
{
    /// <summary>
    /// Ein Cache für <typeparamref name="TO" /> mit dem Schlüssel <typeparamref name="TI"/>.
    /// </summary>
    public interface ICache<TI, TO>
    {
        /// <summary>
        /// Holt das mit <paramref name="key" /> bezeichnete Objekt.
        /// Entweder aus oder per <see cref="Resolve"/>
        /// </summary>
        /// <param name="key">Der Name des Objekts</param>
        Task<TO> Get(TI key);
        /// <summary>
        /// Funktion um <typeparamref name="TO" /> zu erhalten
        /// </summary>
        Func<TI, Task<TO>> Resolve { set; }
    }
}