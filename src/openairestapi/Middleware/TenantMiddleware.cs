#region Using Directives
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Primitives; 
#endregion

namespace OpenAiRestApi.Middleware;

public class TenantMiddleware
{
    #region Private Fields
    private readonly string _tenantParameterName;
    private readonly string _tenantHeaderName;
    private readonly string _tenantClaimName;
    private readonly string _noTenantErrorMessage;
    private readonly RequestDelegate _next;
    #endregion

    #region Public Constructors
    public TenantMiddleware(RequestDelegate next,
                       string tenantParameterName = "tenant",
                       string tenantHeaderName = "X-Tenant",
                       string tenantClaimName = "tenant",
                       string noTenantErrorMessage = "No tenant found in the request.")
    {
        _next = next;
        _tenantParameterName = tenantParameterName;
        _tenantHeaderName = tenantHeaderName;
        _tenantClaimName = tenantClaimName;
        _noTenantErrorMessage = noTenantErrorMessage;
    }
    #endregion

    #region Public Methods
    // IMessageWriter is injected into InvokeAsync
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            if (!httpContext.Request.Path.ToString().StartsWith("/openai"))
            {
                await _next(httpContext);
                return;
            }

            if (httpContext.Request.Query.ContainsKey(_tenantParameterName) &&
                !string.IsNullOrEmpty(httpContext.Request.Query[_tenantParameterName]))
            {
                await _next(httpContext);
                return;
            }

            string tenant;

            if (!string.IsNullOrEmpty(tenant = GetTenantFromHeader(httpContext.Request)))
            {
                AddTenantParameter(httpContext, tenant);
                await _next(httpContext);
                return;
            }

            if (!string.IsNullOrEmpty(tenant = GetTenantFromToken(httpContext.Request)))
            {
                AddTenantParameter(httpContext, tenant);
                await _next(httpContext);
                return;
            }

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsync(_noTenantErrorMessage);
        }
        catch (Exception ex)
        {
            // Set the status code and write the error message to the response
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync(ex.Message);
        }

    }
    #endregion

    #region Private Methods
    private void AddTenantParameter(HttpContext httpContext, string tenant)
    {
        httpContext.Request.Query = new QueryCollection(httpContext.Request.Query
                     .ToDictionary(x => x.Key, x => x.Value)
                     .Concat(new Dictionary<string, StringValues>() { { _tenantParameterName, new StringValues(tenant) } })
                     .ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private string GetTenantFromHeader(HttpRequest request)
    {
        // Retrieve the tenant parameter from the tenant header
        string tenant = request.Headers[_tenantHeaderName].FirstOrDefault()!;
        return tenant;
    }

    private string GetTenantFromToken(HttpRequest request)
    {
        // Retrieve the authorization header value
        string authorizationHeader = request.Headers.Authorization.FirstOrDefault()!;

        if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
        {
            string token = authorizationHeader.Substring("Bearer ".Length).Trim();

            // Remove the "Bearer " prefix from the token
            string jwtToken = token.Replace("Bearer ", string.Empty);

            // Create an instance of JwtSecurityTokenHandler
            var tokenHandler = new JwtSecurityTokenHandler();

            // Read the token and parse it to a JwtSecurityToken object
            var parsedToken = tokenHandler.ReadJwtToken(jwtToken);

            // Return the value of the tenant claim from the token's payload
            return parsedToken?.Claims?.FirstOrDefault(claim => claim.Type == _tenantClaimName)?.Value!;
        }
        return string.Empty;
    } 
    #endregion
}

public static class TenantMiddlewareExtensions
{
    #region Public Static Methods
    public static IApplicationBuilder UseTenantMiddleware(
    this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    } 
    #endregion
}
