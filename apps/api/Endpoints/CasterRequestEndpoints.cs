using api.Data;
using api.DTOs;
using api.Models;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.EntityFrameworkCore;

namespace api.Endpoints;

public static class CasterRequestEndpoints
{
    public static void MapCasterRequestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/caster-requests").WithTags("Caster Requests");

        group.MapGet("/", GetAllCasterRequests);
        group.MapGet("/{id}", GetCasterRequestById);
        group.MapPost("/", CreateCasterRequest);
        group.MapPut("/{id}", UpdateCasterRequest);
        group.MapDelete("/{id}", DeleteCasterRequest);
        group.MapGet("/requester/{requesterId}", GetCasterRequestsByRequesterId);
        group.MapGet("/caster/{casterId}", GetCasterRequestsByCasterId);
    }

    private static async Task<IResult> GetAllCasterRequests(IDynamoDBContext dynamoDb)
    {
        var requests = await dynamoDb.ScanAsync<CasterRequest>(default).GetRemainingAsync();
        return Results.Ok(requests.Select(r => MapToCasterRequestDto(r)));
    }

    private static async Task<IResult> GetCasterRequestById(string id, IDynamoDBContext dynamoDb)
    {
        var request = await dynamoDb.LoadAsync<CasterRequest>(id);
        
        if (request is null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(MapToCasterRequestDto(request));
    }

    private static async Task<IResult> GetCasterRequestsByRequesterId(
        int requesterId, 
        IDynamoDBContext dynamoDb,
        ApplicationDbContext db)
    {
        var user = await db.Users.FindAsync(requesterId);
        if (user is null)
        {
            return Results.NotFound("Requester not found");
        }
        
        var requests = await dynamoDb.ScanAsync<CasterRequest>(
            new[] { new ScanCondition("RequesterId", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, requesterId) }
        ).GetRemainingAsync();
        
        return Results.Ok(requests.Select(r => MapToCasterRequestDto(r)));
    }

    private static async Task<IResult> GetCasterRequestsByCasterId(
        int casterId, 
        IDynamoDBContext dynamoDb,
        ApplicationDbContext db)
    {
        var caster = await db.Casters.FindAsync(casterId);
        if (caster is null)
        {
            return Results.NotFound("Caster not found");
        }
        
        var requests = await dynamoDb.ScanAsync<CasterRequest>(
            new[] { new ScanCondition("CasterId", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, casterId) }
        ).GetRemainingAsync();
        
        return Results.Ok(requests.Select(r => MapToCasterRequestDto(r)));
    }

    private static async Task<IResult> CreateCasterRequest(
        CasterRequestCreateDto requestDto, 
        int requesterId,
        IDynamoDBContext dynamoDb,
        ApplicationDbContext db)
    {
        var requester = await db.Users.FindAsync(requesterId);
        if (requester is null)
        {
            return Results.NotFound("Requester not found");
        }

        // Validate caster if provided
        if (requestDto.CasterId.HasValue)
        {
            var caster = await db.Casters.FindAsync(requestDto.CasterId.Value);
            if (caster is null)
            {
                return Results.NotFound("Caster not found");
            }
        }

        var request = new CasterRequest
        {
            Id = Guid.NewGuid().ToString(),
            EventName = requestDto.EventName,
            Location = requestDto.Location,
            EventDate = requestDto.EventDate,
            EventDescription = requestDto.EventDescription,
            RequesterId = requesterId,
            RequesterName = $"{requester.FirstName} {requester.LastName}",
            CasterId = requestDto.CasterId,
            Price = requestDto.Price,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await dynamoDb.SaveAsync(request);

        return Results.Created($"/api/caster-requests/{request.Id}", MapToCasterRequestDto(request));
    }

    private static async Task<IResult> UpdateCasterRequest(
        string id, 
        CasterRequestUpdateDto requestDto, 
        IDynamoDBContext dynamoDb)
    {
        var request = await dynamoDb.LoadAsync<CasterRequest>(id);
        if (request is null)
        {
            return Results.NotFound();
        }

        if (!string.IsNullOrEmpty(requestDto.Status))
        {
            request.Status = requestDto.Status;
        }
        
        if (requestDto.Price.HasValue)
        {
            request.Price = requestDto.Price.Value;
        }
        
        request.UpdatedAt = DateTime.UtcNow;

        await dynamoDb.SaveAsync(request);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCasterRequest(string id, IDynamoDBContext dynamoDb)
    {
        var request = await dynamoDb.LoadAsync<CasterRequest>(id);
        if (request is null)
        {
            return Results.NotFound();
        }

        await dynamoDb.DeleteAsync<CasterRequest>(id);
        return Results.NoContent();
    }
    
    private static CasterRequestDto MapToCasterRequestDto(CasterRequest request)
    {
        return new CasterRequestDto
        {
            Id = request.Id,
            EventName = request.EventName,
            Location = request.Location,
            EventDate = request.EventDate,
            EventDescription = request.EventDescription,
            RequesterId = request.RequesterId,
            RequesterName = request.RequesterName,
            CasterId = request.CasterId,
            Status = request.Status,
            Price = request.Price,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }
}
