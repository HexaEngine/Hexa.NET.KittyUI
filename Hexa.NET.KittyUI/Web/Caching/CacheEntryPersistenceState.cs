namespace Hexa.NET.KittyUI.Web.Caching
{
    [Flags]
    public enum CacheEntryPersistenceStateFlags
    {
        None = 0,
        Dirty = 1,
        Ghost = 2,
    }

    /// <summary>
    /// Represents the persistence state of a cache entry.
    /// </summary>
    public struct CacheEntryPersistenceState
    {
        public CacheEntryPersistenceStateFlags Flags;

        /// <summary>
        /// Indicates whether the cache entry has been modified and needs to be written back to disk.
        /// </summary>
        public bool IsDirty
        {
            readonly get => (Flags & CacheEntryPersistenceStateFlags.Dirty) != 0; set
            {
                if (value)
                {
                    Flags |= CacheEntryPersistenceStateFlags.Dirty;
                }
                else
                {
                    Flags &= ~CacheEntryPersistenceStateFlags.Dirty;
                }
            }
        }

        /// <summary>
        /// Indicates whether the cache entry is a ghost entry which means it's not longer in memory nor on disk. Very spooky.
        /// </summary>
        public bool IsGhost
        {
            readonly get => (Flags & CacheEntryPersistenceStateFlags.Ghost) != 0; set
            {
                if (value)
                {
                    Flags |= CacheEntryPersistenceStateFlags.Ghost;
                }
                else
                {
                    Flags &= ~CacheEntryPersistenceStateFlags.Ghost;
                }
            }
        }

        /// <summary>
        /// The previous size of the cache entry.
        /// </summary>
        public uint OldSize;
    }
}