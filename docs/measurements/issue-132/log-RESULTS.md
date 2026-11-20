# Issue #132 pilot (2/3 and 3/3): nginx-combined, syslog RFC 3164 and logfmt, grammar vs hand-written parsers

Measured at `e8016927`. Each format pairs a Minotaur grammar (per-line validity plus one
whole-corpus file parse with strict positional semantics) with a hand-written incumbent
(compiled regex for nginx and syslog, a character scanner for logfmt).

## Measurement (5,000 lines per format, per-line mode)

| format | lines | bytes | minotaur us/line | hand us/line | slowdown | minotaur lines/s | hand lines/s |
| --- | --- | --- | --- | --- | --- | --- | --- |
| nginx-combined | 5000 | 687506 | 417.92 | 2.45 | x165.1 | 2363 | 389997 |
| syslog-3164 | 5000 | 311923 | 201.91 | 1.18 | x161.3 | 4896 | 789752 |
| logfmt | 5000 | 616606 | 3118.26 | 1.54 | x1910.3 | 320 | 611785 |

## File mode (single Initialize+Parse over a 30-line corpus)

File mode is measured on a 30-line subset of each corpus so the three formats stay
comparable: whole-file parses hit an engine GLR path-budget ceiling at different sizes
(probed this run: nginx clean through 2000 (no failure within the probed ladder), syslog clean at 1000 but failing at 2000,
logfmt clean at 30 but failing at 40; the failing parse returns success=false with an
empty resolved-token stream and no diagnostics). The per-format caps are engine
findings, recorded as such in the results comment.

| format | file lines | file parse wall ms | records equal to hand parsers |
| --- | --- | --- | --- |
| nginx-combined | 30 | 25.2 | yes (30 records) |
| syslog-3164 | 30 | 21.7 | yes (30 records) |
| logfmt | 30 | 50.6 | yes (30 records) |

## Honest limitations

- Corpus lines are generator-produced (seeded): realistic shapes, not production captures;
  nginx bytes are always numeric (>= 1000) and quoted strings contain no embedded quotes.
- Syslog timestamps use one month label and RFC 3164's double-space day padding.
- All per-line measurements run in one process; ratios matter more than absolutes.

