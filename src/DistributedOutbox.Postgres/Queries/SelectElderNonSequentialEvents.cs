namespace DistributedOutbox.Postgres.Queries
{
    public partial class SelectElderNonSequentialEventsQuery
    {
        private readonly string _schema;
        private readonly string _table;

        public SelectElderNonSequentialEventsQuery(string schema, string table)
        {
            _schema = schema;
            _table = table;
        }

        partial void ProcessCachedSql(ref string queryText)
        {
            queryText = string.Format(queryText, _schema, _table);
        }
    }
}