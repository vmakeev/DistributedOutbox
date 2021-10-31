SELECT DISTINCT "SequenceName"
FROM "{0}"."{1}"
WHERE ("Status" = 'New' OR "Status" = 'Failed')
  AND "SequenceName" IS NOT NULL LIMIT @limit