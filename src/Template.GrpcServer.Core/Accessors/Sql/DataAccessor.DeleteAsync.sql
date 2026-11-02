DELETE FROM
    Data
WHERE
    Id = /*@ id */0
/*% if (version != null) { */
    AND Version = /*@ version */1
/*% } */
