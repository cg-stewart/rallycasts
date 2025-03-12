#!/bin/bash
# Deployment script for Rallycasts API

set -e

# Check if environment parameter is provided
if [ -z "$1" ]; then
  echo "Usage: $0 <environment>"
  echo "Environment options: development, staging, production"
  exit 1
fi

ENVIRONMENT=$1
echo "Deploying to $ENVIRONMENT environment..."

# Set environment-specific variables
case $ENVIRONMENT in
  development)
    ECS_CLUSTER="rallycasts-dev"
    ECS_SERVICE="rallycasts-api-dev"
    ECR_REPOSITORY="rallycasts-api-dev"
    ;;
  staging)
    ECS_CLUSTER="rallycasts-staging"
    ECS_SERVICE="rallycasts-api-staging"
    ECR_REPOSITORY="rallycasts-api-staging"
    ;;
  production)
    ECS_CLUSTER="rallycasts-prod"
    ECS_SERVICE="rallycasts-api-prod"
    ECR_REPOSITORY="rallycasts-api-prod"
    ;;
  *)
    echo "Invalid environment: $ENVIRONMENT"
    echo "Valid options: development, staging, production"
    exit 1
    ;;
esac

# Get AWS account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
AWS_REGION=$(aws configure get region)

# Build and tag Docker image
echo "Building Docker image..."
docker build -t $ECR_REPOSITORY:latest ./publish

# Login to ECR
echo "Logging in to Amazon ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

# Create ECR repository if it doesn't exist
aws ecr describe-repositories --repository-names $ECR_REPOSITORY || aws ecr create-repository --repository-name $ECR_REPOSITORY

# Tag and push image to ECR
echo "Pushing image to Amazon ECR..."
docker tag $ECR_REPOSITORY:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:latest
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:latest

# Update ECS service
echo "Updating ECS service..."
aws ecs update-service --cluster $ECS_CLUSTER --service $ECS_SERVICE --force-new-deployment

echo "Deployment to $ENVIRONMENT completed successfully!"
