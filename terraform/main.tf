terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.0"
    }
  }
  # Using local backend for now
  # To use S3 backend, uncomment this and create the S3 bucket first
  # backend "s3" {
  #   bucket = "rallycasts-terraform-state"
  #   key    = "rallycasts/terraform.tfstate"
  #   region = "us-east-1"
  # }
}

provider "aws" {
  region = var.aws_region
}

# Variables
variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "app_name" {
  description = "Application name"
  type        = string
  default     = "rallycasts"
}

# Cognito User Pool
resource "aws_cognito_user_pool" "main" {
  name = "${var.app_name}-${var.environment}"

  username_attributes = ["email"]
  auto_verified_attributes = ["email"]
  
  # Configure verification
  verification_message_template {
    default_email_option = "CONFIRM_WITH_CODE"
    email_subject        = "Your verification code"
    email_message        = "Your verification code is {####}"
  }
  
  # Configure email
  email_configuration {
    email_sending_account = "COGNITO_DEFAULT"
  }
  
  mfa_configuration = "OFF"

  password_policy {
    minimum_length    = 8
    require_lowercase = true
    require_numbers   = true
    require_symbols   = true
    require_uppercase = true
  }

  schema {
    attribute_data_type      = "String"
    developer_only_attribute = false
    mutable                  = true
    name                     = "email"
    required                 = true

    string_attribute_constraints {
      min_length = 7
      max_length = 320
    }
  }

  schema {
    attribute_data_type      = "String"
    developer_only_attribute = false
    mutable                  = true
    name                     = "given_name"
    required                 = false

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  schema {
    attribute_data_type      = "String"
    developer_only_attribute = false
    mutable                  = true
    name                     = "family_name"
    required                 = false

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  admin_create_user_config {
    allow_admin_create_user_only = false
  }

  tags = {
    Environment = var.environment
    Name        = "${var.app_name}-user-pool-${var.environment}"
  }
}

# Cognito User Pool Client
resource "aws_cognito_user_pool_client" "client" {
  name                         = "${var.app_name}-client-${var.environment}"
  user_pool_id                 = aws_cognito_user_pool.main.id
  generate_secret              = true
  refresh_token_validity       = 30
  access_token_validity        = 1
  id_token_validity            = 1
  prevent_user_existence_errors = "ENABLED"
  explicit_auth_flows          = [
    "ALLOW_USER_PASSWORD_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_USER_SRP_AUTH"
  ]
}

# S3 Bucket for file uploads
resource "aws_s3_bucket" "uploads" {
  bucket = "${var.app_name}-uploads-${var.environment}"

  tags = {
    Name        = "${var.app_name}-uploads-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_s3_bucket_cors_configuration" "uploads_cors" {
  bucket = aws_s3_bucket.uploads.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "POST", "DELETE", "HEAD"]
    allowed_origins = ["*"]
    expose_headers  = ["ETag"]
    max_age_seconds = 3000
  }
}

resource "aws_s3_bucket_ownership_controls" "uploads_ownership" {
  bucket = aws_s3_bucket.uploads.id
  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

resource "aws_s3_bucket_acl" "uploads_acl" {
  depends_on = [aws_s3_bucket_ownership_controls.uploads_ownership]
  bucket = aws_s3_bucket.uploads.id
  acl    = "private"
}

# SNS Topic for notifications
resource "aws_sns_topic" "notifications" {
  name = "${var.app_name}-notifications-${var.environment}"

  tags = {
    Name        = "${var.app_name}-notifications-${var.environment}"
    Environment = var.environment
  }
}

# SQS Queue for processing notifications
resource "aws_sqs_queue" "notifications_queue" {
  name                      = "${var.app_name}-notifications-queue-${var.environment}"
  delay_seconds             = 0
  max_message_size          = 262144
  message_retention_seconds = 345600
  receive_wait_time_seconds = 0

  tags = {
    Name        = "${var.app_name}-notifications-queue-${var.environment}"
    Environment = var.environment
  }
}

# SNS Subscription to SQS
resource "aws_sns_topic_subscription" "notifications_sqs_target" {
  topic_arn = aws_sns_topic.notifications.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.notifications_queue.arn
}

# VPC for Aurora Serverless
resource "aws_vpc" "main" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true
  
  tags = {
    Name        = "${var.app_name}-vpc-${var.environment}"
    Environment = var.environment
  }
}

# Create public subnets for Aurora Serverless
resource "aws_subnet" "public" {
  count                   = length(var.public_subnet_cidrs)
  vpc_id                  = aws_vpc.main.id
  cidr_block              = var.public_subnet_cidrs[count.index]
  availability_zone       = data.aws_availability_zones.available.names[count.index]
  map_public_ip_on_launch = true
  
  tags = {
    Name        = "${var.app_name}-public-subnet-${count.index}-${var.environment}"
    Environment = var.environment
  }
}

# Create private subnets for Aurora Serverless
resource "aws_subnet" "private" {
  count             = length(var.private_subnet_cidrs)
  vpc_id            = aws_vpc.main.id
  cidr_block        = var.private_subnet_cidrs[count.index]
  availability_zone = data.aws_availability_zones.available.names[count.index]
  
  tags = {
    Name        = "${var.app_name}-private-subnet-${count.index}-${var.environment}"
    Environment = var.environment
  }
}

# Internet Gateway for the public subnets
resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id
  
  tags = {
    Name        = "${var.app_name}-igw-${var.environment}"
    Environment = var.environment
  }
}

# Route table for public subnets
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id
  
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }
  
  tags = {
    Name        = "${var.app_name}-public-route-table-${var.environment}"
    Environment = var.environment
  }
}

