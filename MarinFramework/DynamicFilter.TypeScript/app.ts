import { Domain,toJson,Term } from "./domain";

const term1: Term = ['name', '=', 'amine'];
const term2: Term = ['age', '>', 25];
const term3: Term = ['birthday', '=', new Date(2022, 2, 1, 5,5,5,5)];

const domain1: Domain = ['&', '|', term1, term2, term3];
const domain2: Domain = ['&', '&', term1, term2, ['city', '=', 'Malaga']];
const domain3: Domain = ['|', domain1, domain2];


/*
 Partner
 ID
 Name
 Age,
 Balance,
 Address,
 IsAdmin
 */


// get all parners where ID (from 1 to 100) and (from 200 to 400) and (Age > 25) and (balance > 10000)
const partnersFilter: Domain = [['ID', '>=', 1],
                                ['ID', '<=', 100],
                                ['ID', '>=', 200],
    ['ID', '<=', 400],
    ['Age', '>', 25],
                                ['balance', '>', 10000]];

console.log(toJson(partnersFilter));