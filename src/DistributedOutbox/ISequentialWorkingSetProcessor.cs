namespace DistributedOutbox
{
    /// <summary>
    /// Маркерный интерфейс обработчика, выполняющего строго последовательную обработку сообщений
    /// </summary>
    public interface ISequentialWorkingSetProcessor : IWorkingSetProcessor
    {
    }
}