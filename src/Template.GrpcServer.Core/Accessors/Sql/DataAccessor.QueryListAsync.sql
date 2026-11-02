SELECT
    *
FROM
    Data
WHERE
    1 = 1
/*% if (name != null) { */
    AND Name LIKE /*@ name */'' ESCAPE '\'
/*% } */
ORDER BY
/*% if (desc) { */
    /*# sort */Id DESC
/*% } else { */
    /*# sort */Id
/*% } */
