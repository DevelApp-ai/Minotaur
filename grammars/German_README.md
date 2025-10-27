# Comprehensive German Language Grammar (Hochdeutsch)

This grammar provides extensive coverage of Standard German (Hochdeutsch) as spoken and written in Germany.

## Grammar Size & Scope

- **600+ lines** of comprehensive grammar rules
- Production-ready for advanced natural language processing applications
- Complete case system with four cases (nominative, accusative, dative, genitive)
- Three grammatical genders (masculine, feminine, neuter)

## Core Features

### 1. Complete Verb System

#### Regular Verb Conjugations
- **Present tense**: stem + personal endings (e, st, t, en, t)
- **Past tense**: stem + -te/-test/-te/-ten/-tet
- **Perfect tense**: haben/sein + past participle
- **Pluperfect tense**: hatte/war + past participle
- **Future tense**: werden + infinitive

#### Irregular Verb Conjugations (30+ verbs)
Including: sein, haben, werden, gehen, kommen, nehmen, geben, sehen, essen, fahren, lesen, schreiben, sprechen, trinken, finden, denken, bringen, wissen, tun, lassen, halten, stehen, verstehen, beginnen, gewinnen, singen, tragen, schlafen, rufen

#### Voice
- **Active voice**: All standard tenses and moods
- **Passive voice**: werden + past participle (Das Buch wird gelesen)

#### Modal Verbs
können, müssen, dürfen, sollen, wollen, mögen/möchten

### 2. Four-Case System

German distinguishes four grammatical cases:

- **Nominative**: Subject case (Der Mann ist hier)
- **Accusative**: Direct object case (Ich sehe den Mann)
- **Dative**: Indirect object case (Ich gebe dem Mann das Buch)
- **Genitive**: Possessive case (Das Haus des Mannes)

### 3. Three-Gender System

- **Masculine** (der-words): der Mann, ein Mann
- **Feminine** (die-words): die Frau, eine Frau
- **Neuter** (das-words): das Kind, ein Kind

### 4. Adjective Declension

German adjectives have three declension patterns:

#### Strong Declension
Used when no article precedes the adjective:
- guter Wein (good wine)
- schönes Wetter (beautiful weather)

#### Weak Declension
Used after definite articles:
- der gute Wein (the good wine)
- das schöne Wetter (the beautiful weather)

#### Mixed Declension
Used after indefinite articles and possessives:
- ein guter Wein (a good wine)
- mein schönes Haus (my beautiful house)

### 5. V2 Word Order (Verb-Second)

German main clauses follow strict V2 word order:
- **Position 1** (topic field): Any constituent
- **Position 2**: Finite verb (always)
- **Middle field**: Subject, objects, adverbs
- **Verb complement**: Separable prefixes, infinitives, participles
- **Final field**: Subordinate clauses

Examples:
```
Heute arbeite ich.           (Today work I)
Das Buch lese ich.           (The book read I)
In Berlin wohne ich.         (In Berlin live I)
```

### 6. Verb Bracket (Satzklammer)

The finite verb and non-finite verb parts form a bracket around the middle field:
- Ich **habe** gestern ein Buch **gelesen**. (I have yesterday a book read)
- Er **wird** morgen nach Berlin **fahren**. (He will tomorrow to Berlin go)

### 7. Subordinate Clauses

In subordinate clauses, the finite verb moves to the end:
- Ich weiß, dass er **kommt**. (I know that he comes)
- Wenn es **regnet**, bleiben wir zu Hause. (When it rains, stay we at home)

### 8. Separable Prefix Verbs

