# Postal Codes Grammars for Minotaur

This directory contains comprehensive grammar files for validating postal codes from around the world. These grammars are designed for use with the Minotaur parsing system and can be used for address validation, parsing, and generation.

## 📁 Directory Structure

```
PostalCodes/
├── International_Postal_Codes.grammar    # Master comprehensive grammar
├── PostalCode_Index.grammar              # Index of all postal code grammars
├── README.md                              # This file
├── North_America/
│   ├── US_Postal_Code.grammar
│   ├── Canada_Postal_Code.grammar
│   └── Mexico_Postal_Code.grammar
├── Europe/
│   ├── UK_Postal_Code.grammar
│   ├── Germany_Postal_Code.grammar
│   ├── France_Postal_Code.grammar
│   ├── Italy_Postal_Code.grammar
│   └── ...
├── Asia/
│   ├── Japan_Postal_Code.grammar
│   ├── China_Postal_Code.grammar
│   └── ...
├── Oceania/
│   ├── Australia_Postal_Code.grammar
│   └── New_Zealand_Postal_Code.grammar
├── Africa/
│   ├── South_Africa_Postal_Code.grammar
│   └── ...
└── South_America/
    ├── Brazil_Postal_Code.grammar
    └── ...
```

## 🌍 Currently Available Grammars

### International
- **International_Postal_Codes.grammar** - Comprehensive grammar covering 50+ countries
- **PostalCode_Index.grammar** - Master index for all postal code grammars

### North America
- **US_Postal_Code.grammar** - United States (ZIP codes: 5-digit, ZIP+4, military)
- **Canada_Postal_Code.grammar** - Canada (A1A 1A1 format)
- **Mexico_Postal_Code.grammar** - Mexico (5-digit codes)

### Europe
- **UK_Postal_Code.grammar** - United Kingdom (A9 9AA, A99 9AA, etc.)
- **Germany_Postal_Code.grammar** - Germany (5-digit PLZ)
- **France_Postal_Code.grammar** - France (5-digit code postal)
- **Italy_Postal_Code.grammar** - Italy (5-digit CAP)
- **Spain_Postal_Code.grammar** - Spain (5-digit codigo postal)
- **Netherlands_Postal_Code.grammar** - Netherlands (1234 AB format)
- **Belgium_Postal_Code.grammar** - Belgium (4-digit)
- **Switzerland_Postal_Code.grammar** - Switzerland (4-digit PLZ)
- **Austria_Postal_Code.grammar** - Austria (4-digit PLZ)
- **Sweden_Postal_Code.grammar** - Sweden (123 45 format)
- **Norway_Postal_Code.grammar** - Norway (4-digit)
- **Finland_Postal_Code.grammar** - Finland (5-digit)
- **Denmark_Postal_Code.grammar** - Denmark (4-digit)
- **Poland_Postal_Code.grammar** - Poland (12-345 format)
- **Portugal_Postal_Code.grammar** - Portugal (1234-567 format)
- **Ireland_Postal_Code.grammar** - Ireland (Eircode: A99 AA99)
- **Russia_Postal_Code.grammar** - Russia (6-digit)

### Asia
- **Japan_Postal_Code.grammar** - Japan (123-4567 format)
- **China_Postal_Code.grammar** - China (6-digit)
- **India_Postal_Code.grammar** - India (6-digit PIN code)
- **South_Korea_Postal_Code.grammar** - South Korea (5-6 digit)
- **Singapore_Postal_Code.grammar** - Singapore (6-digit)
- **Hong_Kong_Postal_Code.grammar** - Hong Kong (optional)
- **Thailand_Postal_Code.grammar** - Thailand (5-digit)
- **Malaysia_Postal_Code.grammar** - Malaysia (5-digit)
- **Indonesia_Postal_Code.grammar** - Indonesia (5-digit)
- **Philippines_Postal_Code.grammar** - Philippines (4-digit)
- **Vietnam_Postal_Code.grammar** - Vietnam (6-digit)
- **Taiwan_Postal_Code.grammar** - Taiwan (3+2 or 5-digit)
- **Israel_Postal_Code.grammar** - Israel (5-7 digit)
- **Saudi_Arabia_Postal_Code.grammar** - Saudi Arabia (5-digit)
- **UAE_Postal_Code.grammar** - United Arab Emirates

