# Golem Evaluation Environment Setup for Windows 11
# This script sets up a production-ready environment for 14-hour evaluations

param(
    [string]$InstallPath = "C:\GolemEvaluation",
    [string]$MistralApiKey = "",
    [string]$MistralEndpoint = "https://api.mistral.ai/v1",
    [switch]$SkipNodeInstall = $false,
    [switch]$SkipPythonInstall = $false
)

Write-Host "Setting up Golem Evaluation Environment on Windows 11" -ForegroundColor Green
Write-Host "Installation Path: $InstallPath" -ForegroundColor Cyan

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Please restart PowerShell as Administrator."
    exit 1
}

# Prompt for Mistral API key if not provided
if ([string]::IsNullOrEmpty($MistralApiKey)) {
    Write-Host ""
    Write-Host "Mistral AI API Configuration" -ForegroundColor Yellow
    Write-Host "Please enter your Mistral API key (input will be hidden for security):" -ForegroundColor Cyan
    $MistralApiKey = Read-Host -AsSecureString | ConvertFrom-SecureString -AsPlainText
    
    if ([string]::IsNullOrEmpty($MistralApiKey)) {
        Write-Warning "No API key provided. You can add it later to the .env file."
        $MistralApiKey = ""
    } else {
        Write-Host "API key received and will be configured securely." -ForegroundColor Green
    }
    Write-Host ""
}

# Create installation directory
Write-Host "Creating installation directory..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
Set-Location $InstallPath

# Install Chocolatey if not present
if (!(Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Chocolatey package manager..." -ForegroundColor Yellow
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    refreshenv
}

# Install Node.js if not present or if not skipped
if (!$SkipNodeInstall -and !(Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Node.js..." -ForegroundColor Yellow
    choco install nodejs -y
    refreshenv
}

# Install Python if not present or if not skipped
if (!$SkipPythonInstall -and !(Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Python..." -ForegroundColor Yellow
    choco install python -y
    refreshenv
}

# Install Git if not present
if (!(Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Git..." -ForegroundColor Yellow
    choco install git -y
    refreshenv
}

# Verify installations
Write-Host "Verifying installations..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Error "Node.js installation failed or not found in PATH"
    exit 1
}

try {
    $pythonVersion = python --version
    Write-Host "Python: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Error "Python installation failed or not found in PATH"
    exit 1
}

try {
    $gitVersion = git --version
    Write-Host "Git: $gitVersion" -ForegroundColor Green
} catch {
    Write-Error "Git installation failed or not found in PATH"
    exit 1
}

# Clone Golem repository
Write-Host "Cloning Golem repository..." -ForegroundColor Yellow
if (Test-Path "Minotaur") {
    Write-Host "Repository already exists, pulling latest changes..." -ForegroundColor Cyan
    Set-Location Minotaur
    git pull origin main
} else {
    # Clone the correct DevelApp-ai repository
    git clone https://github.com/DevelApp-ai/Minotaur.git
    Set-Location Minotaur
    git checkout main
}

# Install Node.js dependencies
Write-Host "Installing Node.js dependencies..." -ForegroundColor Yellow
Write-Host "Using --legacy-peer-deps to resolve TypeScript version conflicts..." -ForegroundColor Cyan
npm install --legacy-peer-deps

if ($LASTEXITCODE -ne 0) {
    Write-Host "npm install failed, trying with --force..." -ForegroundColor Yellow
    npm install --force
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install Node.js dependencies. Please check your Node.js installation and network connection."
        exit 1
    }
}

# Install Python dependencies
Write-Host "Installing Python dependencies..." -ForegroundColor Yellow
python -m pip install --upgrade pip
pip install pytest numpy pandas matplotlib requests beautifulsoup4 lxml

# Build the project
Write-Host "Building the project..." -ForegroundColor Yellow
npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Error "Project build failed. Please check for compilation errors."
    exit 1
}

Write-Host "Project built successfully" -ForegroundColor Green

# Create environment configuration
Write-Host "Creating environment configuration..." -ForegroundColor Yellow
$envConfigContent = @(
    "# Golem Evaluation Environment Configuration",
    "NODE_ENV=production",
    "MISTRAL_API_KEY=$MistralApiKey",
    "MISTRAL_API_BASE=$MistralEndpoint",
    "EVALUATION_WORKSPACE=$InstallPath\workspace",
    "EVALUATION_RESULTS=$InstallPath\results",
    "EVALUATION_LOGS=$InstallPath\logs",
    "CHECKPOINT_INTERVAL=300",
    "MAX_RETRIES=5",
    "BATCH_SIZE=50",
    "PARALLEL_WORKERS=4",
    "ENABLE_CHECKPOINTING=true",
    "ENABLE_AUTO_RECOVERY=true",
    "LOG_LEVEL=info"
)

$envConfigContent | Out-File -FilePath ".env" -Encoding UTF8

# Create workspace directories
Write-Host "Creating workspace directories..." -ForegroundColor Yellow
$directories = @(
    "$InstallPath\workspace",
    "$InstallPath\results",
    "$InstallPath\logs",
    "$InstallPath\checkpoints",
    "$InstallPath\benchmark_data"
)

foreach ($dir in $directories) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

# Create Windows service configuration
Write-Host "Creating Windows service configuration..." -ForegroundColor Yellow
$serviceConfigContent = @(
    "{",
    "  `"name`": `"GolemEvaluationService`",",
    "  `"description`": `"Golem Benchmark Evaluation Service`",",
    "  `"script`": `"$InstallPath\\Minotaur\\src\\evaluation\\run-evaluation.js`",",
    "  `"nodeOptions`": [`"--max-old-space-size=8192`"],",
    "  `"env`": {",
    "    `"NODE_ENV`": `"production`",",
    "    `"EVALUATION_MODE`": `"service`"",
    "  },",
    "  `"logpath`": `"$InstallPath\\logs`",",
    "  `"logmode`": `"rotate`",",
    "  `"error`": `"$InstallPath\\logs\\error.log`",",
    "  `"out`": `"$InstallPath\\logs\\output.log`"",
    "}"
)

