# FreelanceAI API Testing Guide

## Overview
FreelanceAI is a smart AI request routing service that intelligently routes requests to different AI providers (Groq, Ollama, etc.) based on availability, cost optimisation, and rate limiting.

## API Endpoints

### Base URL
- Development: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

## 1. Health Check Endpoints

### Simple Health Check
```http
GET /health
```

**Expected Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-06-25T20:00:00Z"
}
```

### Detailed Health Check
```http
POST /api/ai/health
```

**Expected Response:**
```json
{
  "status": "Healthy",
  "healthyProviders": 2,
  "totalProviders": 2,
  "timestamp": "2025-06-25T20:00:00Z"
}
```

## 2. Provider Status

### Get Provider Status
```http
GET /api/ai/status
```

**Expected Response:**
```json
[
  {
    "name": "Groq",
    "isHealthy": true,
    "requestsToday": 5,
    "costToday": 0.25,
    "remainingRequests": 95
  },
  {
    "name": "Ollama",
    "isHealthy": true,
    "requestsToday": 0,
    "costToday": 0.0,
    "remainingRequests": 2147483647
  }
]
```

## 3. Cost Tracking

### Get Today's Spend
```http
GET /api/ai/spend
```

**Expected Response:**
```json
2.45
```

## 4. AI Generation (Main Endpoint)

### Basic Text Generation
```http
POST /api/ai/generate
Content-Type: application/json

{
  "prompt": "Explain quantum computing in simple terms"
}
```

**Expected Success Response:**
```json
{
  "success": true,
  "content": "Quantum computing is a revolutionary technology that...",
  "provider": "Groq",
  "cost": 0.05,
  "duration": 1250.5
}
```

### Advanced Generation with Parameters
```http
POST /api/ai/generate
Content-Type: application/json

{
  "prompt": "Write a Python function to sort a list",
  "maxTokens": 500,
  "temperature": 0.3,
  "model": "llama-3.3-70b-versatile",
  "stopSequences": ["```", "END"]
}
```

### Expected Failure Response (All Providers Down)
```json
{
  "success": false,
  "error": "All AI providers exhausted or unavailable",
  "failedProviders": ["Groq", "Ollama"],
  "totalAttemptedCost": 0.0,
  "duration": 5000.0
}
```

## 5. Response History

### Get Response History
```http
GET /api/ai/history
```

**Expected Response:**
```json
{
  "lastUpdated": "2025-06-25T20:00:00Z",
  "responses": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "timestamp": "2025-06-25T19:55:00Z",
      "prompt": "Explain quantum computing",
      "maxTokens": 1000,
      "temperature": 0.7,
      "model": "default",
      "success": true,
      "provider": "Groq",
      "content": "Quantum computing is...",
      "error": null,
      "cost": 0.05,
      "duration": 1250.5
    }
  ],
  "totalRequests": 1,
  "totalCost": 0.05
}
```

### Force File Rollover
```http
POST /api/ai/rollover
```

**Expected Response:**
```json
{
  "message": "File rollover completed successfully"
}
```

## Testing Scenarios for Swagger UI

### 1. **Basic Functionality Test**
Test the basic AI generation capability:

**Endpoint:** `POST /api/ai/generate`
**Body:**
```json
{
  "prompt": "Hello, world! How are you today?"
}
```
**Expected:** Should return a successful response from available provider.

### 2. **Parameter Validation Tests**

**Empty Prompt Test:**
```json
{
  "prompt": ""
}
```
**Expected:** `400 Bad Request` - "Prompt is required"

**Custom Parameters Test:**
```json
{
  "prompt": "Count from 1 to 5",
  "maxTokens": 100,
  "temperature": 0.1,
  "model": "llama-3.3-70b-versatile"
}
```
**Expected:** Should respect the parameters and return a concise response.

### 3. **Provider Routing Tests**

**Long Complex Prompt (Tests Fallback):**
```json
{
  "prompt": "Write a detailed 2000-word essay about the history of artificial intelligence, including major milestones, key researchers, and future implications. Cover everything from Alan Turing to modern deep learning.",
  "maxTokens": 4000,
  "temperature": 0.8
}
```
**Expected:** Should route to appropriate provider based on availability and limits.

### 4. **Rate Limiting Tests**

To test rate limiting, make multiple rapid requests:
```json
{
  "prompt": "Quick test number 1"
}
```
```json
{
  "prompt": "Quick test number 2"
}
```
*(Continue until rate limit is hit)*

**Expected:** Should eventually hit rate limits and show provider switching.

### 5. **Error Handling Tests**

**Invalid JSON Test:**
Send malformed JSON to test error handling.

**Large Request Test:**
```json
{
  "prompt": "Very large prompt that exceeds reasonable limits...",
  "maxTokens": 100000,
  "temperature": 2.0
}
```

## Testing with cURL

### Basic Generation Test
```bash
curl -X POST "http://localhost:5000/api/ai/generate" \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "What is the capital of France?",
    "maxTokens": 100,
    "temperature": 0.3
  }'
