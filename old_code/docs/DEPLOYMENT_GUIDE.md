# Deployment and Maintenance Guide

## Table of Contents
1. [Deployment Overview](#deployment-overview)
2. [System Requirements](#system-requirements)
3. [Installation Methods](#installation-methods)
4. [Configuration](#configuration)
5. [Environment Setup](#environment-setup)
6. [Security Configuration](#security-configuration)
7. [Monitoring and Logging](#monitoring-and-logging)
8. [Backup and Recovery](#backup-and-recovery)
9. [Maintenance Procedures](#maintenance-procedures)
10. [Troubleshooting](#troubleshooting)
11. [Performance Optimization](#performance-optimization)
12. [Scaling Guidelines](#scaling-guidelines)

## Deployment Overview

The Transformation Rule Creation and Management System supports multiple deployment scenarios, from single-user development environments to enterprise-scale production deployments. The system's LLM-agnostic architecture ensures it can operate in any environment, including air-gapped networks.

### Deployment Architectures

#### 1. Single-User Development
```
┌─────────────────┐
│   Developer     │
│   Workstation   │
│                 │
│ ┌─────────────┐ │
│ │   Minotaur  │ │
│ │   System    │ │
│ └─────────────┘ │
└─────────────────┘
```

#### 2. Team Deployment
```
┌─────────────────┐    ┌─────────────────┐
│   Developer 1   │    │   Developer 2   │
└─────────┬───────┘    └─────────┬───────┘
          │                      │
          └──────────┬───────────┘
                     │
          ┌─────────────────┐
          │  Shared Server  │
          │                 │
          │ ┌─────────────┐ │
          │ │   Minotaur  │ │
          │ │   System    │ │
          │ └─────────────┘ │
          └─────────────────┘
```

#### 3. Enterprise Deployment
```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│Load Balancer│    │Load Balancer│    │Load Balancer│
└──────┬──────┘    └──────┬──────┘    └──────┬──────┘
       │                  │                  │
┌──────┴──────┐    ┌──────┴──────┐    ┌──────┴──────┐
│ App Server 1│    │ App Server 2│    │ App Server 3│
└──────┬──────┘    └──────┬──────┘    └──────┬──────┘
       │                  │                  │
       └──────────────────┼──────────────────┘
                          │
                ┌─────────┴─────────┐
                │   Database        │
                │   Cluster         │
                └───────────────────┘
```

## System Requirements

### Minimum Requirements

#### Hardware
- **CPU**: 2 cores, 2.0 GHz
- **Memory**: 4 GB RAM
- **Storage**: 10 GB available space
- **Network**: Optional (system works offline)

#### Software
- **Operating System**: 
  - Windows 10/11
  - macOS 10.15+
  - Linux (Ubuntu 18.04+, CentOS 7+, RHEL 7+)
- **Node.js**: 16.0+ (for development/build)
- **Browser**: Chrome 90+, Firefox 88+, Safari 14+, Edge 90+

### Recommended Requirements

#### Hardware
- **CPU**: 4 cores, 3.0 GHz
- **Memory**: 8 GB RAM
- **Storage**: 50 GB available space (SSD preferred)
- **Network**: 10 Mbps (if using LLM features)

#### Software
- **Operating System**: Latest stable versions
- **Node.js**: 18.0+ LTS
- **Database**: PostgreSQL 12+ (for enterprise deployments)
- **Reverse Proxy**: Nginx 1.18+ or Apache 2.4+

### Enterprise Requirements

#### Hardware
- **CPU**: 8+ cores, 3.5 GHz
- **Memory**: 16+ GB RAM
- **Storage**: 100+ GB SSD
- **Network**: 100 Mbps, redundant connections

#### Software
- **Container Platform**: Docker 20.0+, Kubernetes 1.20+
- **Database**: PostgreSQL 13+ with replication
- **Monitoring**: Prometheus, Grafana
- **Security**: SSL/TLS certificates, WAF

## Installation Methods

### Method 1: Docker Deployment (Recommended)

#### Prerequisites
```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

#### Quick Start
```bash
# Clone the repository
git clone https://github.com/minotaur-team/minotaur.git
cd minotaur

# Start with Docker Compose
docker-compose up -d

# Access the application
open http://localhost:3000
```

#### Docker Compose Configuration
```yaml
# docker-compose.yml
version: '3.8'

services:
  minotaur:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=production
      - DATABASE_URL=sqlite:./data/minotaur.db
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - minotaur
    restart: unless-stopped
```

### Method 2: Native Installation

#### Ubuntu/Debian
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Node.js
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Install build tools
sudo apt-get install -y build-essential python3

# Clone and build
git clone https://github.com/minotaur-team/minotaur.git
cd minotaur
npm install
npm run build

# Start the application
npm start
```

#### CentOS/RHEL
```bash
# Install Node.js
curl -fsSL https://rpm.nodesource.com/setup_18.x | sudo bash -
sudo yum install -y nodejs

# Install build tools
sudo yum groupinstall -y "Development Tools"
sudo yum install -y python3

# Clone and build
git clone https://github.com/minotaur-team/minotaur.git
cd minotaur
npm install
npm run build

# Start the application
npm start
```

#### Windows
```powershell
# Install Node.js (download from nodejs.org)
# Install Git (download from git-scm.com)

# Clone and build
git clone https://github.com/minotaur-team/minotaur.git
cd minotaur
npm install
npm run build

# Start the application
npm start
```

### Method 3: Kubernetes Deployment

#### Kubernetes Manifests
```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: minotaur

---
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: minotaur
  namespace: minotaur
spec:
  replicas: 3
  selector:
    matchLabels:
      app: minotaur
  template:
    metadata:
      labels:
        app: minotaur
    spec:
      containers:
      - name: minotaur
        image: minotaur/minotaur:latest
        ports:
        - containerPort: 3000
        env:
        - name: NODE_ENV
          value: "production"
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: minotaur-secrets
              key: database-url
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 3000
          initialDelaySeconds: 5
          periodSeconds: 5

---
# service.yaml
apiVersion: v1
kind: Service
metadata:
  name: minotaur-service
  namespace: minotaur
spec:
  selector:
    app: minotaur
  ports:
  - protocol: TCP
    port: 80
    targetPort: 3000
  type: ClusterIP

---
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: minotaur-ingress
  namespace: minotaur
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
  - hosts:
    - minotaur.yourdomain.com
    secretName: minotaur-tls
  rules:
  - host: minotaur.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: minotaur-service
            port:
              number: 80
```

#### Deploy to Kubernetes
```bash
# Apply manifests
kubectl apply -f namespace.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f ingress.yaml

# Check deployment status
kubectl get pods -n minotaur
kubectl get services -n minotaur
kubectl get ingress -n minotaur
```

## Configuration

### Environment Variables

```bash
# Application Configuration
NODE_ENV=production                    # Environment: development, staging, production
PORT=3000                             # Server port
HOST=0.0.0.0                         # Server host

# Database Configuration
DATABASE_URL=sqlite:./data/minotaur.db # Database connection string
DATABASE_POOL_MIN=1                   # Minimum database connections
DATABASE_POOL_MAX=10                  # Maximum database connections

# Security Configuration
JWT_SECRET=your-jwt-secret-key        # JWT signing secret
SESSION_SECRET=your-session-secret    # Session encryption secret
CORS_ORIGIN=*                         # CORS allowed origins

# Engine Configuration
ENABLE_RULE_BASED=true               # Enable rule-based engine
ENABLE_PATTERN_BASED=true            # Enable pattern-based engine
ENABLE_LLM=false                     # Enable LLM engine (optional)

# LLM Configuration (if enabled)
OPENAI_API_KEY=your-openai-api-key   # OpenAI API key
OPENAI_API_BASE=https://api.openai.com/v1 # OpenAI API base URL
LLM_MAX_COST_PER_REQUEST=0.10        # Maximum cost per LLM request
LLM_DAILY_LIMIT=10.00                # Daily LLM cost limit
LLM_MONTHLY_LIMIT=100.00             # Monthly LLM cost limit

# Logging Configuration
LOG_LEVEL=info                        # Log level: debug, info, warn, error
LOG_FORMAT=json                       # Log format: json, text
LOG_FILE=./logs/minotaur.log         # Log file path

# Performance Configuration
CACHE_SIZE=10000                      # Cache size (number of entries)
CACHE_TTL=3600                        # Cache TTL in seconds
MAX_CONCURRENT_REQUESTS=100           # Maximum concurrent requests
REQUEST_TIMEOUT=30000                 # Request timeout in milliseconds

# Monitoring Configuration
METRICS_ENABLED=true                  # Enable metrics collection
HEALTH_CHECK_INTERVAL=30000          # Health check interval in milliseconds
```

### Configuration Files

#### Main Configuration
```yaml
# config/production.yaml
app:
  name: "Minotaur Transformation System"
  version: "1.0.0"
  environment: "production"

server:
  host: "0.0.0.0"
  port: 3000
  cors:
    enabled: true
    origins: ["https://yourdomain.com"]
  rateLimit:
    windowMs: 900000  # 15 minutes
    max: 1000         # requests per window

database:
  type: "postgresql"
  host: "localhost"
  port: 5432
  database: "minotaur"
  username: "minotaur_user"
  password: "${DATABASE_PASSWORD}"
  pool:
    min: 2
    max: 20
    acquireTimeoutMillis: 30000
    idleTimeoutMillis: 30000

engines:
  ruleBased:
    enabled: true
    priority: 1000
    cacheSize: 1000
    maxConcurrency: 10
  
  patternBased:
    enabled: true
    priority: 800
    learningRate: 0.1
    minConfidence: 0.7
  
  llmEnhanced:
    enabled: false
    priority: 600
    model: "gpt-4"
    temperature: 0.1
    maxTokens: 2048

logging:
  level: "info"
  format: "json"
  outputs: ["console", "file"]
  file:
    path: "./logs/minotaur.log"
    maxSize: "100MB"
    maxFiles: 10
    compress: true

monitoring:
  metrics:
    enabled: true
    port: 9090
    path: "/metrics"
  
  healthCheck:
    enabled: true
    path: "/health"
    interval: 30000
  
  alerts:
    email:
      enabled: true
      smtp:
        host: "smtp.yourdomain.com"
        port: 587
        secure: false
        auth:
          user: "alerts@yourdomain.com"
          pass: "${SMTP_PASSWORD}"
```

#### Security Configuration
```yaml
# config/security.yaml
authentication:
  methods: ["local", "oauth2"]
  session:
    timeout: 3600      # 1 hour
    maxAge: 86400      # 24 hours
  
  oauth2:
    google:
      clientId: "${GOOGLE_CLIENT_ID}"
      clientSecret: "${GOOGLE_CLIENT_SECRET}"
      callbackURL: "/auth/google/callback"
    
    github:
      clientId: "${GITHUB_CLIENT_ID}"
      clientSecret: "${GITHUB_CLIENT_SECRET}"
      callbackURL: "/auth/github/callback"

authorization:
  model: "rbac"
  roles:
    admin:
      permissions: ["*"]
    developer:
      permissions: 
        - "read:rules"
        - "write:rules"
        - "execute:translate"
        - "read:analytics"
    viewer:
      permissions:
        - "read:rules"
        - "execute:translate"

security:
  encryption:
    algorithm: "aes-256-gcm"
    keyRotation: "90d"
  
  headers:
    contentSecurityPolicy: "default-src 'self'"
    xFrameOptions: "DENY"
    xContentTypeOptions: "nosniff"
    referrerPolicy: "strict-origin-when-cross-origin"
  
  rateLimit:
    api:
      windowMs: 900000  # 15 minutes
      max: 1000         # requests per window
    auth:
      windowMs: 900000  # 15 minutes
      max: 5            # login attempts per window
```

## Environment Setup

### Development Environment

```bash
# Clone repository
git clone https://github.com/minotaur-team/minotaur.git
cd minotaur

# Install dependencies
npm install

# Set up environment variables
cp .env.example .env.development
# Edit .env.development with your settings

# Initialize database
npm run db:migrate
npm run db:seed

# Start development server
npm run dev

# Run tests
npm test
npm run test:watch
npm run test:coverage
```

### Staging Environment

```bash
# Set up staging environment
export NODE_ENV=staging

# Use staging configuration
cp config/staging.yaml.example config/staging.yaml
# Edit staging.yaml with your settings

# Build application
npm run build

# Run database migrations
npm run db:migrate

# Start application
npm start

# Run integration tests
npm run test:integration
npm run test:e2e
```

### Production Environment

```bash
# Set up production environment
export NODE_ENV=production

# Use production configuration
cp config/production.yaml.example config/production.yaml
# Edit production.yaml with your settings

# Build application for production
npm run build:production

# Run database migrations
npm run db:migrate

# Start application with PM2
npm install -g pm2
pm2 start ecosystem.config.js --env production

# Set up log rotation
pm2 install pm2-logrotate
pm2 set pm2-logrotate:max_size 100M
pm2 set pm2-logrotate:retain 30
```

#### PM2 Configuration
```javascript
// ecosystem.config.js
module.exports = {
  apps: [{
    name: 'minotaur',
    script: './dist/server.js',
    instances: 'max',
    exec_mode: 'cluster',
    env: {
      NODE_ENV: 'production',
      PORT: 3000
    },
    error_file: './logs/err.log',
    out_file: './logs/out.log',
    log_file: './logs/combined.log',
    time: true,
    max_memory_restart: '1G',
    node_args: '--max-old-space-size=1024'
  }]
};
```

## Security Configuration

### SSL/TLS Setup

#### Nginx Configuration
```nginx
# /etc/nginx/sites-available/minotaur
server {
    listen 80;
    server_name minotaur.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name minotaur.yourdomain.com;

    ssl_certificate /etc/ssl/certs/minotaur.crt;
    ssl_certificate_key /etc/ssl/private/minotaur.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options DENY always;
    add_header X-Content-Type-Options nosniff always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Proxy to application
    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # Static files
    location /static/ {
        alias /var/www/minotaur/static/;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

### Firewall Configuration

#### UFW (Ubuntu)
```bash
# Enable UFW
sudo ufw enable

# Allow SSH
sudo ufw allow ssh

# Allow HTTP and HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Allow application port (if not behind reverse proxy)
sudo ufw allow 3000/tcp

# Deny all other incoming traffic
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Check status
sudo ufw status verbose
```

#### iptables
```bash
# Flush existing rules
sudo iptables -F

# Set default policies
sudo iptables -P INPUT DROP
sudo iptables -P FORWARD DROP
sudo iptables -P OUTPUT ACCEPT

# Allow loopback
sudo iptables -A INPUT -i lo -j ACCEPT

# Allow established connections
sudo iptables -A INPUT -m conntrack --ctstate ESTABLISHED,RELATED -j ACCEPT

# Allow SSH
sudo iptables -A INPUT -p tcp --dport 22 -j ACCEPT

# Allow HTTP and HTTPS
sudo iptables -A INPUT -p tcp --dport 80 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 443 -j ACCEPT

# Save rules
sudo iptables-save > /etc/iptables/rules.v4
```

### Database Security

#### PostgreSQL Security
```sql
-- Create dedicated user
CREATE USER minotaur_user WITH PASSWORD 'strong_password';

-- Create database
CREATE DATABASE minotaur OWNER minotaur_user;

-- Grant minimal permissions
GRANT CONNECT ON DATABASE minotaur TO minotaur_user;
GRANT USAGE ON SCHEMA public TO minotaur_user;
GRANT CREATE ON SCHEMA public TO minotaur_user;

-- Enable SSL
-- In postgresql.conf:
-- ssl = on
-- ssl_cert_file = 'server.crt'
-- ssl_key_file = 'server.key'

-- Configure authentication
-- In pg_hba.conf:
-- hostssl minotaur minotaur_user 0.0.0.0/0 md5
```

## Monitoring and Logging

### Application Monitoring

#### Health Check Endpoint
```typescript
// Health check implementation
app.get('/health', async (req, res) => {
  const health = {
    status: 'ok',
    timestamp: new Date().toISOString(),
    uptime: process.uptime(),
    version: process.env.npm_package_version,
    checks: {
      database: await checkDatabase(),
      engines: await checkEngines(),
      memory: checkMemory(),
      disk: await checkDisk()
    }
  };

  const isHealthy = Object.values(health.checks).every(check => check.status === 'ok');
  res.status(isHealthy ? 200 : 503).json(health);
});
```

#### Metrics Collection
```typescript
// Prometheus metrics
import { register, Counter, Histogram, Gauge } from 'prom-client';

const httpRequestsTotal = new Counter({
  name: 'http_requests_total',
  help: 'Total number of HTTP requests',
  labelNames: ['method', 'route', 'status']
});

const httpRequestDuration = new Histogram({
  name: 'http_request_duration_seconds',
  help: 'Duration of HTTP requests in seconds',
  labelNames: ['method', 'route']
});

const translationRequestsTotal = new Counter({
  name: 'translation_requests_total',
  help: 'Total number of translation requests',
  labelNames: ['engine', 'source_language', 'target_language', 'status']
});

const activeConnections = new Gauge({
  name: 'active_connections',
  help: 'Number of active connections'
});

// Metrics endpoint
app.get('/metrics', (req, res) => {
  res.set('Content-Type', register.contentType);
  res.end(register.metrics());
});
```

### Log Configuration

#### Structured Logging
```typescript
// Winston logger configuration
import winston from 'winston';

const logger = winston.createLogger({
  level: process.env.LOG_LEVEL || 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.errors({ stack: true }),
    winston.format.json()
  ),
  defaultMeta: {
    service: 'minotaur',
    version: process.env.npm_package_version
  },
  transports: [
    new winston.transports.File({
      filename: './logs/error.log',
      level: 'error',
      maxsize: 100 * 1024 * 1024, // 100MB
      maxFiles: 10
    }),
    new winston.transports.File({
      filename: './logs/combined.log',
      maxsize: 100 * 1024 * 1024, // 100MB
      maxFiles: 10
    })
  ]
});

if (process.env.NODE_ENV !== 'production') {
  logger.add(new winston.transports.Console({
    format: winston.format.simple()
  }));
}
```

#### Log Rotation
```bash
# Logrotate configuration
# /etc/logrotate.d/minotaur
/var/log/minotaur/*.log {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 644 minotaur minotaur
    postrotate
        systemctl reload minotaur
    endscript
}
```

### External Monitoring

#### Prometheus Configuration
```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'minotaur'
    static_configs:
      - targets: ['localhost:3000']
    metrics_path: '/metrics'
    scrape_interval: 30s

rule_files:
  - "minotaur_rules.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093
```

#### Grafana Dashboard
```json
{
  "dashboard": {
    "title": "Minotaur System Dashboard",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total[5m])",
            "legendFormat": "{{method}} {{route}}"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "title": "Translation Success Rate",
        "type": "stat",
        "targets": [
          {
            "expr": "rate(translation_requests_total{status=\"success\"}[5m]) / rate(translation_requests_total[5m])",
            "legendFormat": "Success Rate"
          }
        ]
      }
    ]
  }
}
```

## Backup and Recovery

### Database Backup

#### PostgreSQL Backup
```bash
#!/bin/bash
# backup-database.sh

# Configuration
DB_NAME="minotaur"
DB_USER="minotaur_user"
BACKUP_DIR="/var/backups/minotaur"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/minotaur_$DATE.sql"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create backup
pg_dump -h localhost -U $DB_USER -d $DB_NAME > $BACKUP_FILE

# Compress backup
gzip $BACKUP_FILE

# Remove old backups (keep 30 days)
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

# Upload to cloud storage (optional)
# aws s3 cp $BACKUP_FILE.gz s3://your-backup-bucket/database/
```

#### SQLite Backup
```bash
#!/bin/bash
# backup-sqlite.sh

# Configuration
DB_FILE="./data/minotaur.db"
BACKUP_DIR="/var/backups/minotaur"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/minotaur_$DATE.db"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create backup
sqlite3 $DB_FILE ".backup $BACKUP_FILE"

# Compress backup
gzip $BACKUP_FILE

# Remove old backups (keep 30 days)
find $BACKUP_DIR -name "*.db.gz" -mtime +30 -delete
```

### Application Data Backup

```bash
#!/bin/bash
# backup-application.sh

# Configuration
APP_DIR="/opt/minotaur"
BACKUP_DIR="/var/backups/minotaur"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup rule library
tar -czf $BACKUP_DIR/rules_$DATE.tar.gz $APP_DIR/data/rules/

# Backup patterns
tar -czf $BACKUP_DIR/patterns_$DATE.tar.gz $APP_DIR/data/patterns/

# Backup configuration
tar -czf $BACKUP_DIR/config_$DATE.tar.gz $APP_DIR/config/

# Backup logs (last 7 days)
find $APP_DIR/logs -name "*.log" -mtime -7 | tar -czf $BACKUP_DIR/logs_$DATE.tar.gz -T -

# Remove old backups (keep 30 days)
find $BACKUP_DIR -name "*.tar.gz" -mtime +30 -delete
```

### Automated Backup Schedule

```bash
# Crontab configuration
# crontab -e

# Daily database backup at 2 AM
0 2 * * * /opt/minotaur/scripts/backup-database.sh

# Daily application data backup at 3 AM
0 3 * * * /opt/minotaur/scripts/backup-application.sh

# Weekly full system backup on Sunday at 1 AM
0 1 * * 0 /opt/minotaur/scripts/backup-full-system.sh
```

### Recovery Procedures

#### Database Recovery
```bash
# PostgreSQL recovery
gunzip minotaur_20240101_020000.sql.gz
psql -h localhost -U minotaur_user -d minotaur < minotaur_20240101_020000.sql

# SQLite recovery
gunzip minotaur_20240101_020000.db.gz
cp minotaur_20240101_020000.db ./data/minotaur.db
```

#### Application Data Recovery
```bash
# Restore rule library
tar -xzf rules_20240101_030000.tar.gz -C /opt/minotaur/

# Restore patterns
tar -xzf patterns_20240101_030000.tar.gz -C /opt/minotaur/

# Restore configuration
tar -xzf config_20240101_030000.tar.gz -C /opt/minotaur/

# Restart application
systemctl restart minotaur
```

## Maintenance Procedures

### Regular Maintenance Tasks

#### Daily Tasks
```bash
#!/bin/bash
# daily-maintenance.sh

# Check disk space
df -h | grep -E '(8[0-9]|9[0-9])%' && echo "WARNING: Disk space low"

# Check log file sizes
find /var/log/minotaur -name "*.log" -size +100M -exec echo "Large log file: {}" \;

# Check application health
curl -f http://localhost:3000/health || echo "ERROR: Health check failed"

# Update system packages (if auto-update is disabled)
# apt update && apt list --upgradable
```

#### Weekly Tasks
```bash
#!/bin/bash
# weekly-maintenance.sh

# Analyze database performance
psql -h localhost -U minotaur_user -d minotaur -c "ANALYZE;"

# Clean up old log files
find /var/log/minotaur -name "*.log.*" -mtime +7 -delete

# Update rule library statistics
curl -X POST http://localhost:3000/api/v1/maintenance/update-stats

# Check for security updates
apt list --upgradable | grep -i security
```

#### Monthly Tasks
```bash
#!/bin/bash
# monthly-maintenance.sh

# Database maintenance
psql -h localhost -U minotaur_user -d minotaur -c "VACUUM ANALYZE;"

# Clean up old backups
find /var/backups/minotaur -name "*.gz" -mtime +90 -delete

# Review and rotate SSL certificates
openssl x509 -in /etc/ssl/certs/minotaur.crt -noout -dates

# Performance analysis
curl -X POST http://localhost:3000/api/v1/maintenance/performance-report
```

### Update Procedures

#### Application Updates
```bash
#!/bin/bash
# update-application.sh

# Backup current version
cp -r /opt/minotaur /opt/minotaur.backup.$(date +%Y%m%d)

# Stop application
systemctl stop minotaur

# Pull latest code
cd /opt/minotaur
git fetch origin
git checkout tags/v1.1.0  # or latest stable version

# Install dependencies
npm ci --production

# Run database migrations
npm run db:migrate

# Build application
npm run build

# Start application
systemctl start minotaur

# Verify update
curl -f http://localhost:3000/health

# Clean up old backup (if successful)
# rm -rf /opt/minotaur.backup.$(date +%Y%m%d)
```

#### Security Updates
```bash
#!/bin/bash
# security-updates.sh

# Update system packages
apt update && apt upgrade -y

# Update Node.js dependencies
cd /opt/minotaur
npm audit fix

# Update SSL certificates (if using Let's Encrypt)
certbot renew

# Restart services
systemctl restart nginx
systemctl restart minotaur

# Verify security
nmap -sS -O localhost
```

## Troubleshooting

### Common Issues

#### Application Won't Start
```bash
# Check logs
journalctl -u minotaur -f

# Check configuration
npm run config:validate

# Check dependencies
npm ls

# Check ports
netstat -tlnp | grep :3000

# Check permissions
ls -la /opt/minotaur
```

#### Database Connection Issues
```bash
# Test database connection
psql -h localhost -U minotaur_user -d minotaur -c "SELECT 1;"

# Check database status
systemctl status postgresql

# Check connection limits
psql -h localhost -U postgres -c "SELECT * FROM pg_stat_activity;"

# Check configuration
cat /opt/minotaur/config/production.yaml | grep -A 10 database
```

#### Performance Issues
```bash
# Check system resources
top
htop
iotop

# Check application metrics
curl http://localhost:3000/metrics

# Check database performance
psql -h localhost -U minotaur_user -d minotaur -c "SELECT * FROM pg_stat_statements ORDER BY total_time DESC LIMIT 10;"

# Check log files for errors
tail -f /var/log/minotaur/error.log
```

#### Memory Issues
```bash
# Check memory usage
free -h
ps aux --sort=-%mem | head

# Check for memory leaks
valgrind --tool=memcheck --leak-check=full node dist/server.js

# Adjust Node.js memory limits
export NODE_OPTIONS="--max-old-space-size=2048"

# Monitor garbage collection
node --trace-gc dist/server.js
```

### Diagnostic Tools

#### Health Check Script
```bash
#!/bin/bash
# health-check.sh

echo "=== Minotaur System Health Check ==="

# Check application status
echo "Application Status:"
systemctl is-active minotaur

# Check database status
echo "Database Status:"
systemctl is-active postgresql

# Check disk space
echo "Disk Space:"
df -h | grep -E '/$|/var|/opt'

# Check memory usage
echo "Memory Usage:"
free -h

# Check network connectivity
echo "Network Connectivity:"
curl -s -o /dev/null -w "%{http_code}" http://localhost:3000/health

# Check log errors
echo "Recent Errors:"
tail -n 50 /var/log/minotaur/error.log | grep ERROR | tail -n 5

echo "=== Health Check Complete ==="
```

#### Performance Monitoring Script
```bash
#!/bin/bash
# performance-monitor.sh

# Monitor for 60 seconds
DURATION=60
INTERVAL=5

echo "=== Performance Monitoring (${DURATION}s) ==="

for i in $(seq 1 $((DURATION/INTERVAL))); do
    echo "Sample $i:"
    
    # CPU usage
    CPU=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    echo "  CPU: ${CPU}%"
    
    # Memory usage
    MEM=$(free | grep Mem | awk '{printf "%.1f", $3/$2 * 100.0}')
    echo "  Memory: ${MEM}%"
    
    # Response time
    RESPONSE=$(curl -o /dev/null -s -w "%{time_total}" http://localhost:3000/health)
    echo "  Response Time: ${RESPONSE}s"
    
    # Active connections
    CONNECTIONS=$(netstat -an | grep :3000 | grep ESTABLISHED | wc -l)
    echo "  Active Connections: $CONNECTIONS"
    
    sleep $INTERVAL
done

echo "=== Monitoring Complete ==="
```

## Performance Optimization

### Application Optimization

#### Node.js Optimization
```javascript
// Performance configuration
const cluster = require('cluster');
const numCPUs = require('os').cpus().length;

if (cluster.isMaster) {
  // Fork workers
  for (let i = 0; i < numCPUs; i++) {
    cluster.fork();
  }
  
  cluster.on('exit', (worker, code, signal) => {
    console.log(`Worker ${worker.process.pid} died`);
    cluster.fork();
  });
} else {
  // Worker process
  require('./app.js');
}

// Memory optimization
process.on('warning', (warning) => {
  console.warn(warning.name);
  console.warn(warning.message);
  console.warn(warning.stack);
});

// Garbage collection optimization
if (process.env.NODE_ENV === 'production') {
  setInterval(() => {
    if (global.gc) {
      global.gc();
    }
  }, 30000);
}
```

#### Database Optimization
```sql
-- PostgreSQL optimization
-- postgresql.conf settings

-- Memory settings
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 4MB
maintenance_work_mem = 64MB

-- Connection settings
max_connections = 100
shared_preload_libraries = 'pg_stat_statements'

-- Performance settings
random_page_cost = 1.1
effective_io_concurrency = 200
max_worker_processes = 4
max_parallel_workers_per_gather = 2

-- Create indexes for common queries
CREATE INDEX CONCURRENTLY idx_rules_source_target 
ON transformation_rules(source_language, target_language);

CREATE INDEX CONCURRENTLY idx_rules_enabled 
ON transformation_rules(enabled) WHERE enabled = true;

CREATE INDEX CONCURRENTLY idx_rules_tags 
ON transformation_rules USING GIN(tags);

-- Analyze tables
ANALYZE transformation_rules;
ANALYZE translation_history;
```

#### Caching Strategy
```typescript
// Redis caching implementation
import Redis from 'ioredis';

const redis = new Redis({
  host: process.env.REDIS_HOST || 'localhost',
  port: parseInt(process.env.REDIS_PORT || '6379'),
  retryDelayOnFailover: 100,
  maxRetriesPerRequest: 3
});

class CacheService {
  async get(key: string): Promise<any> {
    try {
      const value = await redis.get(key);
      return value ? JSON.parse(value) : null;
    } catch (error) {
      console.error('Cache get error:', error);
      return null;
    }
  }
  
  async set(key: string, value: any, ttl: number = 3600): Promise<void> {
    try {
      await redis.setex(key, ttl, JSON.stringify(value));
    } catch (error) {
      console.error('Cache set error:', error);
    }
  }
  
  async del(key: string): Promise<void> {
    try {
      await redis.del(key);
    } catch (error) {
      console.error('Cache delete error:', error);
    }
  }
}

// Cache middleware
const cacheMiddleware = (ttl: number = 3600) => {
  return async (req: Request, res: Response, next: NextFunction) => {
    const key = `cache:${req.method}:${req.originalUrl}`;
    const cached = await cacheService.get(key);
    
    if (cached) {
      return res.json(cached);
    }
    
    const originalSend = res.json;
    res.json = function(data) {
      cacheService.set(key, data, ttl);
      return originalSend.call(this, data);
    };
    
    next();
  };
};
```

## Scaling Guidelines

### Horizontal Scaling

#### Load Balancer Configuration
```nginx
# nginx load balancer
upstream minotaur_backend {
    least_conn;
    server 10.0.1.10:3000 weight=1 max_fails=3 fail_timeout=30s;
    server 10.0.1.11:3000 weight=1 max_fails=3 fail_timeout=30s;
    server 10.0.1.12:3000 weight=1 max_fails=3 fail_timeout=30s;
}

server {
    listen 80;
    server_name minotaur.yourdomain.com;
    
    location / {
        proxy_pass http://minotaur_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Health check
        proxy_next_upstream error timeout invalid_header http_500 http_502 http_503 http_504;
        proxy_connect_timeout 5s;
        proxy_send_timeout 10s;
        proxy_read_timeout 10s;
    }
    
    # Health check endpoint
    location /health {
        access_log off;
        proxy_pass http://minotaur_backend;
    }
}
```

#### Auto Scaling Configuration
```yaml
# Kubernetes HPA
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: minotaur-hpa
  namespace: minotaur
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: minotaur
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
```

### Vertical Scaling

#### Resource Monitoring
```bash
#!/bin/bash
# resource-monitor.sh

# Monitor resource usage and recommend scaling
CPU_THRESHOLD=80
MEM_THRESHOLD=80

while true; do
    # Get current usage
    CPU_USAGE=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1 | cut -d' ' -f1)
    MEM_USAGE=$(free | grep Mem | awk '{printf "%.0f", $3/$2 * 100.0}')
    
    # Check thresholds
    if (( $(echo "$CPU_USAGE > $CPU_THRESHOLD" | bc -l) )); then
        echo "WARNING: CPU usage is ${CPU_USAGE}% (threshold: ${CPU_THRESHOLD}%)"
        echo "Consider scaling up CPU resources"
    fi
    
    if (( $(echo "$MEM_USAGE > $MEM_THRESHOLD" | bc -l) )); then
        echo "WARNING: Memory usage is ${MEM_USAGE}% (threshold: ${MEM_THRESHOLD}%)"
        echo "Consider scaling up memory resources"
    fi
    
    sleep 60
done
```

#### Database Scaling
```sql
-- Read replica setup
-- On master database
CREATE USER replication_user REPLICATION LOGIN CONNECTION LIMIT 5 ENCRYPTED PASSWORD 'strong_password';

-- On replica database
-- pg_basebackup -h master_host -D /var/lib/postgresql/12/main -U replication_user -v -P -W

-- Connection pooling with PgBouncer
-- /etc/pgbouncer/pgbouncer.ini
[databases]
minotaur = host=localhost port=5432 dbname=minotaur

[pgbouncer]
listen_port = 6432
listen_addr = *
auth_type = md5
auth_file = /etc/pgbouncer/userlist.txt
pool_mode = transaction
max_client_conn = 1000
default_pool_size = 25
reserve_pool_size = 5
```

---

This deployment and maintenance guide provides comprehensive instructions for successfully deploying, configuring, and maintaining the Transformation Rule Creation and Management System in any environment. Follow these guidelines to ensure optimal performance, security, and reliability of your deployment.

