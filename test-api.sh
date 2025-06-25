#!/bin/bash

# FreelanceAI API Quick Test Script
# Make sure the API is running on localhost:5000 before running this script

BASE_URL="http://localhost:5000"
echo "Testing FreelanceAI API at $BASE_URL"
echo "======================================"

# Function to check if API is running
check_api() {
    echo -n "🔍 Checking if API is running... "
    if curl -s "$BASE_URL/health" > /dev/null 2>&1; then
        echo "✅ API is running"
        return 0
    else
        echo "❌ API is not responding"
        echo "Please start the API with: dotnet run --project src/FreelanceAI.WebApi"
        exit 1
    fi
}

# Test 1: Basic Health Check
test_health() {
    echo -n "🏥 Testing health endpoint... "
    response=$(curl -s "$BASE_URL/health")
    if echo "$response" | grep -q "healthy"; then
        echo "✅ PASS"
    else
        echo "❌ FAIL"
        echo "Response: $response"
    fi
}

# Test 2: Detailed Health Check
test_detailed_health() {
    echo -n "🏥 Testing detailed health endpoint... "
    response=$(curl -s -X POST "$BASE_URL/api/ai/health")
    if echo "$response" | grep -q "status"; then
        echo "✅ PASS"
    else
        echo "❌ FAIL"
        echo "Response: $response"
    fi
}

# Test 3: Provider Status
test_provider_status() {
    echo -n "📊 Testing provider status... "
    response=$(curl -s "$BASE_URL/api/ai/status")
    if echo "$response" | grep -q "name"; then
        echo "✅ PASS"
        echo "   Providers found: $(echo "$response" | grep -o '"name":"[^"]*"' | wc -l)"
    else
        echo "❌ FAIL"
        echo "Response: $response"
    fi
}

# Test 4: Cost Tracking
test_cost_tracking() {
    echo -n "💰 Testing cost tracking... "
    response=$(curl -s "$BASE_URL/api/ai/spend")
    if [[ "$response" =~ ^[0-9]*\.?[0-9]+$ ]]; then
        echo "✅ PASS - Today's spend: \$$response"
    else
        echo "❌ FAIL"
        echo "Response: $response"
    fi
}

# Test 5: Basic AI Generation
test_basic_generation() {
    echo -n "🤖 Testing basic AI generation... "
    response=$(curl -s -X POST "$BASE_URL/api/ai/generate" \
        -H "Content-Type: application/json" \
        -d '{"prompt": "Say hello and tell me your name"}')
    
    if echo "$response" | grep -q '"success":true'; then
        echo "✅ PASS"
        provider=$(echo "$response" | grep -o '"provider":"[^"]*"' | cut -d'"' -f4)
        echo "   Provider used: $provider"
    elif echo "$response" | grep -q '"success":false'; then
        echo "⚠️  PARTIAL - Request processed but failed"
        echo "   This might be expected if no providers are configured"
    else
        echo "❌ FAIL"
        echo "Response: $response"
    fi
}

# Test 6: Parameter Validation
test_validation() {
    echo -n "✅ Testing input validation... "
    response=$(curl -s -X POST "$BASE_URL/api/ai/generate" \
        -H "Content-Type: application/json" \
        -d '{"prompt": ""}')
    
    if echo "$response" | grep -q "Prompt is required"; then
        echo "✅ PASS - Empty prompt correctly rejected"
    else
        echo "❌ FAIL"
        echo "Response: $response"
    fi
}

# Test 7: Response History
test_history() {
    echo -n "📚 Testing response history... "
    response=$(curl -s "$BASE_URL/api/ai/history")
    if echo "$response" | grep -q "responses"; then
        echo "✅ PASS"
    else
        echo "❌ FAIL"
        echo "Response: $response"
    fi
}

# Run all tests
main() {
    check_api
    echo
    
    test_health
    test_detailed_health
    test_provider_status
    test_cost_tracking
    test_basic_generation
    test_validation
    test_history
    
    echo
    echo "🎉 Test suite completed!"
    echo "💡 For more detailed testing, visit: $BASE_URL/swagger"
    echo "📖 See API_TESTING_GUIDE.md for comprehensive testing scenarios"
}

# Make the script executable and run it
main "$@"
