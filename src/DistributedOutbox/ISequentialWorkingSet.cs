namespace DistributedOutbox
{
    /// <summary>
    /// Маркерный интерфейс рабочего набора, чьи сообщения следует обрабатывать последовательно
    /// </summary>
    public interface ISequentialWorkingSet : IWorkingSet
    {
    }
}