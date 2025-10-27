# Comprehensive Portuguese Language Grammar

This grammar provides extensive coverage of the Portuguese language, including unique features not found in other Romance languages.

## Grammar Size & Scope

- **619 lines** of comprehensive grammar rules
- **Production-ready** for European and Brazilian Portuguese
- **Unique Portuguese features**: Personal infinitive, future subjunctive
- **Full coverage of Portuguese verb system** including all tenses and moods

## Unique Portuguese Features

### 1. Personal Infinitive
Portuguese has a unique inflected infinitive that agrees with its subject:
- **Uninflected**: É importante estudar (It's important to study)
- **Personal**: É importante estudares (It's important for you to study)
- **Conjugation**: falar → falares, falarmos, falardes, falarem

### 2. Future Subjunctive  
Used in temporal and conditional clauses referring to future:
- **Quando**: Quando for ao Brasil... (When I go to Brazil...)
- **Se**: Se tiveres tempo... (If you have time...)
- **Unique to Portuguese**: No other Romance language has this tense

### 3. Ser vs Estar vs Ficar
Three copular verbs with distinct uses:
- **ser**: permanent/essential qualities (Sou português)
- **estar**: temporary states/location (Estou cansado, Estou em Lisboa)
- **ficar**: change of state/becoming (Fico feliz - I become happy)

## Core Features

### 1. Complete Verb System

#### Three Verb Conjugation Groups
- **-ar verbs**: falar, cantar, trabalhar
- **-er verbs**: comer, beber, correr
- **-ir verbs**: partir, vir, dormir

#### All Tenses
- **Presente**: falo, comes, parte
- **Pretérito perfeito**: falei, comeste, partiu
- **Pretérito imperfeito**: falava, comias, partia
- **Pretérito mais-que-perfeito** (simple): falara, comera, partira
- **Futuro**: falarei, comerás, partirá
- **Condicional**: falaria, comerias, partiria

#### All Subjunctive Moods
- **Present subjunctive**: fale, comas, parta
- **Imperfect subjunctive**: falasse, comesses, partisse
- **Future subjunctive**: falar, comeres, partir (unique to Portuguese)

#### Compound Tenses (with ter)
- **Pretérito perfeito composto**: tenho falado
- **Pretérito mais-que-perfeito composto**: tinha falado
- **Futuro perfeito**: terei falado

### 2. Clitic Pronoun Placement

Three positions for object pronouns:

#### Proclisis (before verb)
- After negation: Não o vejo
- After certain adverbs: Já te disse
- In subordinate clauses: Espero que te ajude

#### Enclisis (after verb)
- In affirmative statements: Vejo-o
- In imperative: Fala-me
- Starting a sentence: Disse-me que...

#### Mesoclisis (inside verb)
- Only in future and conditional: Ver-te-ei (I will see you)
- Dar-lhe-ia (I would give him/her)

### 3. Articles and Contractions

#### Definite Articles
- Masculine: o, os
- Feminine: a, as

#### Contractions (mandatory)
- **de + o/a/os/as** → do, da, dos, das
- **em + o/a/os/as** → no, na, nos, nas
- **a + o/a/os/as** → ao, à, aos, às
- **por + o/a/os/as** → pelo, pela, pelos, pelas

### 4. Gender and Number

#### Two Genders
- **Masculine**: o livro, um gato
- **Feminine**: a casa, uma mesa

#### Plural Formation
- Add -s: livro → livros
- Add -es: animal → animais
- Irregular: pão → pães

### 5. Pronouns

#### Subject Pronouns (often omitted)
- **eu** (I), **tu** (you informal), **você** (you formal), **ele/ela** (he/she)
- **nós** (we), **vós** (you pl. archaic), **vocês** (you pl.), **eles/elas** (they)
- **a gente** (we - colloquial)

#### Object Pronouns
- **Direct**: me, te, o/a, nos, vos, os/as
- **Indirect**: me, te, lhe, nos, vos, lhes
- **Reflexive**: me, te, se, nos, vos, se

### 6. Major Irregular Verbs (14+)

ser, estar, ter, haver, ir, fazer, dizer, vir, pôr, saber, querer, poder, ver, dar

### 7. Adjectives

#### Position
- Usually after noun: casa branca
- Some before: bom homem

#### Agreement  
Must agree in gender and number with noun

#### Absolute Superlative
Unique intensive form:
- bonito → bonitíssimo (very beautiful)
- grande → grandíssimo (very large)

### 8. Comparison

#### Comparative
- Superiority: mais alto (que)
- Inferiority: menos alto (que)
- Equality: tão alto (como)

#### Superlative
- Relative: o mais alto
- Absolute: altíssimo

#### Irregular
- bom → melhor → (o) melhor → ótimo
- mau → pior → (o) pior → péssimo

### 9. Gerund and Progressive

#### Gerund Formation
- -ar → -ando: falando
- -er → -endo: comendo
- -ir → -indo: partindo

#### Progressive Tenses
- **estar a + infinitive**: Estou a trabalhar (European)
- **estar + gerund**: Estou trabalhando (Brazilian)

### 10. Prepositions

#### Simple Prepositions
a, ante, após, até, com, contra, de, desde, em, entre, para, perante, por, sem, sob, sobre, trás

#### Complex Prepositions
através de, acerca de, além de, ao lado de, em vez de, apesar de, antes de, depois de, dentro de, fora de

## Technical Implementation

- **Format**: CEBNF (Context-Enhanced Backus-Naur Form)
- **Token splitting**: Space-based for natural language
- **Context attributes**: @GENDER, @NUMBER, @CASE, @TENSE, @MOOD, @PERSON, @IRREGULAR, @REFLEXIVE
- **Symbol table**: Hierarchical sharing
- **Error levels**: ERROR, WARNING

## Use Cases

1. **Natural Language Processing**: Parse Portuguese text
2. **Grammar Checking**: Validate Portuguese writing
3. **Language Learning**: Systematic Portuguese grammar teaching
4. **Translation Systems**: MT source/target language
5. **Linguistic Research**: Portuguese language analysis
6. **Text Generation**: Generate grammatically correct Portuguese

## References

Based on standard Portuguese grammar:
- European Portuguese (Portugal)
- Brazilian Portuguese variants
- Modern Portuguese orthographic conventions
- Acordo Ortográfico da Língua Portuguesa

## License

This grammar file is part of the Minotaur project and is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
