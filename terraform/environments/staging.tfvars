environment = "staging"
aws_region = "us-east-1"

# VPC Configuration
vpc_cidr = "10.1.0.0/16"
public_subnet_cidrs = ["10.1.1.0/24", "10.1.2.0/24"]
private_subnet_cidrs = ["10.1.3.0/24", "10.1.4.0/24"]

# ECS Configuration
ecs_cluster_name = "rallycasts-staging"
ecs_service_name = "rallycasts-api-staging"
ecs_task_family = "rallycasts-api-staging"
ecs_container_name = "rallycasts-api"
ecs_desired_count = 2
ecs_container_port = 80
ecs_cpu = 512
ecs_memory = 1024

# RDS Configuration
db_instance_class = "db.t3.medium"
db_allocated_storage = 50
db_name = "rallycasts_staging"
db_multi_az = true
db_deletion_protection = true

# Cognito Configuration
cognito_user_pool_name = "rallycasts-staging-user-pool"
cognito_client_name = "rallycasts-staging-client"

# S3 Configuration
s3_bucket_name = "rallycasts-staging-storage"

# CloudFront Configuration
cloudfront_enabled = true
cloudfront_price_class = "PriceClass_100"

# Route53 Configuration
domain_name = "staging.rallycasts.com"
create_route53_records = true

# Tags
tags = {
  Environment = "staging"
  Project     = "Rallycasts"
  ManagedBy   = "Terraform"
}