### Oceania
- **Australia_Postal_Code.grammar** - Australia (4-digit)
- **New_Zealand_Postal_Code.grammar** - New Zealand (4-digit)
- **Pacific_Islands_Postal_Code.grammar** - Pacific Islands

### Africa
- **South_Africa_Postal_Code.grammar** - South Africa (4-digit)
- **Egypt_Postal_Code.grammar** - Egypt (5-digit)
- **Nigeria_Postal_Code.grammar** - Nigeria (6-digit)
- **Kenya_Postal_Code.grammar** - Kenya (5-digit)
- **Morocco_Postal_Code.grammar** - Morocco (5-digit)
- **Algeria_Postal_Code.grammar** - Algeria (5-digit)
- **Ghana_Postal_Code.grammar** - Ghana
- **Tunisia_Postal_Code.grammar** - Tunisia

### South America
- **Brazil_Postal_Code.grammar** - Brazil (CEP: 5 or 8-digit)
- **Argentina_Postal_Code.grammar** - Argentina (4 or 8-digit)
- **Chile_Postal_Code.grammar** - Chile (7-digit)
- **Colombia_Postal_Code.grammar** - Colombia (6-digit)
- **Peru_Postal_Code.grammar** - Peru (5-digit)
- **Venezuela_Postal_Code.grammar** - Venezuela (4-digit)
- **Uruguay_Postal_Code.grammar** - Uruguay
- **Paraguay_Postal_Code.grammar** - Paraguay

## 🎯 Features

### Each Grammar Includes:
- ✅ Full postal code format validation
- ✅ Address parsing and formatting
- ✅ Country-specific validation rules
- ✅ Special postal code handling (military, diplomatic, PO Box, etc.)
- ✅ Geographic region detection
- ✅ State/province/region codes
- ✅ City and locality names
- ✅ Street address components
- ✅ Comprehensive metadata and comments

### Supported Formats:
- Numeric codes (4-7 digits)
- Alphanumeric codes (Canada, UK, Netherlands, etc.)
- Hyphenated codes (Japan, Brazil, etc.)
- Spaced codes (Canada, Sweden, etc.)
- Mixed formats (Argentina, etc.)

## 📖 Usage

### Basic Usage
```
// Load a specific country grammar
<postal-address> ::= <us-postal-address>  // From US_Postal_Code.grammar

// Or use the international grammar for any country
<postal-address> ::= <postal-address>  // From International_Postal_Codes.grammar
```

### Validation
```
// Validate a US ZIP code
<us-zip-validation> ::= <us-zip-5> "->" "Valid US ZIP-5"

// Validate a UK postcode
<uk-postcode-validation> ::= <uk-postcode> "->" "Valid UK Postcode"
```

### Country Detection
```
// Detect country from postal code pattern
<country-from-postal> ::= <us-postal-pattern> "->" "US"
```

## 🔧 Integration with Minotaur Marketplace

### Grammar Package Format
Each postal code grammar can be packaged for the Minotaur Marketplace with the following `minotaur.grammar.json`:

```json
{
  "name": "us-postal-code",
  "vendor": "minotaur-official",
  "displayName": "United States Postal Code Validator",
  "version": "2.0.0",
  "minotaurVersion": ">=1.0.0",
  "description": "Comprehensive validator for US ZIP codes including 5-digit, ZIP+4, military, and PO Box formats",
  "license": "MIT",
  "category": "validation",
  "subcategory": "postal-codes",
  "tags": ["postal", "zip", "address", "validation", "us", "united-states"],
  "countries": ["US", "USA", "United States"],
  "main": "US_Postal_Code.grammar",
  "formatType": "CEBNF",
  "contextSensitive": true,
  "features": {
    "validation": true,
    "parsing": true,
    "generation": false,
    "countryDetection": true
  },
  "validationRules": {
    "US-ZIP-5": "^\\d{5}$",
    "US-ZIP-5-4": "^\\d{5}-\\d{4}$",
    "US-Military": "^(APO|FPO|DPO) (AA|AE|AP) \\d{5}$"
  },
  "testCases": {
    "valid": ["90210", "10001-1234", "APO AA 12345"],
    "invalid": ["ABCDE", "123", "123456"]
  },
  "dependencies": {},
  "documentation": "README.md"
}
```

### Uploading to Minotaur Marketplace

1. **Create a package directory:**
```bash
mkdir us-postal-code-grammar
cd us-postal-code-grammar
```

