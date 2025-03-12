using api.Data;
using api.Services;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;

namespace api.Extensions;

public static class ServiceExtensions
{
    public static void RegisterApplicationServices(this WebApplicationBuilder builder)
    {
        // Add PostgreSQL with Entity Framework Core
        // Determine which database connection to use based on configuration
        var useAurora = builder.Configuration.GetValue<bool>("UseAuroraServerless");
        
        if (useAurora)
        {
            // Use Aurora Serverless connection
            var auroraConnection = builder.Configuration.GetConnectionString("AuroraServerlessConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(auroraConnection));
        }
        else
        {
            // Use default PostgreSQL connection
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        }

        // Add AWS services
        builder.Services.AddAWSService<IAmazonDynamoDB>();
        builder.Services.AddAWSService<IAmazonS3>();
        builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();
        
        
        // Add application services
        builder.Services.AddSingleton<IStorageService, S3StoragesService>();
        
        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }
}
