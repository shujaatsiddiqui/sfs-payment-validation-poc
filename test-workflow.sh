#!/bin/bash

# SFS Payment Validation Workflow - End-to-End Test
# Based on WORKFLOW_BUSINESS_LOGIC.md test scenarios

LOGIC_APP_URL="https://prod-28.centralus.logic.azure.com:443/workflows/c37d40a22c174f8988a9b9edcdb985cd/triggers/Receive_Payment_Batch/paths/invoke?api-version=2019-05-01&sp=%2Ftriggers%2FReceive_Payment_Batch%2Frun&sv=1.0&sig=IHVxjIyJ4TXC9xoW1ZAgTZTFilQVaCBFKNkQ43rvlZQ"

echo "========================================="
echo "SFS Payment Validation Workflow Test"
echo "========================================="
echo ""
echo "Test Data (from WORKFLOW_BUSINESS_LOGIC.md):"
echo ""
echo "PAY-001: amount=1000  → EXPECTED: SKIPPED (duplicate)"
echo "PAY-002: amount=500   → EXPECTED: APPROVED (payoff)"
echo "PAY-003: amount=800   → EXPECTED: REJECTED (status 93)"
echo "PAY-004: amount=50    → EXPECTED: REJECTED (closed to posting)"
echo "PAY-006: amount=100   → EXPECTED: REJECTED (zero balance + overpayment)"
echo ""
echo "========================================="
echo ""

# Test payload matching the business logic document
TEST_PAYLOAD='{
  "payments": [
    {"id": "PAY-001", "amount": 1000},
    {"id": "PAY-002", "amount": 500},
    {"id": "PAY-003", "amount": 800},
    {"id": "PAY-004", "amount": 50},
    {"id": "PAY-006", "amount": 100}
  ]
}'

echo "Sending batch of 5 payments to Logic App..."
echo ""

RESPONSE=$(curl -X POST "$LOGIC_APP_URL" \
  -H "Content-Type: application/json" \
  -d "$TEST_PAYLOAD" \
  -s -w "\nHTTP_STATUS:%{http_code}")

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS/d')

echo "HTTP Status: $HTTP_STATUS"
echo ""
echo "Response:"
echo "$BODY" | jq '.'

echo ""
echo "========================================="
echo "Results Summary:"
echo "========================================="
echo ""

# Parse and display results
echo "$BODY" | jq -r '.results[] | "Payment: \(.paymentId) | Status: \(.status) | Type: \(.paymentType // "N/A") | Errors: \(.errors // [] | join(", "))"'

echo ""
echo "========================================="
echo "Expected vs Actual:"
echo "========================================="
echo ""
echo "$BODY" | jq -r '
.results[] |
if .paymentId == "PAY-001" then
  "✓ PAY-001: \(.status) (expected: SKIPPED)"
elif .paymentId == "PAY-002" then
  if .status == "APPROVED" and .paymentType == "Payoff" then
    "✓ PAY-002: \(.status) - \(.paymentType) ✅"
  else
    "✗ PAY-002: \(.status) - \(.paymentType) (expected: APPROVED - Payoff)"
  end
elif .paymentId == "PAY-003" then
  if .status == "REJECTED" and (.errors | any(contains("status 93"))) then
    "✓ PAY-003: \(.status) - BR-002 violation ✅"
  else
    "✗ PAY-003: \(.status) (expected: REJECTED with BR-002)"
  end
elif .paymentId == "PAY-004" then
  if .status == "REJECTED" and (.errors | any(contains("closed to posting"))) then
    "✓ PAY-004: \(.status) - BR-003 violation ✅"
  else
    "✗ PAY-004: \(.status) (expected: REJECTED with BR-003)"
  end
elif .paymentId == "PAY-006" then
  if .status == "REJECTED" then
    "✓ PAY-006: \(.status) - Multiple violations ✅"
  else
    "✗ PAY-006: \(.status) (expected: REJECTED)"
  end
else
  "? \(.paymentId): Unknown payment"
end
'

echo ""
echo "Test completed!"
