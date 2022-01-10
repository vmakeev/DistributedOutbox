SELECT *
FROM "{0}"."{1}"
WHERE ("Status" = 'New' OR "Status" = 'Failed')
  AND "SequenceName" = @sequenceName
ORDER BY "Date" ASC, "Id" ASC LIMIT @limit
    FOR
UPDATE NOWAIT