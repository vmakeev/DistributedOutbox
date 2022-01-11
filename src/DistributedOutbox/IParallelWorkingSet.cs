namespace DistributedOutbox
{
    /// <summary>
    /// Маркерный интерфейс рабочего набора, чьи сообщения следуео обрабатывать параллельно
    /// </summary>
    public interface IParallelWorkingSet : IWorkingSet
    {
    }
}