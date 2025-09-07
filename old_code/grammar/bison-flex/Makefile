# Makefile for Bison/Flex Grammars
# Compiles all grammar examples

CC = gcc
CFLAGS = -Wall -g
BISON = bison
FLEX = flex

# Targets
TARGETS = calculator expression config

.PHONY: all clean

all: $(TARGETS)

# Calculator
calculator: calculator.tab.c lex.yy.c
	$(CC) $(CFLAGS) -o $@ $^ -lm

calculator.tab.c: calculator.y
	$(BISON) -d $<

lex.yy.c: calculator.l calculator.tab.h
	$(FLEX) $<

# Expression
expression: expression.tab.c expression.lex.c
	$(CC) $(CFLAGS) -o $@ $^ -lm

expression.tab.c: expression.y
	$(BISON) -d -b expression $<

expression.lex.c: expression.l expression.tab.h
	$(FLEX) -o $@ $<

# Config
config: config.tab.c config.lex.c
	$(CC) $(CFLAGS) -o $@ $^

config.tab.c: config.y
	$(BISON) -d -b config $<

config.lex.c: config.l config.tab.h
	$(FLEX) -o $@ $<

# Clean
clean:
	rm -f $(TARGETS)
	rm -f *.tab.c *.tab.h *.lex.c lex.yy.c
	rm -f *.o

# Test targets
test-calculator: calculator
	@echo "Testing calculator..."
	@echo "2 + 3 * 4" | ./calculator

test-expression: expression
	@echo "Testing expression..."
	@echo "10 + 5 * 2" | ./expression

test-config: config
	@echo "Testing config parser..."
	@echo -e "[database]\nhost = localhost\nport = 3306" | ./config

test: test-calculator test-expression test-config

# Help
help:
	@echo "Available targets:"
	@echo "  all        - Build all grammars"
	@echo "  calculator - Build calculator example"
	@echo "  expression - Build expression example"
	@echo "  config     - Build config parser example"
	@echo "  test       - Run all tests"
	@echo "  clean      - Remove generated files"
	@echo "  help       - Show this help"

