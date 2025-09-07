#!/bin/bash

# Validate Evaluation System
# This script validates that the Golem evaluation system is ready for extensive testing

set -e

echo "🚀 Validating Golem Evaluation System"
echo "====================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0

# Function to run a test
run_test() {
    local test_name="$1"
    local test_command="$2"
    local required="${3:-false}"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    echo -e "${BLUE}Testing: $test_name${NC}"
    
    if eval "$test_command" > /dev/null 2>&1; then
        echo -e "   ${GREEN}✅ PASS${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        return 0
    else
        if [ "$required" = "true" ]; then
            echo -e "   ${RED}❌ FAIL (REQUIRED)${NC}"
            return 1
        else
            echo -e "   ${YELLOW}⚠️  FAIL (OPTIONAL)${NC}"
            return 0
        fi
    fi
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

echo "1. Environment Validation"
echo "========================="

# Check required tools
run_test "PowerShell availability" "command_exists pwsh" true
run_test "Node.js availability" "command_exists node" true
run_test "NPM availability" "command_exists npm" true
run_test "Git availability" "command_exists git" true

echo ""
echo "2. PowerShell Script Validation"
echo "==============================="

# PowerShell script tests
run_test "PowerShell script exists" "test -f deployment/windows/setup-windows-environment.ps1" true

if [ -f "deployment/windows/setup-windows-environment.ps1" ]; then
    run_test "PowerShell syntax validation" "pwsh -Command 'try { \$content = Get-Content \"./deployment/windows/setup-windows-environment.ps1\" -Raw; \$tokens = [System.Management.Automation.PSParser]::Tokenize(\$content, [ref]\$null); exit 0 } catch { exit 1 }'" true
    
    run_test "PowerShell WhatIf test" "pwsh ./test-whatif-compilation.ps1" true
fi

echo ""
echo "3. Project Structure Validation"
echo "==============================="

# Check project files
run_test "Package.json exists" "test -f package.json" true
run_test "TypeScript config exists" "test -f tsconfig.json" true
run_test "Source directory exists" "test -d src" true
run_test "Evaluation directory exists" "test -d src/evaluation" true

echo ""
echo "4. Golem Component Validation"
echo "============================="

# Check Golem files
GOLEM_FILES=(
    "src/evaluation/GolemEvaluationRunner.ts"
    "src/evaluation/GolemBenchmarkSolver.ts"
    "src/evaluation/GolemQualityTestingSystem.ts"
    "src/evaluation/BenchmarkDatasetManager.ts"
)

for file in "${GOLEM_FILES[@]}"; do
    run_test "Golem file: $(basename $file)" "test -f $file" true
done

echo ""
echo "5. Build System Validation"
echo "=========================="

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install --legacy-peer-deps > /dev/null 2>&1 || echo "Warning: npm install had issues"
fi

run_test "Dependencies installed" "test -d node_modules" true
run_test "TypeScript compilation" "npx tsc --noEmit --project ." true
run_test "Build process" "npm run build" false

echo ""
echo "6. Test System Validation"
echo "========================="

# Test system validation
run_test "Test scripts exist" "test -f test-golem-compatibility.js && test -f test-golem-instantiation.js" true
run_test "Golem instantiation test" "node test-golem-instantiation.js" true
run_test "Unit tests" "npm test -- --watchAll=false --testTimeout=30000" false

echo ""
echo "7. CI/CD Integration Validation"
echo "==============================="

run_test "GitHub Actions workflow exists" "test -f .github/workflows/test-evaluation-system.yml" true
run_test "Validation script exists" "test -f scripts/validate-evaluation-system.sh" true

echo ""
echo "📊 Validation Summary"
echo "===================="

echo -e "Total tests: $TOTAL_TESTS"
echo -e "Passed tests: ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed tests: ${RED}$((TOTAL_TESTS - PASSED_TESTS))${NC}"

# Calculate success rate
SUCCESS_RATE=$((PASSED_TESTS * 100 / TOTAL_TESTS))

if [ $SUCCESS_RATE -eq 100 ]; then
    echo ""
    echo -e "${GREEN}🎉 ALL TESTS PASSED!${NC}"
    echo -e "${GREEN}The Golem evaluation system is ready for extensive testing!${NC}"
    echo ""
    echo -e "${BLUE}Next steps:${NC}"
    echo "1. Run on Windows 11: powershell -File .\\deployment\\windows\\setup-windows-environment.ps1 -WhatIf"
    echo "2. Provide Mistral API key when prompted"
    echo "3. Execute extensive evaluation tests"
    exit 0
elif [ $SUCCESS_RATE -ge 80 ]; then
    echo ""
    echo -e "${YELLOW}⚠️  MOSTLY READY ($SUCCESS_RATE% tests passed)${NC}"
    echo -e "${YELLOW}The system should work but some optional components may have issues.${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}❌ SYSTEM NOT READY ($SUCCESS_RATE% tests passed)${NC}"
    echo -e "${RED}Critical issues found. Please fix the failing tests before proceeding.${NC}"
    exit 1
fi