$serviceConfigContent | Out-File -FilePath "service-config.json" -Encoding UTF8

# Install node-windows for service management
Write-Host "Installing Windows service management..." -ForegroundColor Yellow
npm install -g node-windows

# Create PowerShell execution policy bypass for the current user
Write-Host "Configuring PowerShell execution policy..." -ForegroundColor Yellow
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force

# Create startup scripts
Write-Host "Creating startup scripts..." -ForegroundColor Yellow

# Create start-evaluation.ps1
$startScriptContent = @"
# Start Golem Evaluation with Full Configuration Options
param(
    [string]`$ConfigFile = "evaluation-config.json",
    [switch]`$Resume = `$false,
    [int]`$Problems = 0,  # 0 = all problems, >0 = limit per benchmark
    [string]`$Benchmarks = "humaneval,mbpp,swe-bench,quixbugs,fim",
    [switch]`$Verbose = `$false,
    [switch]`$DryRun = `$false,
    [string]`$OutputDir = "",
    [switch]`$Help = `$false
)

if (`$Help) {
    Write-Host "Golem Evaluation Runner" -ForegroundColor Green
    Write-Host "========================"
    Write-Host ""
    Write-Host "Usage Examples:" -ForegroundColor Yellow
    Write-Host "  .\start-evaluation.ps1                              # Run all problems (full evaluation)"
    Write-Host "  .\start-evaluation.ps1 -Problems 50                 # Run 50 problems per benchmark"
    Write-Host "  .\start-evaluation.ps1 -Benchmarks ""humaneval,mbpp"" # Run only specific benchmarks"
    Write-Host "  .\start-evaluation.ps1 -Resume                      # Resume interrupted evaluation"
    Write-Host "  .\start-evaluation.ps1 -DryRun -Verbose             # Test configuration"
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -Problems <number>     Limit problems per benchmark (0 = all)"
    Write-Host "  -Benchmarks <list>     Comma-separated benchmark list"
    Write-Host "  -Resume                Resume from last checkpoint"
    Write-Host "  -Verbose               Enable detailed logging"
    Write-Host "  -DryRun                Validate configuration only"
    Write-Host "  -OutputDir <path>      Custom output directory"
    Write-Host "  -ConfigFile <path>     Custom configuration file"
    Write-Host ""
    Write-Host "Expected Results:" -ForegroundColor Yellow
    Write-Host "  HumanEval: ~164 problems"
    Write-Host "  MBPP: ~500 problems"
    Write-Host "  SWE-Bench: ~300 problems"
    Write-Host "  QuixBugs: ~40 problems"
    Write-Host "  FIM: ~100 problems"
    Write-Host "  Total: ~1,100+ problems (full evaluation)"
    Write-Host ""
    return
}

Set-Location "$InstallPath\Minotaur"

# Build command arguments
`$args = @()
if (`$ConfigFile -ne "evaluation-config.json") {
    `$args += "--config"
    `$args += `$ConfigFile
}
if (`$Benchmarks -ne "humaneval,mbpp,swe-bench,quixbugs,fim") {
    `$args += "--benchmarks"
    `$args += `$Benchmarks
}
if (`$Problems -gt 0) {
    `$args += "--problems"
    `$args += `$Problems
}
if (`$OutputDir -ne "") {
    `$args += "--output"
    `$args += `$OutputDir
}
if (`$Verbose) {
    `$args += "--verbose"
}
if (`$DryRun) {
    `$args += "--dry-run"
}

# Display configuration
Write-Host "Golem Evaluation Configuration" -ForegroundColor Green
Write-Host "================================"
Write-Host "Benchmarks: `$Benchmarks" -ForegroundColor Cyan
if (`$Problems -gt 0) {
    Write-Host "Problems per benchmark: `$Problems" -ForegroundColor Cyan
    Write-Host "Total problems: ~`$(`$Problems * (`$Benchmarks.Split(',').Count))" -ForegroundColor Cyan
} else {
    Write-Host "Problems per benchmark: ALL" -ForegroundColor Cyan
    Write-Host "Total problems: ~1,100+ (full evaluation)" -ForegroundColor Cyan
}
Write-Host "Rate limiting: 1 request per second (built-in)" -ForegroundColor Cyan
Write-Host "Expected duration: Several hours" -ForegroundColor Cyan
Write-Host ""

if (`$Resume) {
    Write-Host "Resuming evaluation from checkpoint..." -ForegroundColor Yellow
    if (`$args.Count -gt 0) {
        npm run eval:resume -- `$args
    } else {
        npm run eval:resume
    }
} else {
    if (`$DryRun) {
        Write-Host "Dry run mode - validating configuration..." -ForegroundColor Yellow
    } else {
        Write-Host "Starting new evaluation..." -ForegroundColor Green
    }
    if (`$args.Count -gt 0) {
        npm run eval:full -- `$args
    } else {
        npm run eval:full
    }
}
"@

