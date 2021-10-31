// <auto-generated/>
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox.Postgres.Queries
{
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class GetNextEventIdQuery
    {
        private string _cachedSql;
        private readonly object _cachedSqlLocker = new object();

        /// <summary>
        /// Заготовка для определенной пользователем пост-обработки текста запроса
        /// </summary>
        /// <param name="queryText">Текст кэшированного sql-запроса</param>
        partial void ProcessCachedSql(ref string queryText);

        /// <summary>
        /// Возвращает текст запроса
        /// </summary>
        /// <returns>Текст запроса</returns>
        protected virtual string GetQueryText()
        {
            if (_cachedSql == null)
            {
                lock (_cachedSqlLocker)
                {
                    using (Stream stream = typeof(GetNextEventIdQuery).Assembly.GetManifestResourceStream("DistributedOutbox.Postgres.Queries.GetNextEventIdQuery.sql"))
                    {
                        string sql = new StreamReader(stream ?? throw new InvalidOperationException("Can not get manifest resource stream.")).ReadToEnd();

                        const string sectionRegexPattern = @"--\s*begin\s+[a-zA-Z0-9_]*\s*\r?\n.*?\s*\r?\n\s*--\s*end\s*\r?\n";
                        const RegexOptions regexOptions = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
                        sql = Regex.Replace(sql, sectionRegexPattern, string.Empty, regexOptions);

                        _cachedSql = sql;

                        ProcessCachedSql(ref _cachedSql);
                    }
                }
            }

            return _cachedSql;
        }

        /// <summary>
        /// Добавляет параметр к команде
        /// </summary>
        /// <param name="command">Команда SQL</param>
        /// <param name="parameterType">Тип параметра</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="value">Значение параметра</param>
        /// <param name="length">Длина параметра</param>
        protected virtual void AddParameter(IDbCommand command, NpgsqlDbType parameterType, string parameterName, object value, int? length = null)
        {
            var parameter = new NpgsqlParameter
            {
                ParameterName = parameterName,
                NpgsqlDbType = parameterType,
                Value = value ?? DBNull.Value
            };

            if (length.HasValue && length.Value > 0)
            {
                parameter.Size = length.Value;
            }
    
            command.Parameters.Add(parameter);
        }

        /// <summary>
        /// Возвращает значение из первого столбца <paramref name="record"/>
        /// </summary>
        /// <typeparam name="T">Тип значения</typeparam>
        /// <param name="record">Строка БД</param>
        /// <returns>Значение из первого столбца <paramref name="record"/></returns>
        protected virtual T GetScalarFromRecord<T>(IDataRecord record)
        {
            if (record.FieldCount < 1)
            {
                throw new Exception("Data record contain no values.");
            }

            object valueObject = record[0];

            return GetScalarFromValue<T>(valueObject);    
        }

        /// <summary>
        /// Конвертирует значение <paramref name="valueObject"/> в <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Тип значения</typeparam>
        /// <param name="valueObject">Строка БД</param>
        /// <returns>Значение <paramref name="valueObject"/>, сконвертированное в <typeparamref name="T"/></returns>
        protected virtual T GetScalarFromValue<T>(object valueObject)
        {
            switch (valueObject)
            {
                case null:
                // ReSharper disable once UnusedVariable
                case DBNull dbNull:
                    return default(T);

                case T value:
                    return value;

                case IConvertible convertible:
                    return (T)Convert.ChangeType(convertible, typeof(T));

                default:
                    // ReSharper disable once ConstantConditionalAccessQualifier
                    throw new InvalidCastException($"Can not convert {valueObject?.GetType().FullName ?? "null"} to {typeof(T).FullName}");
            }
        }

        /// <summary>
        /// Выполняет ленивую загрузку значений типа <see cref="long?"/>
        /// </summary>
        /// <param name="connection">Подключение к БД</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Значения типа  <see cref="long?"/></returns>
        public virtual async IAsyncEnumerable<long?> GetAsync(DbConnection connection, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = GetQueryText();

            PrepareCommand(cmd);

            using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                yield return GetScalarFromRecord<long?>(reader);
            }
        }

        /// <summary>
        /// Заготовка для определенной пользователем обработки команды перед исполнением
        /// </summary>
        /// <param name="command">Команда</param>
        partial void PrepareCommand(IDbCommand command);
    }
}