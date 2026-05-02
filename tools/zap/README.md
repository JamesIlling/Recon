# OWASP ZAP MCP Server - Local Setup

## Prerequisites
- Docker Desktop running
- openssl available (Git Bash or WSL on Windows)

## Setup

1. Generate API keys:
   ```bash
   openssl rand -hex 32   # use output for MCP_API_KEY
   openssl rand -hex 32   # use output for ZAP_API_KEY
   ```

2. Copy and configure the env file:
   ```bash
   cp .env.example .env
   # Edit .env and replace REPLACE_WITH_GENERATED_KEY with your generated keys
   ```

3. Start the stack:
   ```bash
   docker compose up -d
   ```

4. Set the environment variable Kiro uses for the API key:
   ```bash
   # Windows (PowerShell) - set for current session
   $env:MCP_ZAP_API_KEY = "your-mcp-api-key-from-.env"

   # Or set permanently via System Properties > Environment Variables
   ```

5. Enable the MCP server in Kiro:
   - Open ~/.kiro/settings/mcp.json
   - Set "disabled": false on the owasp-zap entry

6. Reconnect MCP servers in Kiro (MCP Server view in the feature panel)

## Stop

```bash
docker compose down
```

## Reports

ZAP reports are saved to the `./zap-workplace` folder.
