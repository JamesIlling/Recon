---
inclusion: always
---

# Deployment Requirements

This project MUST be deployable to both **AWS** and **Azure** without code changes. All deployment configuration is cloud-agnostic by default, with cloud-specific IaC in separate directories. Deployments follow the **AWS Well-Architected Framework** (6 pillars) and the **Azure Well-Architected Framework** (5 pillars), which share the same core concerns.

---

## Well-Architected Pillars

Both frameworks share these pillars. Every architectural and deployment decision MUST be evaluated against all of them:

| Pillar | AWS | Azure | Core concern |
|---|---|---|---|
| Operational Excellence | Yes | Yes | Automate operations, safe deployments, runbooks |
| Security | Yes | Yes | Identity, least privilege, encryption, threat protection |
| Reliability | Yes | Yes | Fault tolerance, auto-recovery, multi-AZ/zone |
| Performance Efficiency | Yes | Yes | Right-sizing, autoscaling, caching |
| Cost Optimization | Yes | Yes | Right-sizing, reserved capacity, waste elimination |
| Sustainability | AWS only | - | Minimise environmental impact, efficient resource use |

---

## Deployable Components

| Component | AWS Service | Azure Service |
|---|---|---|
| ASP.NET Core Web API | ECS Fargate (container) | Azure Container Apps |
| React frontend | S3 + CloudFront | Azure Static Web Apps |
| SQL Server + GIS | RDS for SQL Server | Azure SQL Database |
| Container registry | Amazon ECR | Azure Container Registry (ACR) |
| Secrets management | AWS Secrets Manager | Azure Key Vault |
| Identity / auth | AWS Cognito or IAM roles | Azure Entra ID + Managed Identity |
| Observability (OTel OTLP) | AWS X-Ray + CloudWatch | Azure Monitor + Application Insights |
| Load balancer | Application Load Balancer (ALB) | Azure Application Gateway |
| DNS | Route 53 | Azure DNS |
| IaC | Terraform (preferred) or AWS CDK | Terraform (preferred) or Bicep |

---

## Pillar 1 - Operational Excellence

- All infrastructure MUST be defined as Infrastructure as Code (IaC) - no manual portal configuration.
- IaC lives in `infra/aws/` and `infra/azure/` respectively.
- Use Terraform as the preferred IaC tool for cloud-agnostic parity.
- All deployments MUST go through automated CI/CD pipelines - no manual deploys to production.
- Use blue/green or rolling deployment strategies - no in-place deployments that cause downtime.
- Every deployment MUST pass the pre-deployment checklist (see `pentest.md`) before reaching production.
- Maintain runbooks in `docs/runbooks/` for all operational procedures (restart, rollback, scaling).
- Use small, incremental deployments - avoid large batches of changes in a single release.

## Pillar 2 - Security

- All secrets MUST be stored in AWS Secrets Manager or Azure Key Vault - never in container images or IaC source.
- Use managed identities (Azure) or IAM roles (AWS) for service-to-service auth - no long-lived credentials.
- Apply least privilege to all IAM roles, managed identities, and database accounts.
- All traffic MUST use TLS 1.2+ - enforce HTTPS at the load balancer and CDN layer.
- Enable WAF (AWS WAF or Azure WAF) in front of the API for OWASP Top 10 protection.
- Enable DDoS protection (AWS Shield Standard or Azure DDoS Protection).
- Database MUST NOT be publicly accessible - place in a private subnet or VNet.
- Container images MUST be scanned for vulnerabilities before deployment (ECR scanning or ACR Defender).
- Run the full pre-deployment pen test (see `pentest.md`) before every production release.

## Pillar 3 - Reliability

- Deploy the API across at least 2 availability zones (AWS) or availability zones (Azure).
- Configure health checks on the load balancer - unhealthy instances are removed automatically.
- API MUST expose `/health/live` and `/health/ready` endpoints (configured via Aspire ServiceDefaults).
- Configure auto-scaling - scale out under load, scale in during quiet periods.
- Database MUST have automated backups enabled with a minimum 7-day retention.
- Enable multi-AZ (AWS RDS) or zone-redundant (Azure SQL) for the database.
- Define and test a rollback procedure for every deployment - document it in the runbook.
- Set circuit breakers and retry policies on all outbound HTTP calls (via Aspire resilience defaults).

