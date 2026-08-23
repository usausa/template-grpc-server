SELECT * FROM Data
WHERE (/*@ name */'' IS NULL) OR (Name LIKE '%' || /*@ name */'' || '%')
ORDER BY Id
