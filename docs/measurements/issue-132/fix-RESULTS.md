# Issue #132 pilot (1/3): FIX 4.4, grammar vs hand-written parser

Measured at `e8016927`. Both systems implement the same message spec: SOH-terminated
`<tag>=<value>` fields, first field `8=FIX.4.4`, second `9=<body length>`, last `10=<checksum>`,
checksum = sum of message bytes preceding `10=` mod 256, zero-padded to 3 digits.

## Correctness gate

| case | expected | minotaur | hand |
| --- | --- | --- | --- |
| V1 | valid | valid | valid |
| V2 | valid | valid | valid |
| V3 | valid | valid | valid |
| I1 | invalid | invalid | invalid |
| I2 | invalid | invalid | invalid |
| I3 | invalid | invalid | invalid |
| I4 | invalid | invalid | invalid |

## Latency and throughput (100000 messages, seed 421337)

Message mix: 15% heartbeat, 35% NewOrderSingle, 35% ExecutionReport, 10% MarketDataSnapshot,
5% Logon; fresh session per message (the engine's per-buffer object has no reset API,
issue #127 finding); per-message latencies over a full pass.

| metric | minotaur (grammar) | hand (scanner) |
| --- | --- | --- |
| p50 per message | 489.5 us | 0.5 us |
| p95 per message | 1865.2 us | 0.9 us |
| p99 per message | 2706.2 us | 1.1 us |
| throughput | 1662 msgs/s | 1608547 msgs/s |
| slowdown factor | x967.8 | 1.0 (baseline) |

100000 messages, 7144648 bytes; invalid verdicts during measurement: 0.

## Honest limitations

- Latency measured per message in a single process on one sandbox machine (1 vCPU);
  absolute numbers are indicative, the grammar-vs-hand ratio is the point.
- The corpus models a representative field mix, not an exchange's full session lifecycle.

