namespace DistributedOutbox.Postgres.Queries
{
    public partial class GetNextEventIdQuery
    {
        private readonly string _schema;

        public GetNextEventIdQuery(string schema)
        {
            _schema = schema;
        }

        partial void ProcessCachedSql(ref string queryText)
        {
            queryText = string.Format(queryText, _schema);
        }
    }
}