```

### Health Check Test
```bash
curl -X GET "http://localhost:5000/health"
```

### Provider Status Test
```bash
curl -X GET "http://localhost:5000/api/ai/status"
```

### Cost Tracking Test
```bash
curl -X GET "http://localhost:5000/api/ai/spend"
```

## Configuration Testing

### Test Different Provider Configurations

1. **Test with Groq Enabled:**
   - Ensure `"Groq.Enabled": true` in appsettings.json
   - Verify requests route to Groq first

2. **Test with Ollama Enabled:**
   - Set `"Ollama.Enabled": true`
   - Ensure Ollama is running locally
   - Test local model responses

3. **Test Rate Limiting:**
   - Lower the `RequestLimit` in configuration
   - Test that limits are enforced

4. **Test Cost Limiting:**
   - Lower the `DailyBudget`
   - Test that cost limits prevent requests

## Monitoring and Logging

### Check Logs
The application provides detailed logging. Look for:
- Provider routing decisions
- Rate limit hits
- Cost calculations
- Error conditions

### File Logging
If `JsonFileServiceOptions.Enabled` is true, check:
- `freelance-ai-responses.json` for request history
- Backup files in the configured directory

## Performance Testing

### Load Testing Example
```bash
# Using Apache Bench
ab -n 100 -c 10 -p data.json -T application/json http://localhost:5000/api/ai/generate

# Where data.json contains:
# {"prompt":"Hello world"}
```

### Response Time Expectations
- Simple prompts: < 2 seconds
- Complex prompts: < 10 seconds
- Provider health checks: < 1 second

## Troubleshooting Common Issues

### 1. All Providers Returning Errors
- Check API keys in configuration
- Verify provider URLs are accessible
- Check rate limits haven't been exceeded

### 2. High Response Times
- Check network connectivity to AI providers
- Monitor provider health status
- Review rate limiting settings

### 3. Cost Tracking Issues
- Verify usage tracking is enabled
- Check file permissions for JSON logging
- Review cost calculation logic in logs

### 4. Configuration Issues
- Validate JSON configuration syntax
- Check that all required sections are present
- Verify environment-specific settings

## Integration Testing Checklist

- [ ] Basic health check responds correctly
- [ ] Provider status shows all configured providers
- [ ] Simple text generation works
- [ ] Complex generation with parameters works
- [ ] Rate limiting triggers correctly
- [ ] Cost tracking updates accurately
- [ ] Error responses are properly formatted
- [ ] Logging captures all important events
- [ ] File rollover works when enabled
- [ ] Response history is tracked correctly

## Security Testing

- [ ] API doesn't expose sensitive configuration
- [ ] Error messages don't leak internal details
- [ ] File operations are secure
- [ ] Rate limiting prevents abuse
- [ ] Input validation prevents injection attacks
