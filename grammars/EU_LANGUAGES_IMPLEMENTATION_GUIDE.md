# EU Languages Implementation Guide

## Overview

This document provides a comprehensive guide for implementing the remaining 13 EU official languages that are not yet covered at the Danish grammar level (1,393 lines).

## Status Summary

### ✅ Completed Languages (10/24 - Base Implementation)

| Language | Lines | Family | Status |
|----------|-------|--------|--------|
| Danish | 1,393 | Germanic | ✅ Complete with 6 dialects (100% - Reference) |
| Modern Greek | 1,490 | Hellenic | ✅ Complete (107% of Danish) |
| German | 1,367 | Germanic | ✅ Complete (98% of Danish) |
| Finnish | 1,293 | Uralic | ✅ Complete (93% of Danish) |
| Italian | 913 | Romance | ✅ Complete (66% of Danish) |
| French | 911 | Romance | ✅ Complete (65% of Danish) |
| **Spanish** | **1,009** | Romance | ✅ **Complete (72% of Danish)** |
| **Portuguese** | **813** | Romance | ✅ **Complete (58% of Danish)** |
| **Dutch** | **519** | Germanic | ✅ **Complete (37% of Danish)** |
| Swedish | 269+ | Germanic | ✅ Complete (19% of Danish) |

### 🚀 Enhanced to ~50% of Danish Level (3/23)

| Language | Lines | % of Danish | Context Rules | Family | Status |
|----------|-------|-------------|---------------|--------|--------|
| **Romanian** | **716** | **51%** | 22 | Romance | ✅ Enhanced with comprehensive features |
| **Polish** | **692** | **50%** | 18 | Slavic (West) | ✅ Enhanced with comprehensive features |
| **Czech** | **691** | **50%** | 17 | Slavic (West) | ✅ Enhanced with comprehensive features |

**Common features in enhanced languages:**
- Comprehensive pronoun systems (demonstrative, interrogative, indefinite, reciprocal with full case declension)
- Full numeral systems (cardinals 1-1,000,000, ordinals, fractions, distributives)
- Complete quantifier systems (15+ quantifiers: indefinite, universal, negative)
- Extensive temporal expressions (50-60 forms: time points, durations, frequencies, times of day)
- Extensive spatial expressions (30-40 forms: locations, directions, distances)
- Comparative constructions (3-4 types)
- Enhanced sentence structures (parenthetical, fronted subordinate, coordinated complex, extraposed, embedded sequences)
- 33+ irregular verbs matching Danish standard

### 📈 Partially Enhanced Languages (5/23)

| Language | Lines | % of Danish | Context Rules | Status |
|----------|-------|-------------|---------------|--------|
| Hungarian | 438 | 31% | 13 | Enhanced Phase 1 *(needs Phase 2 to 50%)* |
| Bulgarian | 385 | 28% | 13 | Enhanced Phase 1 *(needs Phase 2 to 50%)* |
| Slovak | 234 | 17% | 12 | Enhanced Phase 1 *(needs Phase 2 to 50%)* |
| Croatian | 233 | 17% | 12 | Enhanced Phase 1 *(needs Phase 2 to 50%)* |
| Estonian | 216 | 15% | 10 | Enhanced Phase 1 *(needs Phase 2 to 50%)* |

### 📋 Foundational Languages (3/23)

| Language | Lines | % of Danish | Status |
|----------|-------|-------------|--------|
| Maltese | 25 | 2% | Foundational implementation |
| Irish (Gaelic) | 24 | 2% | Foundational implementation |
| Lithuanian | 20 | 1% | Foundational implementation |
| Latvian | 20 | 1% | Foundational implementation |
| Slovene | - | 0% | Not yet implemented |

**Note**: English is also an official EU language (Ireland, Malta) but is already extensively documented globally and does not require a custom grammar for this project. Total EU official languages: 24. After excluding English: 23 languages, with 10 complete and 13 remaining.

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
**Status**: 10 of 23 implementation-required EU languages complete (43%), 13 remaining  
**Note**: English is the 24th official EU language but is excluded from this implementation as it is already extensively documented worldwide
