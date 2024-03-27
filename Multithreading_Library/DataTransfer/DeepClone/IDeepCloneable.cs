namespace Multithreading_Library.DataTransfer.DeepClone
{
    /// <summary>
    /// implements the Function DeepClone which returns a cloned Object
    /// </summary>
    /// <typeparam name="T">the type to be cloneable</typeparam>
    public interface IDeepCloneable<T>
    {
        /// <summary>
        /// implements the Function DeepClone which returns a cloned Object
        /// </summary>
        /// <returns></returns>
        T DeepClone();
    }
}
