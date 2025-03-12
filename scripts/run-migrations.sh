#!/bin/bash
# Database migration script for Rallycasts API

set -e

# Check if environment parameter is provided
if [ -z "$1" ]; then
  echo "Usage: $0 <environment>"
  echo "Environment options: development, staging, production"
  exit 1
fi

ENVIRONMENT=$1
echo "Running database migrations for $ENVIRONMENT environment..."

# Set environment-specific variables
case $ENVIRONMENT in
  development)
    CONNECTION_STRING_SECRET="rallycasts-dev/db-connection"
    ;;
  staging)
    CONNECTION_STRING_SECRET="rallycasts-staging/db-connection"
    ;;
  production)
    CONNECTION_STRING_SECRET="rallycasts-prod/db-connection"
    ;;
  *)
    echo "Invalid environment: $ENVIRONMENT"
    echo "Valid options: development, staging, production"
    exit 1
    ;;
esac

# Get database connection string from AWS Secrets Manager
echo "Retrieving database connection string from AWS Secrets Manager..."
CONNECTION_STRING=$(aws secretsmanager get-secret-value --secret-id $CONNECTION_STRING_SECRET --query SecretString --output text)

# Create a temporary appsettings file with the connection string
echo "Creating temporary appsettings file..."
cat > ./publish/appsettings.Migration.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "$CONNECTION_STRING"
  }
}
EOF

# Run EF Core migrations
echo "Running database migrations..."
cd ./publish
dotnet ef database update --configuration Release --no-build --verbose

# Clean up temporary files
echo "Cleaning up..."
rm -f ./appsettings.Migration.json

echo "Database migrations for $ENVIRONMENT completed successfully!"