# Route table association for public subnets
resource "aws_route_table_association" "public" {
  count          = length(var.public_subnet_cidrs)
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

# NAT Gateway for private subnets
resource "aws_eip" "nat" {
  vpc = true
  
  tags = {
    Name        = "${var.app_name}-nat-eip-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_nat_gateway" "main" {
  allocation_id = aws_eip.nat.id
  subnet_id     = aws_subnet.public[0].id
  
  tags = {
    Name        = "${var.app_name}-nat-gateway-${var.environment}"
    Environment = var.environment
  }
  
  depends_on = [aws_internet_gateway.main]
}

# Route table for private subnets
resource "aws_route_table" "private" {
  vpc_id = aws_vpc.main.id
  
  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.main.id
  }
  
  tags = {
    Name        = "${var.app_name}-private-route-table-${var.environment}"
    Environment = var.environment
  }
}

# Route table association for private subnets
resource "aws_route_table_association" "private" {
  count          = length(var.private_subnet_cidrs)
  subnet_id      = aws_subnet.private[count.index].id
  route_table_id = aws_route_table.private.id
}

# Security group for Aurora Serverless
resource "aws_security_group" "aurora" {
  name        = "${var.app_name}-aurora-sg-${var.environment}"
  description = "Security group for Aurora Serverless"
  vpc_id      = aws_vpc.main.id
  
  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = concat(var.public_subnet_cidrs, var.private_subnet_cidrs)
  }
  
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
  
  tags = {
    Name        = "${var.app_name}-aurora-sg-${var.environment}"
    Environment = var.environment
  }
}

# DB Subnet Group for Aurora Serverless
resource "aws_db_subnet_group" "aurora" {
  name       = "${var.app_name}-aurora-subnet-group-${var.environment}"
  subnet_ids = aws_subnet.private[*].id
  
  tags = {
    Name        = "${var.app_name}-aurora-subnet-group-${var.environment}"
    Environment = var.environment
  }
}

# Get available AZs
data "aws_availability_zones" "available" {}

# Aurora Serverless v2 Cluster
resource "aws_rds_cluster" "aurora_serverless" {
  cluster_identifier      = "${var.app_name}-aurora-${var.environment}"
  engine                  = "aurora-postgresql"
  engine_mode             = "provisioned"
  engine_version          = "13.9"
  database_name           = replace(var.app_name, "-", "_")
  master_username         = var.db_username
  master_password         = var.db_password
  backup_retention_period = 7
  preferred_backup_window = "03:00-04:00"
  skip_final_snapshot     = true
  db_subnet_group_name    = aws_db_subnet_group.aurora.name
  vpc_security_group_ids  = [aws_security_group.aurora.id]
  storage_encrypted       = true
  
  serverlessv2_scaling_configuration {
    min_capacity = 0.5  # Minimum ACUs (can be as low as 0.5)
    max_capacity = 1.0  # Maximum ACUs - adjust based on your needs
  }
  
  tags = {
    Name        = "${var.app_name}-aurora-cluster-${var.environment}"
    Environment = var.environment
  }
}

# Aurora Serverless v2 Instance
resource "aws_rds_cluster_instance" "aurora_serverless_instance" {
  count                = 1
  identifier           = "${var.app_name}-aurora-instance-${count.index}-${var.environment}"
  cluster_identifier   = aws_rds_cluster.aurora_serverless.id
  instance_class       = "db.serverless"
  engine               = aws_rds_cluster.aurora_serverless.engine
  engine_version       = aws_rds_cluster.aurora_serverless.engine_version
  db_subnet_group_name = aws_db_subnet_group.aurora.name
  
  tags = {
    Name        = "${var.app_name}-aurora-instance-${count.index}-${var.environment}"
    Environment = var.environment
  }
}

# SQS Queue Policy
resource "aws_sqs_queue_policy" "notifications_queue_policy" {
  queue_url = aws_sqs_queue.notifications_queue.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "sns.amazonaws.com"
        }
        Action = "sqs:SendMessage"
        Resource = aws_sqs_queue.notifications_queue.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = aws_sns_topic.notifications.arn
          }
        }
      }
    ]
  })
}

# Platform-specific resources have been removed as they are not needed for the web application

# DynamoDB Table for Caster Requests
resource "aws_dynamodb_table" "caster_requests" {
  name           = "${var.app_name}-caster-requests-${var.environment}"
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "id"
  
  attribute {
    name = "id"
    type = "S"
  }
  
  attribute {
    name = "user_id"
    type = "S"
  }
  
  global_secondary_index {
    name               = "UserIdIndex"
    hash_key           = "user_id"
    projection_type    = "ALL"
  }
  
  tags = {
    Name        = "${var.app_name}-caster-requests-${var.environment}"
    Environment = var.environment
  }
}

# Outputs
output "cognito_user_pool_id" {
  description = "The ID of the Cognito User Pool"
  value       = aws_cognito_user_pool.main.id
}

output "cognito_client_id" {
  description = "The ID of the Cognito User Pool Client"
  value       = aws_cognito_user_pool_client.client.id
}

output "cognito_client_secret" {
  description = "The client secret of the Cognito User Pool Client"
  value       = aws_cognito_user_pool_client.client.client_secret
  sensitive   = true
}

output "s3_bucket_name" {
  description = "The name of the S3 bucket for file uploads"
  value       = aws_s3_bucket.uploads.bucket
}

output "sns_topic_arn" {
  description = "The ARN of the SNS topic for notifications"
  value       = aws_sns_topic.notifications.arn
}

output "sqs_queue_url" {
  description = "The URL of the SQS queue for notifications"
  value       = aws_sqs_queue.notifications_queue.url
}

output "dynamodb_table_name" {
  description = "The name of the DynamoDB table for caster requests"
  value       = aws_dynamodb_table.caster_requests.name
}