$startScriptContent | Out-File -FilePath "$InstallPath\start-evaluation.ps1" -Encoding UTF8

# Create stop-evaluation.ps1
$stopScriptContent = @(
    "# Stop Golem Evaluation",
    "Write-Host `"Stopping Golem evaluation...`" -ForegroundColor Yellow",
    "",
    "# Find and stop the evaluation process",
    "Get-Process -Name `"node`" | Where-Object { `$_.CommandLine -like `"*eval*`" -or `$_.Path -like `"*Minotaur*`" } | Stop-Process -Force",
    "",
    "Write-Host `"Evaluation stopped`" -ForegroundColor Green"
)

$stopScriptContent | Out-File -FilePath "$InstallPath\stop-evaluation.ps1" -Encoding UTF8

# Create status-check.ps1
$statusScriptContent = @(
    "# Check Golem Evaluation Status",
    "Write-Host `"Checking Golem evaluation status...`" -ForegroundColor Yellow",
    "",
    "# Check if evaluation is running",
    "`$process = Get-Process -Name `"node`" -ErrorAction SilentlyContinue | Where-Object { `$_.CommandLine -like `"*eval*`" -or `$_.Path -like `"*Minotaur*`" }",
    "",
    "if (`$process) {",
    "    Write-Host `"Evaluation is running (PID: `$(`$process.Id))`" -ForegroundColor Green",
    "    ",
    "    # Check latest results",
    "    `$resultsDir = `"$InstallPath\results`"",
    "    if (Test-Path `$resultsDir) {",
    "        `$latestResult = Get-ChildItem `$resultsDir -Filter `"*.json`" | Sort-Object LastWriteTime -Descending | Select-Object -First 1",
    "        if (`$latestResult) {",
    "            Write-Host `"Latest result: `$(`$latestResult.Name) (`$(`$latestResult.LastWriteTime))`" -ForegroundColor Cyan",
    "        }",
    "    }",
    "    ",
    "    # Show recent log entries",
    "    `$logFile = `"$InstallPath\logs\output.log`"",
    "    if (Test-Path `$logFile) {",
    "        Write-Host `"Recent log entries:`" -ForegroundColor Cyan",
    "        Get-Content `$logFile -Tail 10",
    "    }",
    "} else {",
    "    Write-Host `"Evaluation is not running`" -ForegroundColor Red",
    "}"
)

$statusScriptContent | Out-File -FilePath "$InstallPath\status-check.ps1" -Encoding UTF8

# Create default evaluation configuration
Write-Host "Creating default evaluation configuration..." -ForegroundColor Yellow
$evalConfigContent = @(
    "{",
    "  `"benchmarks`": [`"humaneval`", `"mbpp`", `"swe-bench`", `"quixbugs`", `"fim`"],",
    "  `"problemsPerBenchmark`": null,",
    "  `"solutionConfig`": {",
    "    `"maxAttempts`": 3,",
    "    `"timeoutMs`": 120000,",
    "    `"strategy`": `"best-result`",",
    "    `"enableRuleBased`": true,",
    "    `"enablePatternBased`": true,",
    "    `"enableLLM`": true,",
    "    `"maxCostPerProblem`": 10,",
    "    `"minConfidence`": 0.3",
    "  },",
    "  `"validationConfig`": {",
    "    `"timeoutMs`": 60000,",
    "    `"maxRetries`": 2,",
    "    `"enableSandbox`": true,",
    "    `"validateSyntax`": true,",
    "    `"validateSemantics`": true",
    "  },",
    "  `"calculatePassAtK`": [1, 5, 10],",
    "  `"generateDetailedReports`": true,",
    "  `"exportResults`": true,",
    "  `"outputDirectory`": `"$InstallPath\\results`",",
    "  `"parallelProcessing`": false,",
    "  `"maxConcurrentEvaluations`": 4,",
    "  `"enableProgressReporting`": true,",
    "  `"saveIntermediateResults`": true,",
    "  `"checkpointInterval`": 300,",
    "  `"enableAutoRecovery`": true,",
    "  `"maxEvaluationTime`": 50400000",
    "}"
)

$evalConfigContent | Out-File -FilePath "evaluation-config.json" -Encoding UTF8

# Configure Windows Defender exclusions for better performance
Write-Host "Configuring Windows Defender exclusions..." -ForegroundColor Yellow
try {
    Add-MpPreference -ExclusionPath $InstallPath
    Add-MpPreference -ExclusionProcess "node.exe"
    Add-MpPreference -ExclusionProcess "python.exe"
    Write-Host "Windows Defender exclusions configured" -ForegroundColor Green
} catch {
    Write-Warning "Could not configure Windows Defender exclusions. You may need to add them manually for better performance."
}

# Configure power settings to prevent sleep
Write-Host "Configuring power settings..." -ForegroundColor Yellow
try {
    powercfg /change standby-timeout-ac 0
    powercfg /change standby-timeout-dc 0
    powercfg /change hibernate-timeout-ac 0
    powercfg /change hibernate-timeout-dc 0
    Write-Host "Power settings configured to prevent sleep" -ForegroundColor Green
} catch {
    Write-Warning "Could not configure power settings. Please manually set to prevent sleep during evaluation."
}

# Create scheduled task for automatic restart
Write-Host "Creating scheduled task for monitoring..." -ForegroundColor Yellow
$taskAction = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File `"$InstallPath\status-check.ps1`""
$taskTrigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes 30)
$taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

try {
    Register-ScheduledTask -TaskName "GolemEvaluationMonitor" -Action $taskAction -Trigger $taskTrigger -Settings $taskSettings -Description "Monitor Golem evaluation status" -Force
    Write-Host "Monitoring scheduled task created" -ForegroundColor Green
} catch {
    Write-Warning "Could not create scheduled task. Manual monitoring may be required."
}

# Final setup summary
Write-Host ""
Write-Host "Golem Evaluation Environment Setup Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Installation Summary:" -ForegroundColor Cyan
Write-Host "- Installation Path: $InstallPath" -ForegroundColor White
Write-Host "- Repository: $InstallPath\Minotaur" -ForegroundColor White
Write-Host "- Configuration: .env file created" -ForegroundColor White
Write-Host "- Scripts: start-evaluation.ps1, stop-evaluation.ps1, status-check.ps1" -ForegroundColor White
Write-Host "- Monitoring: Scheduled task created" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review the .env file and update any configuration as needed" -ForegroundColor White
Write-Host "2. Run evaluation:" -ForegroundColor White
Write-Host "   .\start-evaluation.ps1                    # Full evaluation (~1,100 problems)" -ForegroundColor Gray
Write-Host "   .\start-evaluation.ps1 -Problems 50       # Limited evaluation (250 problems)" -ForegroundColor Gray
Write-Host "   .\start-evaluation.ps1 -Help              # Show all options" -ForegroundColor Gray
Write-Host "3. Monitor progress with: .\status-check.ps1" -ForegroundColor White
Write-Host "4. Resume interrupted evaluation: .\start-evaluation.ps1 -Resume" -ForegroundColor White
Write-Host "5. Stop evaluation with: .\stop-evaluation.ps1" -ForegroundColor White
Write-Host ""
Write-Host "For 14-hour evaluations, ensure:" -ForegroundColor Yellow
Write-Host "- Stable power supply (UPS recommended)" -ForegroundColor White
Write-Host "- Stable internet connection" -ForegroundColor White
Write-Host "- Sufficient disk space for results" -ForegroundColor White
Write-Host "- Windows Defender exclusions are active" -ForegroundColor White
Write-Host ""

