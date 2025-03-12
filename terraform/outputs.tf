output "aurora_cluster_endpoint" {
  description = "The cluster endpoint for the Aurora Serverless database"
  value       = aws_rds_cluster.aurora_serverless.endpoint
}

output "aurora_reader_endpoint" {
  description = "The reader endpoint for the Aurora Serverless database"
  value       = aws_rds_cluster.aurora_serverless.reader_endpoint
}

output "aurora_database_name" {
  description = "The name of the Aurora Serverless database"
  value       = aws_rds_cluster.aurora_serverless.database_name
}

output "aurora_port" {
  description = "The port on which the Aurora Serverless database accepts connections"
  value       = aws_rds_cluster.aurora_serverless.port
}

output "vpc_id" {
  description = "The ID of the VPC"
  value       = aws_vpc.main.id
}

output "public_subnet_ids" {
  description = "The IDs of the public subnets"
  value       = aws_subnet.public[*].id
}

output "private_subnet_ids" {
  description = "The IDs of the private subnets"
  value       = aws_subnet.private[*].id
}