## Pillar 4 - Performance Efficiency

- Use containerised deployment (ECS Fargate or Azure Container Apps) for the API.
- Right-size compute: start small and scale based on observed metrics, not assumptions.
- Enable CDN (CloudFront or Azure CDN) for the React frontend - serve static assets from edge.
- Configure connection pooling for the SQL Server database.
- Add spatial indexes on all GEOGRAPHY and GEOMETRY columns - required for GIS query performance.
- Set autoscaling rules based on CPU utilisation (target 60-70%) and HTTP request queue depth.
- Use async/await throughout the API - no synchronous blocking I/O.

## Pillar 5 - Cost Optimization

- Use Fargate Spot (AWS) or spot instances for non-production environments.
- Tag all cloud resources with: Environment, Project, Owner, CostCentre.
- Set budget alerts in AWS Cost Explorer or Azure Cost Management at 80% and 100% of monthly budget.
- Use reserved instances or savings plans for production database and compute once usage is stable.
- Shut down non-production environments outside business hours using scheduled scaling rules.
- Review and right-size resources monthly - remove unused resources immediately.

## Pillar 6 - Sustainability (AWS)

- Prefer managed services over self-managed VMs - managed services are more resource-efficient.
- Use Graviton (ARM) instances on AWS where the .NET workload supports it.
- Deploy to AWS regions with higher renewable energy usage where latency requirements allow.
- Enable S3 Intelligent-Tiering for any object storage used for reports or assets.

---

## Container Strategy

The API and frontend are deployed as containers for cloud portability.

### API Container (src/Api)
- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (minimal, hardened)
- Build image: `mcr.microsoft.com/dotnet/sdk:10.0-alpine`
- Run as non-root user - set `USER app` in Dockerfile
- No secrets in the image - all config via environment variables injected at runtime
- Health check endpoint: `/health/live`

### Frontend (src/client)
- Build with Vite: `npm run build` produces static files in `dist/`
- Deploy to S3 + CloudFront (AWS) or Azure Static Web Apps (Azure)
- No API URLs hardcoded - inject via environment variables at build time

---

## IaC Structure

```
infra/
  aws/
    networking/     # VPC, subnets, security groups
    compute/        # ECS cluster, Fargate task definitions
    database/       # RDS SQL Server
    cdn/            # CloudFront + S3
    iam/            # Roles and policies
  azure/
    networking/     # VNet, subnets, NSGs
    compute/        # Container Apps environment
    database/       # Azure SQL
    cdn/            # Static Web Apps or CDN
    identity/       # Managed identities, Key Vault
  shared/           # Shared Terraform modules (cloud-agnostic)
```

---

## CI/CD Pipeline Stages

Every pipeline MUST include these stages in order:

1. Build - `dotnet build` + `npm run build`, fail on warnings
2. Test - `dotnet test` + `npm run test -- --run`
3. SAST - `semgrep --config=p/owasp-top-ten`
4. SCA - `dotnet list package --vulnerable` + `npm audit`
5. Container build and scan - build image, scan with ECR or ACR vulnerability scanner
6. Deploy to staging - deploy via IaC, run smoke tests
7. Pen test - run automated ZAP scan against staging (see `pentest.md`)
8. Deploy to production - blue/green or rolling, gated on all previous stages passing

---

## Deployment Rules

- NEVER deploy directly to production - all changes go through the pipeline.
- NEVER store secrets in IaC source files - use Secrets Manager or Key Vault references.
- NEVER use the same database for staging and production.
- All IaC changes MUST be reviewed via PR before apply runs.
- Production deployments MUST be approved by a named owner before the pipeline proceeds.
- A pen test report MUST exist in `docs/security/` before every production deployment.

