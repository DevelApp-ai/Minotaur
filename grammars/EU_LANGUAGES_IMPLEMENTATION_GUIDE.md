# EU Languages Implementation Guide

## Overview

This document provides a comprehensive guide for implementing the remaining 13 EU official languages that are not yet covered at the Danish grammar level (1,393 lines).

## Status Summary

### ✅ Completed Languages (10/24)

| Language | Lines | Family | Status |
|----------|-------|--------|--------|
| Danish | 1,393 | Germanic | ✅ Complete with 6 dialects |
| Modern Greek | 1,490 | Hellenic | ✅ Complete |
| German | 1,367 | Germanic | ✅ Complete |
| Finnish | 1,293 | Uralic | ✅ Complete |
| French | 911 | Romance | ✅ Complete |
| Italian | 913 | Romance | ✅ Complete |
| **Spanish** | **736** | Romance | ✅ **NEW - Complete** |
| **Portuguese** | **619** | Romance | ✅ **NEW - Complete** |
| **Dutch** | **348** | Germanic | ✅ **NEW - Complete** |
| Swedish | 269+ | Germanic | ✅ Complete |

### 📋 Remaining Languages (14/24)

| Language | Country | Family | Unique Features | Est. Lines |
|----------|---------|--------|-----------------|------------|
| Polish | Poland | Slavic (West) | 7 cases, aspect pairs, mobile stress | 900-1,200 |
| Romanian | Romania | Romance | Case system (5 cases), definite article suffixes | 800-1,000 |
| Czech | Czechia | Slavic (West) | 7 cases, consonant clusters, vowel length | 900-1,200 |
| Hungarian | Hungary | Uralic (Finno-Ugric) | 18+ cases, vowel harmony, agglutination | 1,000-1,300 |
| Bulgarian | Bulgaria | Slavic (South) | No cases, definite articles, verb aspects | 700-900 |
| Slovak | Slovakia | Slavic (West) | 6 cases, rhythmic law, aspect | 900-1,100 |
| Croatian | Croatia | Slavic (South) | 7 cases, three genders, aspect | 900-1,100 |
| Slovene | Slovenia | Slavic (South) | 6 cases, dual number, pitch accent | 900-1,100 |
| Lithuanian | Lithuania | Baltic | 7 cases, pitch accent, three genders | 900-1,100 |
| Latvian | Latvia | Baltic | 7 cases, three genders, diminutives | 800-1,000 |
| Estonian | Estonia | Uralic (Finnic) | 14 cases, no grammatical gender, gradation | 1,000-1,200 |
| Irish (Gaelic) | Ireland | Celtic | VSO word order, initial mutations, two genders | 800-1,000 |
| Maltese | Malta | Semitic | Triconsonantal roots, VSO, Arabic + Romance | 800-1,000 |
| English | Ireland, Malta | Germanic | Already widely documented | N/A |

**Note**: English is already extensively documented and does not require a custom grammar for this project.

## Implementation Pattern

Based on the successfully implemented Spanish, Portuguese, and Dutch grammars, each language should follow this structure:

### 1. Grammar File Structure (`.grammar`)

```
Grammar: [LanguageName]
TokenSplitter: Space
FormatType: CEBNF
ContextSensitive: true
SymbolTableSharing: hierarchical

/* Header comment with feature list */

Keywords: [language-specific keywords]

/* Top-level sentence structure */
<sentence> ::= ...

/* Core grammatical categories */
<noun-phrase> ::= ...
<verb-phrase> ::= ...
<adjective-phrase> ::= ...
<adverbial> ::= ...

/* Language-specific features */
[cases, aspects, moods, etc.]

/* Context-sensitive rules */
@CONTEXT_RULE[rule-name]:
    IF condition
    THEN action
    ELSE alternative
```

### 2. Required Components

Each grammar must include:

#### A. Noun System
- Gender (if applicable)
- Number (singular, plural, dual if applicable)
- Case system (nominative, accusative, genitive, etc.)
- Definiteness
- Diminutives (if applicable)

#### B. Verb System
- All tenses (present, past, future, perfect, pluperfect)
- All moods (indicative, subjunctive, conditional, imperative)
- Aspect (perfective/imperfective for Slavic languages)
- Voice (active, passive, middle if applicable)
- Regular conjugation patterns
- Irregular verbs (minimum 15-20 most common)

#### C. Pronoun System
- Personal pronouns (all cases)
- Possessive pronouns
- Demonstrative pronouns
- Relative pronouns
- Interrogative pronouns
- Indefinite pronouns
- Reflexive pronouns

#### D. Adjective System
- Agreement rules (gender, number, case)
- Comparison (positive, comparative, superlative)
- Position rules

#### E. Syntax
- Word order (SVO, SOV, VSO, V2, etc.)
- Clause structure (main, subordinate, relative)
- Question formation
- Negation patterns
- Complex sentences with nesting (2-3 levels minimum)

#### F. Context Rules
Minimum 8-10 validation rules covering:
- Agreement (gender, number, case)
- Word order constraints
- Case selection
- Aspect/tense concordance
- Language-specific constraints

### 3. Documentation Files

#### README.md
- Grammar size and scope
- Unique language features
- Core features by category
- Technical implementation details
- Use cases
- References

#### Examples.txt
- Minimum 300+ example sentences
- Cover all verb tenses
- Demonstrate all grammar rules
- Show irregular forms
- Include complex sentences
- Demonstrate unique features

## Language-Specific Implementation Notes

