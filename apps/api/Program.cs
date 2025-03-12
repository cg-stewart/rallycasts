using api.Extensions;
using api.Middleware;
using api.Services;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Amazon.SimpleEmail;
using Amazon.SimpleNotificationService;
using Amazon.SimpleQueueService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("https://www.rallycasts.com", "https://rallycasts.com", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure AWS services
var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);

// Add AWS Cognito
builder.Services.AddAWSService<IAmazonCognitoIdentityProvider>();
builder.Services.Configure<CognitoSettings>(builder.Configuration.GetSection("AWS:Cognito"));

// Add AWS S3
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("AWS:S3"));

// Add AWS SNS, SES, SQS
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSService<IAmazonSimpleEmailService>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.Configure<NotificationSettings>(builder.Configuration.GetSection("AWS:Notifications"));

// Add application services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<NotificationService>();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = $"https://cognito-idp.{builder.Configuration["AWS:Cognito:Region"]}.amazonaws.com/{builder.Configuration["AWS:Cognito:UserPoolId"]}";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

// Register application services (database, etc.)
builder.RegisterApplicationServices();

var app = builder.Build();

// Configure the HTTP request pipeline

// Add global exception handling middleware
app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Register API endpoints
app.RegisterEndpoints();

app.Run();