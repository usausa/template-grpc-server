SELECT
    *
FROM
    Data
WHERE
    1 = 1
/*% if (name != null) { */
    AND Name LIKE /*@ name */'' ESCAPE '\'
/*% } */
/*% if (createdFrom != null) { */
    AND CreatedAt >= /*@ createdFrom */'2000-01-01'
/*% } */
/*% if (createdTo != null) { */
    AND CreatedAt < /*@ createdTo */'2000-01-01'
/*% } */
ORDER BY
/*% if (desc) { */
    /*# sort */Id DESC, Id DESC
/*% } else { */
    /*# sort */Id, Id
/*% } */
