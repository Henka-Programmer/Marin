"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.normalize = exports.toJson = exports.parse = void 0;
const TERM_OPERATORS = ['=', '>', '<', '!=', '<=', '>=', 'in', 'not in', 'like'];
const DOMAIN_OPERATORS = ['&', '|', '!'];
function parse(domain) {
    throw new Error('Not Implemented!');
}
exports.parse = parse;
function toJson(element) {
    if (isTerm(element)) {
        return JSON.stringify(normalizeTerm(element));
    }
    return JSON.stringify(normalize(element));
}
exports.toJson = toJson;
function normalize(domain) {
    const result = [];
    for (const element of domain) {
        if (typeof element === 'string' && isDomainOperator(element)) {
            result.push(normalizeOperator(element));
            continue;
        }
        if (isTerm(element)) {
            result.push(normalizeTerm(element));
            continue;
        }
        if (isDomain(element)) {
            result.push(normalize(element));
            continue;
        }
        throw new Error('invalid domain!');
    }
    return result;
}
exports.normalize = normalize;
function isTerm(object) {
    const term = object;
    const array = object;
    return term != null && (array != null && array.length == 3 && isTermOperator(term[1]));
}
function isTermOperator(operator) {
    return typeof operator === 'string' && TERM_OPERATORS.includes(operator);
}
function isDomainOperator(operator) {
    return typeof operator === 'string' && DOMAIN_OPERATORS.includes(operator);
}
function isDomain(object) {
    const domain = object;
    return domain != null && Array.isArray(object) && domain.every((element) => isDomainOperator(element) || isTerm(element) || isDomain(element));
}
function normalizeTerm(term) {
    const normalizedTerm = {
        type: 'Term',
        left: term[0],
        operator: term[1],
        right: {
            type: 'termRight',
            valueType: typeof term[1],
            value: term[2].toString()
        }
    };
    return normalizedTerm;
}
function normalizeOperator(operator) {
    return { type: 'operator', value: operator };
}
//# sourceMappingURL=domain.js.map