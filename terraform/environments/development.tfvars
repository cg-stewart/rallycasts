environment = "development"
aws_region = "us-east-1"

# VPC Configuration
vpc_cidr = "10.0.0.0/16"
public_subnet_cidrs = ["10.0.1.0/24", "10.0.2.0/24"]
private_subnet_cidrs = ["10.0.3.0/24", "10.0.4.0/24"]

# ECS Configuration
ecs_cluster_name = "rallycasts-dev"
ecs_service_name = "rallycasts-api-dev"
ecs_task_family = "rallycasts-api-dev"
ecs_container_name = "rallycasts-api"
ecs_desired_count = 1
ecs_container_port = 80
ecs_cpu = 256
ecs_memory = 512

# RDS Configuration
# Legacy RDS settings (kept for reference)
# db_instance_class = "db.t3.small"
# db_allocated_storage = 20
# db_name = "rallycasts_dev"
# db_multi_az = false
# db_deletion_protection = false

# Aurora Serverless Configuration
db_username = "postgres"
# db_password should be set via environment variable TF_VAR_db_password

# Cognito Configuration
cognito_user_pool_name = "rallycasts-dev-user-pool"
cognito_client_name = "rallycasts-dev-client"

# S3 Configuration
s3_bucket_name = "rallycasts-dev-storage"

# CloudFront Configuration
cloudfront_enabled = true
cloudfront_price_class = "PriceClass_100"

# Route53 Configuration
domain_name = "dev.rallycasts.com"
create_route53_records = true

# Tags
tags = {
  Environment = "development"
  Project     = "Rallycasts"
  ManagedBy   = "Terraform"
}
