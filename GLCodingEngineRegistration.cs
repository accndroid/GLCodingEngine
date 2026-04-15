using FinOps.GLCodingEngine.Core.Interfaces;
using FinOps.GLCodingEngine.Data;
using FinOps.GLCodingEngine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinOps.GLCodingEngine;

public static class GLCodingEngineRegistration
{
    public static IServiceCollection AddGLCodingEngine(
        this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IGLCodingRepository>(new SqlGLCodingRepository(connectionString));
        // Register HTTP Client and the AI Agent
        services.AddHttpClient<AIGLCodingAgent>();
        services.AddScoped<IGLCodingEngine, GLCodingService>();
        return services;
    }
}