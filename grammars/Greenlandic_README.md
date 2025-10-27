# Greenlandic Language Grammar (Kalaallisut)

Comprehensive grammar for Greenlandic with support for both West Greenlandic (Kalaallisut) and East Greenlandic (Tunumiisut) dialects.

## Grammar Size & Scope

- **456 lines** of comprehensive grammar rules
- **Two major dialects**: West Greenlandic and East Greenlandic
- **Production-ready** for processing one of the world's most morphologically complex languages
- **Polysynthetic structure** with extensive noun incorporation and derivational morphology

## Language Features

Greenlandic (Kalaallisut) is an Eskimo-Aleut language spoken by approximately 57,000 people in Greenland. It is the sole official language of Greenland and one of the most polysynthetic languages in the world.

### 1. Polysynthetic Morphology

Greenlandic words can contain an entire sentence's worth of information through extensive affixation:

**Example**: 
```
Aliikusersuillammassuaanerartassagaluarpaalli
(However, they will say that he is a great entertainer, but...)

Breaking down:
aliikuser-    (fun, entertainment)
-siu-         (provide, make)
-lla-         (really)
-mmas-        (verb to adjective)
-sua-         (big, great)
-aner-        (say)
-ar-          (to do repeatedly)
-ta-          (habitual)
-ssa-         (future)
-ga-          (but)
-luarpa-      (although)
-alli         (3rd person plural)
```

### 2. Ergative-Absolutive Alignment

Unlike nominative-accusative languages (like English), Greenlandic uses ergative-absolutive alignment:

- **Absolutive case**: Used for intransitive subjects and transitive objects
- **Ergative case**: Used for transitive subjects

```
Qimmeq amerlerpoq.
dog(ABS) sees.INTR
"The dog sees."

Angut qimmip amerlerpaa.
man(ABS) dog(ERG) sees.TR
"The man sees the dog."
```

### 3. Eight Grammatical Cases

1. **Absolutive** (unmarked): Subject of intransitive, object of transitive
2. **Ergative** (-p, -up): Subject of transitive verbs
3. **Instrumental** (-mik, -umik): "with, by means of"
4. **Locative** (-mi, -umi): "in, at"
5. **Ablative** (-mit, -umit): "from"
6. **Allative** (-mut, -umut): "to, towards"
7. **Vialis** (-kkut, -ukkut): "through, via"
8. **Equative** (-tut, -utut): "like, as"

### 4. Verb Moods

Greenlandic has six verb moods, each with person/number markers:

#### Indicative Mood (Statements)
```
-punga (1sg), -putit (2sg), -poq (3sg)
-pput (1pl), -pusi (2pl), -pput (3pl)

Neripunga. (I eat)
Neripoq. (He/she eats)
```

#### Interrogative Mood (Questions)
```
-va (1sg), -vit (2sg), -va (3sg)
-vat (1pl), -visi (2pl), -vat (3pl)

Neriva? (Do I eat?)
Nerivit? (Do you eat?)
```

#### Imperative Mood (Commands)
```
-git (2sg), -gitse (2pl)

Nerigit! (Eat! - singular)
Nerigitse! (Eat! - plural)
```

#### Optative Mood (Wishes)
```
-langa (1sg), -llagit (2sg), -llaaq (3sg)

Nerilanga. (I wish to eat / Let me eat)
```

#### Conditional Mood
```
-ppat, -ppassi

Nerippat... (If I eat...)
```

#### Participial Mood
```
-soq, -suq (active participle)

Nerisoq (one who eats, eating person)
```

### 5. Noun Incorporation

Greenlandic productively incorporates nouns into verbs:

```
Qimmer-ner-punga
dog-have-1sg.IND
"I have a dog" (literally: "I dog-have")

Ilinniartut-qar-punga
student-have-1sg.IND
"I am a student" (literally: "I student-am")
```

### 6. Derivational Suffixes

Extensive derivational morphology creates new meanings:

```
Neri- (eat)
+ -sima- (perfective): Nerisimavunga (I have eaten)
+ -ssa- (future): Nerissavunga (I will eat)
+ -nngit- (negative): Nerinngippunga (I don't eat)
+ -galaar- (go to): Nerigalaarpoq (He goes to eat)
+ -suaq- (augmentative): Nerisuaappunga (I eat a lot)
+ -laar- (begin): Nerilaarpoq (He begins to eat)
```

