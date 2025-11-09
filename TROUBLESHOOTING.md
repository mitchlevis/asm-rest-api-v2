# Troubleshooting Guide

## Production Deployment Database Connection Issues

### Symptom
When deploying to production via Cloudflare Dashboard automatic deployments (triggered by branch merges), the API hangs and times out when trying to connect to the database. However, manual deployments using `wrangler deploy --env production` work correctly.

### Root Cause Analysis

After checking the Cloudflare account, I found:

1. **✅ Secrets are configured**: Both preview and production have `DATABASE_PASSWORD` properly set
2. **🔍 Key difference identified**: 
   - Preview uses an **IP address** (`184.162.149.155`) - no DNS resolution needed
   - Production uses a **hostname** (`mndSQL80.everleap.com`) - requires DNS resolution
3. **⚠️ Potential issue**: The DNS resolution code was using `fetch` without a timeout, which could hang indefinitely if there are network issues during automatic deployments

The most likely cause is that DNS resolution is hanging during automatic deployments for production (which uses a hostname), while preview works fine because it uses a direct IP address.

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

#### Step 3: If Issues Persist
If DNS resolution is still timing out, consider:
1. **Pre-resolve the hostname**: Add the resolved IP address to your `wrangler.jsonc` for production (like preview does)
2. **Check network connectivity**: Verify that Cloudflare Workers can reach `cloudflare-dns.com` for DNS queries
3. **Check KV binding**: Ensure the KV namespace is properly bound (though the code handles KV failures gracefully)

### Additional Notes

- **Environment-specific secrets**: Secrets in Cloudflare Workers are environment-specific. Setting a secret for `development` or `preview` does NOT automatically set it for `production`.
- **Manual vs Automatic deployments**: When deploying manually with `wrangler`, it uses secrets from your local Cloudflare account configuration. Automatic deployments from the dashboard may use different secret configurations.
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

