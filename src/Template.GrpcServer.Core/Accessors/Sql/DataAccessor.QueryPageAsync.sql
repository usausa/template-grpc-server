SELECT
    *
FROM
    Data
WHERE
    1 = 1
/*% if (name != null) { */
    AND Name LIKE /*@ name */'' ESCAPE '\'
/*% } */
/*% if (status != null) { */
    AND Status = /*@ status */1
/*% } */
/*% if (createdFrom != null) { */
    AND CreatedAt >= /*@ createdFrom */'2000-01-01'
/*% } */
/*% if (createdTo != null) { */
    AND CreatedAt < /*@ createdTo */'2000-01-01'
/*% } */
/*% if (afterId != null) { */
/*% if (desc) { */
    AND (/*# sort */Id, Id) < (/*@ afterKey */0, /*@ afterId */0)
/*% } else { */
    AND (/*# sort */Id, Id) > (/*@ afterKey */0, /*@ afterId */0)
/*% } */
/*% } */
ORDER BY
/*% if (desc) { */
    /*# sort */Id DESC, Id DESC
/*% } else { */
    /*# sort */Id, Id
/*% } */
LIMIT /*@ limit */20
