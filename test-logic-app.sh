#!/bin/bash

# Get the Logic App trigger URL
echo "🔍 Getting Logic App trigger URL..."
TRIGGER_URL=$(az rest --method post \
  --uri "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-sfs-payment-validation/providers/Microsoft.Logic/workflows/la-sfs-payment-validation/triggers/manual/listCallbackUrl?api-version=2016-06-01" \
  --query "value" -o tsv)

echo "✅ Trigger URL obtained"
echo ""

# Test 1: Valid payment (PAY-001)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 1: Valid Payment (PAY-001)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
curl -X POST "$TRIGGER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentId": "PAY-001",
    "ruleSelection": "All Rules"
  }' | jq '.'
echo ""
echo ""

# Test 2: BR-005 violation (PAY-002)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 2: BR-005 Violation (PAY-002)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
curl -X POST "$TRIGGER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentId": "PAY-002",
    "ruleSelection": "All Rules"
  }' | jq '.'
echo ""
echo ""

# Test 3: BR-008 violation (PAY-003)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 3: BR-008 Violation (PAY-003)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
curl -X POST "$TRIGGER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentId": "PAY-003",
    "ruleSelection": "All Rules"
  }' | jq '.'
echo ""
echo ""

# Test 4: Both violations (PAY-004)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 4: Both Violations (PAY-004)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
curl -X POST "$TRIGGER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentId": "PAY-004",
    "ruleSelection": "All Rules"
  }' | jq '.'
echo ""
echo ""

# Test 5: Specific rule (BR-005 only)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 5: BR-005 Only (PAY-002)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
curl -X POST "$TRIGGER_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentId": "PAY-002",
    "ruleSelection": "BR-005"
  }' | jq '.'
echo ""
echo ""

echo "✅ All tests completed!"
echo ""
echo "💡 To manually test, use this URL:"
echo "$TRIGGER_URL"
echo ""
echo "Example curl command:"
echo "curl -X POST \"$TRIGGER_URL\" -H \"Content-Type: application/json\" -d '{\"paymentId\":\"PAY-001\",\"ruleSelection\":\"All Rules\"}'"
