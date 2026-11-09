# Payment Validation React UI

## Purpose
Admin interface for managing business validation rules. Allows users to create, update, delete, and test validation rules.

## Technology Stack
- React 18+
- TypeScript
- React Query (for API calls)
- Material-UI or Tailwind CSS (for styling)
- React Router (for navigation)

## Features

### 1. Rules Management
- **List Rules:** Display all available validation rules
- **Create Rule:** Form to create new validation rule
- **Edit Rule:** Update existing rule configuration
- **Delete Rule:** Remove rule from system
- **Test Rule:** Test rule against sample payment data

### 2. Rule Builder Interface
Visual rule builder for non-technical users:
- Drag-and-drop conditions
- Field selector (VIN, Credit Amount, etc.)
- Operator selector (equals, greater than, not null, etc.)
- Value input

### 3. Payment Testing
- Input sample payment data
- Execute validation against selected rules
- Display validation results

## API Integration

This React app will call the Rules Engine API:

```typescript
// Example API calls
const rulesApi = {
  // Get all rules
  getRules: () => fetch('http://dev-eus-ca-payment-rules-01/api/rules'),

  // Create new rule
  createRule: (rule) => fetch('http://dev-eus-ca-payment-rules-01/api/rules', {
    method: 'POST',
    body: JSON.stringify(rule)
  }),

  // Test validation
  validate: (ruleName, data) => fetch('http://dev-eus-ca-payment-rules-01/api/validate', {
    method: 'POST',
    body: JSON.stringify({ ruleName, data })
  })
};
```

## Deployment

This app will be deployed as a Container App:
- **Name:** `dev-eus-ca-payment-ui-01`
- **Ingress:** External (publicly accessible)
- **URL:** `https://dev-eus-ca-payment-ui-01.azurecontainerapps.io`

## Docker Configuration

Create a `Dockerfile` in this folder:
```dockerfile
# Build stage
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Production stage
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## Sample Project Structure

```
react-app/
├── public/
├── src/
│   ├── components/
│   │   ├── RulesList.tsx
│   │   ├── RuleForm.tsx
│   │   ├── RuleBuilder.tsx
│   │   └── PaymentTester.tsx
│   ├── api/
│   │   └── rulesApi.ts
│   ├── types/
│   │   └── Rule.ts
│   ├── App.tsx
│   └── index.tsx
├── package.json
├── tsconfig.json
└── Dockerfile
```

## Next Steps

1. Create React app: `npx create-react-app . --template typescript`
2. Install dependencies: React Query, Material-UI/Tailwind
3. Implement rule management UI
4. Create Dockerfile and nginx.conf
5. Test locally before deploying to Azure

## Environment Variables

Create `.env` file:
```
REACT_APP_RULES_API_URL=http://dev-eus-ca-payment-rules-01
```
