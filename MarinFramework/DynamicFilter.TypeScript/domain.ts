import { EnumType } from "typescript";

export type TermOperator = '=' | '>' | '<' | '!=' | '<=' | '>=' | 'in' | 'not in' | 'like';
export type DomainOperator = '&' | '|' | '!';

export type Term = [left: string, operator: TermOperator, right: string | number | Date | EnumType | undefined | boolean];
export type ScalarDomain = [...Array<DomainOperator | Term>];

export interface Domain extends Array<ScalarDomain | Domain | DomainOperator | Term >
{
}

export function parse(domain: Domain)
{
    console.log('parsed');
}

export function toJson(domain: Domain): string
{
    return JSON.stringify(domain);
}