Many German verbs have separable prefixes that split off in main clauses:
- **aufstehen** → Ich **stehe** um 7 Uhr **auf**. (I get up at 7 o'clock)
- **ankommen** → Der Zug **kommt** um 10 Uhr **an**. (The train arrives at 10 o'clock)

Common separable prefixes: ab-, an-, auf-, aus-, bei-, ein-, fest-, her-, hin-, los-, mit-, nach-, vor-, weg-, zu-, zurück-, zusammen-

### 9. Compound Nouns

German is famous for its productive compound noun system:
- **Haus** + **tür** = **Haustür** (house door = front door)
- **Schule** + **buch** = **Schulbuch** (school book = textbook)
- **Donau** + **dampf** + **schiff** + **fahrt** + **gesellschaft** + **kapitän** = **Donaudampfschifffahrtsgesellschaftskapitän** (Danube steamship company captain)

### 10. Reflexive Verbs

Many German verbs are reflexive:
- **sich waschen** (to wash oneself): Ich wasche **mich**.
- **sich freuen** (to be happy): Er freut **sich**.
- **sich erinnern** (to remember): Wir erinnern **uns**.

### 11. Comparison

#### Comparative
- Regular: adjective + -er (schnell → schneller)
- Irregular: gut → besser, groß → größer, hoch → höher

#### Superlative
- Regular: adjective + -st(e) or am + adjective + -sten
- schnell → schnellst(e) / am schnellsten
- Irregular: gut → best(e), groß → größt(e), hoch → höchst(e)

### 12. Negation

- **nicht**: General negation (Ich komme nicht)
- **kein**: Negation of nouns (Ich habe kein Geld)
- **nie/niemals**: Never
- **niemand**: Nobody
- **nichts**: Nothing

### 13. Prepositions with Cases

German prepositions govern specific cases:

#### Accusative Prepositions
durch, für, gegen, ohne, um

#### Dative Prepositions
aus, bei, mit, nach, seit, von, zu

#### Genitive Prepositions
während, wegen, trotz, statt

#### Two-Way Prepositions (Accusative or Dative)
an, auf, hinter, in, neben, über, unter, vor, zwischen
- Accusative when indicating direction (motion)
- Dative when indicating location (position)

### 14. Word Order Variations

#### Topicalization
Any constituent can be fronted to topic position:
```
Das Buch lese ich.          (The book, I read)
Morgen komme ich.           (Tomorrow, I come)
Meinem Bruder gebe ich das Buch. (To my brother, I give the book)
```

#### Inversion
Verb-subject inversion after fronted constituent:
```
Jetzt kommt er.             (Now comes he)
Deshalb arbeite ich.        (Therefore work I)
```

## Technical Implementation

- **Format**: CEBNF (Context-Enhanced Backus-Naur Form)
- **Token splitting**: Space-based for natural language
- **Context attributes**:
  - @GENDER[masculine|feminine|neuter]
  - @NUMBER[singular|plural]
  - @CASE[nominative|accusative|dative|genitive]
  - @TENSE[present|past|perfect|pluperfect|future]
  - @MOOD[modal]
  - @VOICE[passive]
  - @DEGREE[positive|comparative|superlative]
  - @IRREGULAR (marker for irregular forms)
  - @REFLEXIVE (marker for reflexive pronouns)
  - @DECLENSION[strong|weak|mixed]

- **Symbol table**: Hierarchical sharing for cross-reference validation
- **Error levels**: ERROR (hard violations), WARNING (stylistic issues)

## Use Cases

This comprehensive grammar enables:

1. **Natural Language Processing**: Parse and analyze German text
2. **Grammar Checking**: Validate German writing for correctness
3. **Language Learning**: Teach German grammar systematically
4. **Translation Systems**: Source/target language for MT systems
5. **Linguistic Research**: Analyze German language patterns
6. **Text Generation**: Generate grammatically correct German
7. **Syntax Highlighting**: Advanced editors for German text
8. **Speech Recognition**: Constraint-based language modeling

## Coverage Comparison

| Feature | Basic Grammar | Comprehensive Grammar |
|---------|---------------|----------------------|
| Cases | 1-2 | 4 (all) |
| Genders | 2 | 3 (all) |
| Verb tenses | 2-3 | 5 |
| Adjective declensions | 1 | 3 (strong/weak/mixed) |
| Modal verbs | 3-4 | 6 (all) |
| Irregular verbs | 10 | 30+ |
| Separable prefixes | 5 | 15+ |
| Prepositions | 10 | 25+ |
| Context rules | 2-3 | 7 |

## Example Sentences

See `German_Examples.txt` for 200+ validated sentences demonstrating:
- All verb forms and tenses
- Case and gender agreement
- V2 word order variations
- Passive voice constructions
- Relative and subordinate clauses
- Comparative and superlative forms
- Reflexive constructions
- Compound nouns
- Separable prefix verbs
- Modal verb usage
- All four cases in context

## References

Based on standard German grammar references:
- Duden - Die Grammatik
- Hammer's German Grammar and Usage
- Deutsche Grammatik (Helbig/Buscha)
- Modern German orthographic conventions (neue Rechtschreibung)

## License

This grammar file is part of the Minotaur project and is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
