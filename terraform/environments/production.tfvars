environment = "production"
aws_region = "us-east-1"

# VPC Configuration
vpc_cidr = "10.2.0.0/16"
public_subnet_cidrs = ["10.2.1.0/24", "10.2.2.0/24"]
private_subnet_cidrs = ["10.2.3.0/24", "10.2.4.0/24"]

# ECS Configuration
ecs_cluster_name = "rallycasts-prod"
ecs_service_name = "rallycasts-api-prod"
ecs_task_family = "rallycasts-api-prod"
ecs_container_name = "rallycasts-api"
ecs_desired_count = 3
ecs_container_port = 80
ecs_cpu = 1024
ecs_memory = 2048

# RDS Configuration
db_instance_class = "db.t3.large"
db_allocated_storage = 100
db_name = "rallycasts_prod"
db_multi_az = true
db_deletion_protection = true

# Cognito Configuration
cognito_user_pool_name = "rallycasts-prod-user-pool"
cognito_client_name = "rallycasts-prod-client"

# S3 Configuration
s3_bucket_name = "rallycasts-prod-storage"

# CloudFront Configuration
cloudfront_enabled = true
cloudfront_price_class = "PriceClass_All"

# Route53 Configuration
domain_name = "rallycasts.com"
create_route53_records = true

# Tags
tags = {
  Environment = "production"
  Project     = "Rallycasts"
  ManagedBy   = "Terraform"
}
