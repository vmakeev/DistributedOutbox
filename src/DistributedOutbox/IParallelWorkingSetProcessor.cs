namespace DistributedOutbox
{
    /// <summary>
    /// Маркерный интерфейс обработчика, выполняющего параллельную обработку сообщений
    /// </summary>
    public interface IParallelWorkingSetProcessor : IWorkingSetProcessor
    {
    }
}