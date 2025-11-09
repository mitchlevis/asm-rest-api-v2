# Troubleshooting Guide

## Production Deployment Database Connection Issues

### Symptom
When deploying to production via Cloudflare Dashboard automatic deployments (triggered by branch merges), the API hangs and times out when trying to connect to the database. However, manual deployments using `wrangler deploy --env production` work correctly.

### Root Cause Analysis

After checking the Cloudflare account and analyzing production logs, I found:

1. **✅ Secrets are configured**: Both preview and production have `DATABASE_PASSWORD` properly set
2. **✅ DNS resolution works**: Production successfully resolves `mndSQL80.everleap.com` to `199.233.255.151`
3. **❌ TCP connection fails**: The error "Could not connect (sequence)" indicates the TCP handshake is failing
4. **🔍 Key difference**:
   - Preview uses an **IP address** (`184.162.149.155`) - works fine
   - Production uses a **hostname** (`mndSQL80.everleap.com`) - DNS works but TCP connection fails

**Actual Root Cause**: The database server is likely blocking TCP connections from Cloudflare Workers IP ranges during automatic deployments. This could be due to:
- IP allowlisting/firewall rules on the database server
- Different network routing between manual and automatic deployments
- Database server not allowing connections from Cloudflare's network

### Solution

The code has been updated with:
1. **DNS timeout protection**: Added a 10-second timeout to DNS resolution to prevent hanging
2. **Better error handling**: DNS resolution errors now fail fast with clear error messages
3. **Enhanced logging**: Added diagnostic logging to identify where failures occur

#### Step 1: Deploy the Updated Code
The updated code includes:
- Timeout protection for DNS resolution (10 seconds)
- Better error messages if DNS resolution fails
- Enhanced logging to diagnose issues

#### Step 2: Monitor the Logs
After deploying, check the Cloudflare Worker logs. You should see:
- "Environment check:" showing all environment variables
- DNS resolution logs if using a hostname
- Clear error messages if DNS resolution times out or fails

#### Step 3: Fix Network/Firewall Issues
The error "Could not connect (sequence)" indicates a TCP connection failure. This is likely a firewall/IP allowlisting issue:

1. **Check Database Server Firewall Rules**:
   - Verify that your database server allows connections from Cloudflare Workers IP ranges
   - Cloudflare publishes their IP ranges: https://www.cloudflare.com/ips/
   - Add Cloudflare's IP ranges to your database server's firewall allowlist

2. **Enable Smart Placement** (already enabled in `wrangler.jsonc`):
   - Smart Placement routes workers closer to resources they connect to
   - This may help with connection routing issues
   - The config now has `"placement": { "mode": "smart" }` enabled

3. **Alternative: Use IP Address Instead of Hostname**:
   - If firewall rules can't be updated, consider using the resolved IP (`199.233.255.151`) directly in `wrangler.jsonc`
   - This matches how preview is configured and avoids DNS resolution entirely
   - Note: IP addresses can change, so monitor DNS records

4. **Check Database Server Logs**:
   - Review your database server logs to see if connection attempts are being blocked
   - Look for firewall/security logs showing denied connections from Cloudflare IPs

### Why Manual Deployments Work But Automatic Don't

**The Short Answer**: There shouldn't be a difference, but there are several possible explanations:

1. **Worker Instance State**:
   - Manual deployments might be tested immediately when the worker is "warm" or fully initialized
   - Automatic deployments might receive traffic before the worker instance is fully ready
   - However, this doesn't explain TCP connection failures

2. **Network Routing Differences**:
   - Cloudflare might route automatic deployments to different worker instances in different regions
   - Different regions might have different network paths to your database
   - This could explain why TCP connections fail (different source IPs)

3. **Configuration Application Timing**:
   - There might be a race condition where environment variables/secrets aren't fully applied when automatic deployments receive their first request
   - However, logs show all env vars are present, so this is less likely

4. **Worker Instance Isolation**:
   - Manual deployments might reuse existing worker instances
   - Automatic deployments might create new worker instances that haven't established network routes yet
   - Smart Placement (now enabled) should help with this

5. **Most Likely**: **Network/Firewall Issue**:
   - The database server might have IP allowlisting that works for manual deployments but blocks automatic ones
   - This could be due to different source IPs or network paths
   - The solution is to ensure Cloudflare's IP ranges are allowed in your database firewall

### Additional Notes

- **Environment-specific secrets**: Secrets in Cloudflare Workers are environment-specific. Setting a secret for `development` or `preview` does NOT automatically set it for `production`.
- **Smart Placement**: Now enabled to route workers closer to your database, which may help with connection routing issues.
- **Error Detection**: The code now includes validation that will immediately fail with a clear error message if `DATABASE_PASSWORD` is missing, rather than hanging indefinitely.

### Verification
After fixing, you should see in the logs:
- "Database adapter initialized with environment"
- "Environment check:" with all required variables showing `hasDATABASE_PASSWORD: true`
- "Connecting to MSSQL server: [host] -> [ip]:[port], database: [name]"
- "Database connection established successfully"

If you see "Missing required database environment variables: DATABASE_PASSWORD", the secret is not properly configured.

### Node.js Version Note
The Node.js version difference (local v22.14.0 vs build v22.16.0) is **unlikely** to be the cause, as:
1. These are patch versions (compatible)
2. Preview automatic deployments work fine (using the same build environment)
3. Manual deployments work fine for both environments

However, the `package.json` now specifies `engines.node` to pin the version. Cloudflare Workers may not strictly enforce this, but it documents the intended version.

