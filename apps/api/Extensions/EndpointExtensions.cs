using api.Endpoints;

namespace api.Extensions;

public static class EndpointExtensions
{
    public static void RegisterEndpoints(this WebApplication app)
    {
        // Register all endpoint groups
        app.MapUserEndpoints();
        app.MapVideoEndpoints();
        app.MapPhotoEndpoints();
        app.MapCasterEndpoints();
        app.MapCasterRequestEndpoints();
    }
}
