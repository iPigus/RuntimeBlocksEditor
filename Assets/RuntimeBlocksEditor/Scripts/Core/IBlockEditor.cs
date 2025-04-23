using UnityEngine;

namespace RuntimeBlocksEditor.Core
{
    /// <summary>
    /// Interface for block editing functionality
    /// </summary>
    public interface IBlockEditor
    {
        /// <summary>
        /// Creates a block at the specified position
        /// </summary>
        /// <param name="position">World position where the block should be created</param>
        /// <returns>True if block was created successfully, false otherwise</returns>
        bool CreateBlock(Vector3Int position);

        /// <summary>
        /// Removes a block at the specified position
        /// </summary>
        /// <param name="position">World position of the block to remove</param>
        /// <returns>True if block was removed successfully, false otherwise</returns>
        bool RemoveBlock(Vector3Int position);

        /// <summary>
        /// Checks if a block exists at the specified position
        /// </summary>
        /// <param name="position">World position to check</param>
        /// <returns>True if block exists at position, false otherwise</returns>
        bool HasBlock(Vector3Int position);

        /// <summary>
        /// Gets the block at the specified position
        /// </summary>
        /// <param name="position">World position of the block</param>
        /// <returns>The block GameObject if it exists, null otherwise</returns>
        GameObject GetBlock(Vector3Int position);

        /// <summary>
        /// Enables or disables edit mode
        /// </summary>
        /// <param name="enabled">True to enable edit mode, false to disable</param>
        void SetEditMode(bool enabled);

        /// <summary>
        /// Gets the current edit mode state
        /// </summary>
        /// <returns>True if edit mode is enabled, false otherwise</returns>
        bool IsEditModeEnabled();
    }
} 