2. **Add grammar files:**
```bash
cp ../US_Postal_Code.grammar .
cp ../minotaur.grammar.json .
```

3. **Create a tar.gz package:**
```bash
tar -czvf us-postal-code-2.0.0.tar.gz US_Postal_Code.grammar minotaur.grammar.json README.md
```

4. **Upload via CLI:**
```bash
minotaur-marketplace publish --directory ./us-postal-code-grammar
```

5. **Or upload via API:**
```bash
curl -X POST https://api.marketplace.minotaur.dev/api/grammars/publish \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "package=@us-postal-code-2.0.0.tar.gz" \
  -F "name=us-postal-code" \
  -F "vendor=your-vendor" \
  -F "version=2.0.0" \
  -F "description=US Postal Code Validator"
```

## 🚀 Creating New Postal Code Grammars

### Template for New Country Grammars

```grammar
Grammar: COUNTRY_Postal_Code
TokenSplitter: Space
FormatType: CEBNF
ContextSensitive: true

/*
 * [Postal Authority Name] Grammar
 * 
 * Comprehensive grammar for [Country] postal addresses and postal codes including:
 * - [Format description]
 * - [Special features]
 * - [Validation rules]
 * 
 * Version: 2.0.0
 * Country: [Country Code] ([Country Name])
 * Postal Authority: [Authority Name]
 */

<country-postal-address> ::= 
    <country-name-part>? <EOL>
    <country-street-address> <EOL>
    <country-city-line> <EOL>
    <country-postal-line> <EOL>
    <country-country-indicator>?

/* Add country-specific rules here */

/* ============================================================================
 * METADATA
 * ============================================================================ */

/*
 * [Country] Postal Facts:
 * - Format: [description]
 * - Introduced: [year]
 * - Range: [range]
 * - Special codes: [special codes]
 * 
 * Validation Notes:
 * - [note 1]
 * - [note 2]
 */
```

### Guidelines for New Grammars

1. **Follow existing patterns** from the provided grammars
2. **Include comprehensive comments** explaining the format
3. **Add validation rules** for different code types
4. **Include special cases** (military, diplomatic, PO Box, etc.)
5. **Add metadata section** with country-specific information
6. **Test thoroughly** with real postal codes from the country
7. **Document** the grammar with examples

## 📊 Coverage Statistics

| Region | Countries | Grammars | Coverage |
|--------|-----------|---------|----------|
| North America | 3 | 3 | 100% |
| Europe | 15+ | 15+ | 90% |
| Asia | 12+ | 12+ | 85% |
| Oceania | 3+ | 3+ | 100% |
| Africa | 7+ | 7+ | 80% |
| South America | 8+ | 8+ | 95% |
| **Total** | **50+** | **50+** | **~90%** |

## 🎨 Color Coding for Validation

Each grammar supports color-coded validation results:

- ✅ **Green**: Valid postal code
- ❌ **Red**: Invalid format
- ⚠️ **Yellow**: Warning (deprecated format, etc.)
- 🔵 **Blue**: Special code (military, diplomatic)

## 🔗 Related Resources

- [Minotaur Marketplace](https://github.com/DevelApp-ai/Minotaur-Marketplace)
- [Minotaur Core](https://github.com/DevelApp-ai/Minotaur)
- [Postal Code Wikipedia](https://en.wikipedia.org/wiki/Postal_code)
- [Universal Postal Union](https://www.upu.int/)

## 📝 Contributing

Contributions are welcome! To add a new postal code grammar:

1. Fork this repository
2. Create a new grammar file following the template
3. Add it to the appropriate regional directory
4. Update the `PostalCode_Index.grammar` file
5. Test your grammar with real postal codes
6. Submit a pull request

### Quality Standards

- ✅ Must validate real postal codes from the country
- ✅ Must include comprehensive comments
- ✅ Must follow existing naming conventions
- ✅ Must include metadata section
- ✅ Must have test cases
- ✅ Must be properly formatted

## 📄 License

All grammars in this directory are licensed under the **MIT License** unless otherwise specified.

## 🙏 Acknowledgments

- Built with ❤️ by the DevelApp AI team
- Inspired by the need for comprehensive international address validation
- Thanks to all contributors and postal authorities worldwide

---

**Postal Codes Grammars** - Making address validation easy for everyone, everywhere.
