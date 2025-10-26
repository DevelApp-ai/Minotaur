# Danish Language Grammar

This directory contains example Danish sentences that demonstrate the Danish.grammar file's capabilities.

## Grammar Features

The Danish grammar includes:

### 1. Regular Verb Conjugations
Regular Danish verbs follow predictable patterns:
- **Present tense**: Add `-r` or `-er` to the stem
- **Past tense**: Add `-ede`, `-de`, or `-te` to the stem
- **Perfect tense**: Use `har`/`er` + past participle (stem + `-et` or `-t`)

### 2. Irregular Verb Conjugations
Common irregular verbs with stem changes and unpredictable forms:
- **være** (to be): er, var, været
- **have** (to have): har, havde, haft
- **blive** (to become): bliver, blev, blevet
- **gå** (to go): går, gik, gået
- **komme** (to come): kommer, kom, kommet
- **se** (to see): ser, så, set
- **give** (to give): giver, gav, givet
- **tage** (to take): tager, tog, taget
- **finde** (to find): finder, fandt, fundet

### 3. Context-Sensitive Rules
The grammar includes context-sensitive validation for:
- **Gender agreement**: Articles and adjectives must agree with noun gender (common/neuter)
- **Number agreement**: Subjects and verbs should agree in number
- **Definiteness agreement**: Adjectives change form with definite nouns
- **Tense compatibility**: Auxiliary verbs must be compatible with main verbs

### 4. Regular Expression Patterns
Full regex support for:
- **Noun stems**: `/[a-zæøå]+/` (including Danish characters æ, ø, å)
- **Verb stems**: Pattern-based matching for conjugation
- **Proper nouns**: `/[A-ZÆØÅ][a-zæøå]*/` (capitalized words)
- **Whitespace**: `/[ \t\n\r]+/` for flexible spacing

### 5. Gender System
Danish has two genders:
- **Common gender** (en-words): Uses "en" as indefinite article
- **Neuter gender** (et-words): Uses "et" as indefinite article

### 6. Definiteness
Definite forms are created by suffixing the article:
- **Indefinite**: en hund (a dog), et hus (a house)
- **Definite**: hunden (the dog), huset (the house)

## Example Sentences

### Regular Verbs

**Present Tense:**
```
Jeg arbejder i København.
(I work in Copenhagen)

Hun studerer dansk.
(She studies Danish)

De spiser morgenmad.
(They eat breakfast)
```

**Past Tense:**
```
Han arbejdede i går.
(He worked yesterday)

Vi studerede hele natten.
(We studied all night)

I spillede fodbold.
(You played football)
```

**Perfect Tense:**
```
Vi har arbejdet hele dagen.
(We have worked all day)

Hun har studeret dansk i to år.
(She has studied Danish for two years)

De har spillet godt.
(They have played well)
```

### Irregular Verbs

**Være (to be):**
```
Present:  Hun er lærer.
          (She is a teacher)

Past:     De var hjemme.
          (They were home)

Perfect:  Jeg har været i Paris.
          (I have been in Paris)
```

**Have (to have):**
```
Present:  Jeg har en hund.
          (I have a dog)

Past:     Han havde en bil.
          (He had a car)

Perfect:  Vi har haft problemer.
          (We have had problems)
```

**Gå (to go):**
```
Present:  Han går til skole.
          (He goes to school)

Past:     Hun gik hjem.
          (She went home)

Perfect:  Vi har gået meget.
          (We have walked a lot)
```

**Komme (to come):**
```
Present:  De kommer i morgen.
          (They come tomorrow)

Past:     Jeg kom for sent.
          (I came too late)

Perfect:  Han har kommet hjem.
          (He has come home)
```

**Se (to see):**
```
Present:  Jeg ser filmen.
          (I see/watch the film)

Past:     Vi så en film.
          (We saw a film)

Perfect:  Hun har set det før.
          (She has seen it before)
```

### Context-Sensitive Gender Agreement

**Common Gender (en-words):**
```
en stor hund
(a big dog)

den store hund
(the big dog)

en rød bil
(a red car)

den røde bil
(the red car)
```

**Neuter Gender (et-words):**
```
et stort hus
(a big house)

det store hus
(the big house)

et rødt æble
(a red apple)

det røde æble
(the red apple)
```

### Complex Sentences

**With Modal Verbs:**
```
Jeg kan tale dansk.
(I can speak Danish)

Hun vil læse bogen.
(She wants to read the book)

Vi skal arbejde i morgen.
(We must work tomorrow)

De kunne ikke komme.
(They could not come)
```

**With Subordinate Clauses:**
```
Jeg ved at hun kommer.
(I know that she is coming)

Han siger at han arbejder.
(He says that he works)

Hvis du kommer, bliver vi glade.
(If you come, we will be happy)
```

**Compound Sentences:**
```
Jeg arbejder og hun studerer.
(I work and she studies)

Han går hjem men hun bliver.
(He goes home but she stays)

Vi spiser nu eller vi spiser senere.
(We eat now or we eat later)
```

### Interrogative Sentences

```
Hvem er du?
(Who are you?)

Hvad laver du?
(What are you doing?)

Hvor bor du?
(Where do you live?)

Hvorfor kommer du ikke?
(Why don't you come?)

Kan du tale dansk?
(Can you speak Danish?)
```

### Imperative Sentences

```
Kom her!
(Come here!)

Læs bogen!
(Read the book!)

Vær stille!
(Be quiet!)

Hav en god dag!
(Have a good day!)
```

## Grammar Validation Features

The grammar includes automatic validation for:

1. **Article-Noun Gender Agreement**: "en hus" ❌ → "et hus" ✓
2. **Adjective-Noun Agreement**: "et stor hus" ❌ → "et stort hus" ✓
3. **Definite Form Agreement**: "den stor hund" ❌ → "den store hund" ✓
4. **Tense Compatibility**: Ensures auxiliary and main verbs work together

## Technical Implementation

The grammar uses:
- **CEBNF** (Context-Enhanced Backus-Naur Form) format
- **Regular expressions** for flexible pattern matching of Danish morphology
- **Context attributes** (@GENDER, @NUMBER, @TENSE, @DEFINITENESS) for agreement checking
- **Symbol table sharing** for cross-reference validation
- **Hierarchical context** for nested clause analysis

## Use Cases

This grammar can be used for:
- Natural language processing of Danish text
- Language learning applications
- Grammar checking tools
- Translation systems
- Linguistic analysis
- Text generation with grammatical correctness
- Parsing Danish sentences into structured representations
