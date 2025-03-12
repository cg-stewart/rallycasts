#!/bin/bash
# Initialize AWS services in LocalStack for local development

set -e

echo "Initializing AWS services in LocalStack..."

# Set up AWS CLI to use LocalStack
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
export ENDPOINT_URL=http://localhost:4566

# Create S3 bucket
echo "Creating S3 bucket..."
aws --endpoint-url=$ENDPOINT_URL s3 mb s3://rallycasts-local

# Create Cognito User Pool
echo "Creating Cognito User Pool..."
aws --endpoint-url=$ENDPOINT_URL cognito-idp create-user-pool \
  --pool-name rallycasts-local \
  --auto-verified-attributes email \
  --schema Name=email,Required=true,Mutable=true \
  --schema Name=given_name,Required=true,Mutable=true \
  --schema Name=family_name,Required=true,Mutable=true

# Get the User Pool ID
USER_POOL_ID=$(aws --endpoint-url=$ENDPOINT_URL cognito-idp list-user-pools --max-results 10 | jq -r '.UserPools[] | select(.Name=="rallycasts-local") | .Id')

# Create Cognito App Client
echo "Creating Cognito App Client..."
aws --endpoint-url=$ENDPOINT_URL cognito-idp create-user-pool-client \
  --user-pool-id $USER_POOL_ID \
  --client-name rallycasts-local-client \
  --no-generate-secret \
  --explicit-auth-flows ADMIN_NO_SRP_AUTH USER_PASSWORD_AUTH

# Create SNS Topic for notifications
echo "Creating SNS Topic..."
aws --endpoint-url=$ENDPOINT_URL sns create-topic --name rallycasts-notifications

# Create SQS Queue for background processing
echo "Creating SQS Queue..."
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --queue-name rallycasts-background-tasks

echo "AWS services initialization completed successfully!"
