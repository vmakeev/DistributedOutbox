SELECT *
FROM "{0}"."{1}"
WHERE ("Status" = 'New' OR "Status" = 'Failed')
  AND "SequenceName" IS NULL
ORDER BY "Date" ASC LIMIT @limit
    FOR
UPDATE SKIP LOCKED