### Slavic Languages (Polish, Czech, Slovak, Croatian, Slovene, Bulgarian)

**Common Features:**
- Case systems (except Bulgarian)
- Aspect pairs (perfective/imperfective)
- Three genders
- Complex consonant clusters

**Polish-Specific:**
- 7 cases: nominative, genitive, dative, accusative, instrumental, locative, vocative
- Nasal vowels: ą, ę
- Palatalization
- Mobile stress

**Czech-Specific:**
- Vowel length distinction (á, é, í, ó, ú, ů, ý)
- Soft/hard consonant distinction
- Complex declension patterns

**Bulgarian-Specific:**
- No case system (analytical)
- Definite articles (postfixed)
- Complex verb system with renarrative mood

### Romance Languages (Romanian)

**Romanian-Specific:**
- Only Romance language with case system (5 cases)
- Definite article suffixes (-ul, -a, -le)
- Three genders (masculine, feminine, neuter)
- Retention of Latin features

### Uralic Languages (Hungarian, Estonian)

**Hungarian-Specific:**
- 18+ grammatical cases
- Vowel harmony (front/back vowels)
- Agglutinative morphology
- Definite and indefinite conjugations
- No grammatical gender

**Estonian-Specific:**
- 14 cases
- Consonant gradation
- Three-way length distinction
- Possessive suffixes
- No grammatical gender

### Baltic Languages (Lithuanian, Latvian)

**Common Features:**
- Seven cases
- Three genders
- Pitch accent system
- Dual number (Lithuanian)

### Celtic Languages (Irish)

**Irish-Specific:**
- VSO word order
- Initial consonant mutations (lenition, eclipsis)
- Two genders
- Conjugated prepositions
- Two copulas (is vs tá)

### Semitic Languages (Maltese)

**Maltese-Specific:**
- Triconsonantal root system
- VSO word order
- Arabic vocabulary + Romance grammar
- Two genders
- Broken plurals

## Priority Order for Implementation

Based on speaker population and economic importance:

1. **Polish** (38M speakers) - Largest Slavic language in EU
2. **Romanian** (24M speakers) - Romance with unique features
3. **Hungarian** (13M speakers) - Complex Uralic language
4. **Czech** (10.7M speakers) - West Slavic
5. **Bulgarian** (7M speakers) - South Slavic, no cases
6. **Slovak** (5.4M speakers) - West Slavic
7. **Croatian** (5.6M speakers) - South Slavic
8. **Lithuanian** (2.8M speakers) - Baltic
9. **Slovene** (2.5M speakers) - South Slavic, dual number
10. **Latvian** (1.75M speakers) - Baltic
11. **Estonian** (1.1M speakers) - Uralic
12. **Irish** (170K native speakers) - Celtic
13. **Maltese** (520K speakers) - Semitic

## Testing and Validation

Each grammar implementation should include:

### 1. Unit Tests
- Parse 300+ example sentences successfully
- Validate all verb conjugations
- Test all case forms
- Verify agreement rules
- Test complex sentences with nesting

### 2. Context Rule Validation
- Verify all @CONTEXT_RULE declarations
- Test agreement enforcement
- Validate word order constraints
- Check case selection rules

### 3. Edge Cases
- Irregular forms
- Exceptions to rules
- Archaic forms
- Colloquial vs formal
- Regional variations (where applicable)

## Quality Standards

To match Danish grammar level (1,393 lines), each grammar should:

- **Size**: Minimum 700 lines, target 900-1,200 lines
- **Verb coverage**: All major tenses, moods, and aspects
- **Irregular verbs**: Minimum 15-20 most common
- **Context rules**: Minimum 8-10 validation rules
- **Examples**: Minimum 300 sentences
- **Documentation**: Complete README with unique features
- **Nesting**: Support 2-3 levels of clause embedding

## Resources for Implementation

### Linguistic References
- Each language's official grammar authority
- Academic grammars (e.g., Routledge Descriptive Grammars series)
- Native speaker consultation
- Corpus analysis (European Parliament proceedings in all languages)

### Technical Resources
- CEBNF format specification
- Existing grammar files as templates
- StepParser documentation
- Context-sensitive rule system documentation

## Timeline Estimate

Per language (assuming full-time development):
- **Simple languages** (Bulgarian, Romanian): 3-4 days
- **Moderate complexity** (Polish, Czech, Slovak, Croatian, Slovene, Lithuanian, Latvian, Irish, Maltese): 5-7 days
- **High complexity** (Hungarian, Estonian): 7-10 days

**Total estimate for all 13 languages**: ~12-15 weeks full-time work

## Maintenance and Updates

Each grammar should be:
- Version controlled
- Documented with changelog
- Tested with example corpus
- Updated for language reforms
- Reviewed by native speakers

## Contributing

When implementing a new language:

1. Create grammar file following template
2. Add comprehensive README
3. Create example sentences (300+)
4. Test all grammar rules
5. Validate context rules
6. Document unique features
7. Submit for native speaker review
8. Add to main README

## References

- **Ethnologue**: Language family classifications
- **WALS (World Atlas of Language Structures)**: Typological features
- **Europarl Corpus**: Multilingual parallel corpus
- **National Language Institutes**: Official grammar authorities
- **Academic Grammars**: Comprehensive linguistic descriptions

## License

All grammar files are part of the Minotaur project and licensed under AGPL-3.0.

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-27  
**Status**: 10/24 EU languages complete, 13 remaining (English excluded as well-documented)
