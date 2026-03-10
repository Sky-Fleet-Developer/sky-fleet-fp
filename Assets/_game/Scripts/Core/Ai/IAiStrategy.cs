using Core.World;

namespace Core.Ai
{
    public interface IAiStrategy
    {
        /// <summary>
        /// Allows strategy to control entity. Don't need to remove entity on disposing. It will be removed automatically.
        /// </summary>
        public void AddControllableEntity(ItemEntity entity);
        
        /// <summary>
        /// Don't need to remove entity on death. It will be removed automatically.
        /// </summary>
        public void RemoveControllableEntity(ItemEntity entity);
    }
}