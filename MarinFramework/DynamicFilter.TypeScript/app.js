"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const domain_1 = require("./domain");
const term1 = ['name', '=', 'amine'];
const term2 = ['age', '>', 25];
const term3 = ['birthday', '=', new Date(2022, 2, 1, 5, 5, 5, 5)];
const domain1 = ['&', '|', term1, term2, term3];
const domain2 = ['&', '&', term1, term2, ['city', '=', 'Malaga']];
const domain3 = ['|', domain1, domain2];
console.log((0, domain_1.toJson)(domain3));
//# sourceMappingURL=app.js.map