### 7. Dialectal Differences: West vs. East

The grammar supports both major Greenlandic dialects:

#### Phonological Differences

**West /v/ → East /w/**:
- West: *vit* (we) → East: *wit*
- West: *uanga* (I) → East: *wanga*

**West /s/ → East /h/** (in some contexts):
- West: *-sima-* (perfective) → East: *-hima-*
- West: *sapinngilaq* (no problem) → East: *hapinngilaq*

**West /g/ → East /k/** (in some contexts):
- West: *-galaar-* (go to) → East: *-kalâr-*
- West: *-gunar-* (when) → East: *-kunar-*

#### Morphological Differences

**Interrogative mood markers**:
- West: *-va*, *-vit*, *-va*
- East: *-wa*, *-wit*, *-wa*

**Past tense marker**:
- West: *-sima-*
- East: *-hima-*

**Imperative plural**:
- West: *-gitse*
- East: *-kitce*

### 8. Number System

Greenlandic has **three numbers**:

1. **Singular**: qimmeq (a dog)
2. **Dual**: qimmek (two dogs)
3. **Plural**: qimmit (dogs - three or more)

The dual is fully productive and used for paired body parts and natural pairs.

### 9. SOV Word Order

Basic word order is Subject-Object-Verb:

```
Uanga qimmeq amerappara.
I(ABS) dog(ABS) see.TR.1sg>3sg
"I see the dog."
```

## Example Sentences

### West Greenlandic (Kalaallisut)

**Basic Sentences**:
```
Uanga Nuummoorpunga.
(I am from Nuuk)

Illit ilinniartorpit.
(You are a student)

Una angerlarpok.
(He/she goes home)
```

**Case Examples**:
```
Illu pissuaq. (Absolutive)
(A big house)

Illup tungaa. (Ergative/Genitive)
(The house's back)

Illumut aavoq. (Allative)
(He goes to the house)

Illumit anivoq. (Ablative)
(He comes from the house)

Illumi najugaqarpoq. (Locative)
(He lives in the house)
```

**Polysynthetic Verbs**:
```
Nerisarapugut.
(We will eat - future)

Nerisimavunga.
(I have eaten - perfective past)

Nerisuaappunga.
(I eat a lot - augmentative)

Nerinngippunga.
(I don't eat - negative)
```

**Questions**:
```
Suna taava?
(What is that?)

Kina pitit?
(Who are you?)

Nerippita?
(Did you eat? - interrogative mood)
```

### East Greenlandic (Tunumiisut)

**Dialectal Variations**:
```
West: Nerisimavunga → East: Nerihimawunga
(I have eaten)

West: Nerivit? → East: Neriwit?
(Did you eat?)

West: Ippassaq → East: Ippahaq
(Yesterday)

West: Uanga → East: Wanga
(I)

West: Ilissi → East: Ilihi
(You plural)
```

## Linguistic Notes

### One of the World's Most Complex Languages

Greenlandic is frequently cited as one of the most morphologically complex languages:
- Single words can express entire sentences
- Theoretical possibility of infinite word length through compounding
- Over 400 inflectional forms for a single verb
- Productive noun incorporation
- Complex derivational system

### Language Vitality

Greenlandic is unique among indigenous languages in that:
- It's the sole official language of Greenland (since 2009)
- Used in all domains: education, government, media
- High intergenerational transmission
- Active language planning and standardization

### Orthographic Reform

The current Latin-based orthography was introduced in 1973, replacing an earlier system. The modern orthography aims to be phonemic while respecting morphological boundaries.

### Dialect Continuum

Beyond West and East, there are several other dialects:
- **Avanersuaq** (Thule/Qaanaaq): Northern Greenland
- **Kitaamiusut**: Central West Greenland (basis for standard)
- **Tunumiisut**: East Greenland (covered in this grammar)
- **Kalaallisut**: West Greenland (covered in this grammar)

## References

Based on Greenlandic linguistic resources:
- Oqaasileriffik (Greenlandic Language Secretariat)
- Greenlandic orthography (1973 reform)
- West Greenlandic grammar (Kalaallisut oqaaseqarfik)
- East Greenlandic dialect documentation (Tunumiisut)
- Comparative Eskimo-Aleut linguistics

## License

This grammar file is part of the Minotaur project and is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
