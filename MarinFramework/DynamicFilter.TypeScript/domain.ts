const TERM_OPERATORS = ['=', '>', '<', '!=', '<=', '>=', 'in', 'not in', 'like'];
const DOMAIN_OPERATORS = ['&', '|', '!'];

export type TermOperator = '=' | '>' | '<' | '!=' | '<=' | '>=' | 'in' | 'not in' | 'like';
export type DomainOperator = '&' | '|' | '!';
export type Operator = TermOperator & DomainOperator;

type TermElementTypes = string | number | Date | undefined | boolean;
export interface Term extends Array<TermElementTypes> { 0: string; 1: TermOperator; 2: string | number | Date | undefined | boolean; }
export interface Domain extends Array<DomainOperator | Domain | Term> { }


interface IFilterElement
{
    type: 'operator' | 'term' | 'value' | 'domain';
    value: IFilterTerm | Operator | IFilterElement | IFilterTermValue | Array<IFilter>;
}

interface IFilterTerm
{
    left: string;
    operator: TermOperator;
    right: IFilterTermValue
}

interface IFilterTermValue
{
    type: string;
    value: TermElementTypes;
}

interface IFilter {
    type: string;
    value: Array<IFilter | IFilterElement >
}

interface INormalization {
    type: string;
    value: string;
}

interface IValueNormalization extends INormalization {
    valueType: string;
}

interface ITermNormalization {
    type: string;
    left: string;
    operator: TermOperator;
    right: IValueNormalization
}

export function parse(domain: Domain): void {
    throw new Error('Not Implemented!');
}

export function toJson(element: Domain): string {
    return JSON.stringify(toFilter(element));
}

export function toFilter(domain: Domain): IFilter
{
    const result: IFilter =
        {
            type: 'domain',
            value: []
        } as IFilter;

    for (const element of domain) {
        if (typeof element === 'string' && isDomainOperator(element)) {
            result.value.push({ type: 'operator', value: element as Operator } as IFilterElement);
            continue;
        }

        if (isTerm(element)) {
            result.value.push({
                type: 'term',
                value: {
                    left: element[0],
                    operator: element[1],
                    right: {
                        type: element[2] instanceof Date ? "date" : typeof element[1],
                        value: element[2] instanceof Date ? element[2].toISOString() : element[2]
                    } as IFilterTermValue
                } as IFilterTerm
            } as IFilterElement);
            continue;
        }

        if (isDomain(element)) {
            result.value.push(toFilter(element));
            continue;
        }

        throw new Error('invalid domain!');
    }

    return result;
}

const isoDateRegex = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*))(?:Z|(\+|-)([\d|:]*))?$/;

function isIsoDate(value) {
    return isoDateRegex.exec(value);
}

export function normalize(domain: Domain): Object[] {
    const result: Object[] = [];
    for (const element of domain) {
        if (typeof element === 'string' && isDomainOperator(element)) {
            result.push(normalizeOperator(element));
            continue;
        }

        if (isTerm(element)) {
            result.push(normalizeTerm(element as Term));
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

function isTerm(object: unknown): object is Term {
    const term: Term | null = object as Term;
    const array: Array<TermElementTypes> | null = object as Array<TermElementTypes>;
    return term != null && (array != null && array.length == 3 && isTermOperator(term[1]));
}

function isTermOperator(operator: unknown) {
    return typeof operator === 'string' && TERM_OPERATORS.includes(operator);
}

function isDomainOperator(operator: unknown) {
    return typeof operator === 'string' && DOMAIN_OPERATORS.includes(operator);
}

function isDomain(object: unknown): object is Domain {
    const domain: Domain | null = object as Domain;
    return domain != null && Array.isArray(object) && domain.every((element: any) => isDomainOperator(element) || isTerm(element) || isDomain(element));
}

function normalizeTerm(term: Term): ITermNormalization {
    const normalizedTerm: ITermNormalization =
    {
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

function normalizeOperator(operator: DomainOperator | TermOperator): INormalization
{
    return { type: 'operator', value: operator };
}

