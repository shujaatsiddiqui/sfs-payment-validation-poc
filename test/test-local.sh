#!/bin/bash

# Test script for SFS Payment Validation Function
# This script tests all 4 payment scenarios locally

FUNCTION_URL="http://localhost:7071/api/SfsValidatePayment"

echo "======================================"
echo "SFS Payment Validation - Local Tests"
echo "======================================"
echo ""

# Test 1: PAY-001 (Valid Payment)
echo "Test 1: PAY-001 (Valid Payment - Should Pass)"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-001","ruleSelection":"All Rules"}' | jq .
echo ""
echo ""

# Test 2: PAY-002 (BR-005 Violation)
echo "Test 2: PAY-002 (BR-005 Violation - Zero Amount)"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-002","ruleSelection":"All Rules"}' | jq .
echo ""
echo ""

# Test 3: PAY-003 (BR-008 Violation)
echo "Test 3: PAY-003 (BR-008 Violation - Status 93)"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-003","ruleSelection":"All Rules"}' | jq .
echo ""
echo ""

# Test 4: PAY-004 (Both Violations)
echo "Test 4: PAY-004 (Both Violations)"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-004","ruleSelection":"All Rules"}' | jq .
echo ""
echo ""

# Test 5: Rule Selection - BR-005 only
echo "Test 5: PAY-002 with BR-005 only"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-002","ruleSelection":"BR-005"}' | jq .
echo ""
echo ""

# Test 6: Rule Selection - BR-008 only
echo "Test 6: PAY-003 with BR-008 only"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-003","ruleSelection":"BR-008"}' | jq .
echo ""
echo ""

# Test 7: Error handling - Invalid payment ID
echo "Test 7: Invalid Payment ID (Should return 404)"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-999","ruleSelection":"All Rules"}' | jq .
echo ""
echo ""

# Test 8: Error handling - Missing payment ID
echo "Test 8: Missing Payment ID (Should return 400)"
echo "--------------------------------------"
curl -s -X POST "$FUNCTION_URL" \
  -H "Content-Type: application/json" \
  -d '{"ruleSelection":"All Rules"}' | jq .
echo ""
echo ""

echo "======================================"
echo "All tests completed!"
echo "======================================"
