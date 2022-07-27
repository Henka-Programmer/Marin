import { Domain,Term } from "./domain";

const term1: Term = ['name', '=', 'amine'];
const term2: Term = ['age', '>', 25];
const term3: Term = ['birthday', '=', new Date(2022, 2, 1)];

const domain1: Domain = ['|', term1, term2, '&', term3];
const domain2: Domain = ['&', term1, term2, '&', ['city', '=', 'Malaga']];
const domain3: Domain = ['|', domain1, domain2];
