namespace DistributedOutbox.Postgres.Queries
{
    public partial class SelectElderEventsBySequenceNameQuery
    {
        private readonly string _schema;
        private readonly string _table;

        public SelectElderEventsBySequenceNameQuery(string schema, string table)
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