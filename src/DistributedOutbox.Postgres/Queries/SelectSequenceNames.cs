namespace DistributedOutbox.Postgres.Queries
{
    public partial class SelectSequenceNamesQuery
    {
        private readonly string _schema;
        private readonly string _table;

        public SelectSequenceNamesQuery(string schema, string table)
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