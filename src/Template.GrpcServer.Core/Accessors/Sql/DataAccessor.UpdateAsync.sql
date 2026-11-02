UPDATE
    Data
SET
/*% if (name != null) { */
    Name = /*@ name */'',
/*% } */
/*% if (value != null) { */
    Value = /*@ value */0,
/*% } */
/*% if (status != null) { */
    Status = /*@ status */1,
/*% } */
/*% if (labels != null) { */
    Labels = /*@ labels */'{}',
/*% } */
    Version = Version + 1
WHERE
    Id = /*@ id */0
/*% if (version != null) { */
    AND Version = /*@ version */1
/*% } */
RETURNING
    *
