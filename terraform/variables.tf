# Aurora Serverless Variables
variable "vpc_cidr" {
  description = "CIDR block for the VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for the public subnets"
  type        = list(string)
  default     = ["10.0.1.0/24", "10.0.2.0/24"]
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for the private subnets"
  type        = list(string)
  default     = ["10.0.3.0/24", "10.0.4.0/24"]
}

variable "db_username" {
  description = "Username for the database"
  type        = string
  sensitive   = true
  default     = "postgres"
}

variable "db_password" {
  description = "Password for the database"
  type        = string
  sensitive   = true
  default     = "" # Should be set via environment variable TF_VAR_db_password
}
