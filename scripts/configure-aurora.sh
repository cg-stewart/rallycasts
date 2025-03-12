#!/bin/bash

# Script to configure Aurora Serverless connection details in the application
# This script should be run after Terraform has created the Aurora Serverless resources

# Check if terraform output command is available
if ! command -v terraform &> /dev/null; then
    echo "Error: terraform command not found. Please install Terraform."
    exit 1
fi

# Navigate to the Terraform directory
cd "$(dirname "$0")/../terraform" || exit 1

# Get Aurora Serverless endpoint and other details from Terraform outputs
AURORA_ENDPOINT=$(terraform output -raw aurora_cluster_endpoint)
AURORA_PORT=$(terraform output -raw aurora_port)
AURORA_DB_NAME=$(terraform output -raw aurora_database_name)

if [ -z "$AURORA_ENDPOINT" ] || [ -z "$AURORA_PORT" ] || [ -z "$AURORA_DB_NAME" ]; then
    echo "Error: Could not retrieve Aurora Serverless details from Terraform outputs."
    echo "Make sure you have applied the Terraform configuration and the Aurora Serverless resources are created."
    exit 1
fi

# Prompt for database credentials
read -p "Enter Aurora Serverless database username: " DB_USERNAME
read -sp "Enter Aurora Serverless database password: " DB_PASSWORD
echo ""

# Path to the appsettings.json file
APPSETTINGS_PATH="../apps/api/appsettings.json"

# Update the appsettings.json file with Aurora Serverless connection details
if [ -f "$APPSETTINGS_PATH" ]; then
    # Create a backup of the original file
    cp "$APPSETTINGS_PATH" "${APPSETTINGS_PATH}.bak"
    
    # Replace placeholders in the connection string with actual values
    sed -i '' "s|{aurora_endpoint}|$AURORA_ENDPOINT|g" "$APPSETTINGS_PATH"
    sed -i '' "s|{db_username}|$DB_USERNAME|g" "$APPSETTINGS_PATH"
    sed -i '' "s|{db_password}|$DB_PASSWORD|g" "$APPSETTINGS_PATH"
    
    # Enable Aurora Serverless in the application settings
    sed -i '' 's|"UseAuroraServerless": false|"UseAuroraServerless": true|g' "$APPSETTINGS_PATH"
    
    echo "Successfully updated application settings with Aurora Serverless connection details."
    echo "Aurora Endpoint: $AURORA_ENDPOINT"
    echo "Aurora Database: $AURORA_DB_NAME"
    echo "Aurora Port: $AURORA_PORT"
    echo "Aurora is now enabled in the application settings."
else
    echo "Error: Could not find appsettings.json file at $APPSETTINGS_PATH"
    exit 1
fi

echo ""
echo "Next steps:"
echo "1. Ensure your application has the necessary permissions to access the Aurora Serverless database."
echo "2. Run database migrations if needed."
echo "3. Restart your application to apply the new settings."
