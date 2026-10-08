using System;
using System.Text;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Infrastructure.Identity;
using DomainCopilot.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace DomainCopilot.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)
            ));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<DomainCopilot.Api.Features.Corpus.Services.IDocumentParserService, DomainCopilot.Api.Features.Corpus.Services.DocumentParserService>();
        services.AddScoped<DomainCopilot.Api.Core.Interfaces.IEmbeddingService, DomainCopilot.Api.Infrastructure.AI.Providers.ResilientEmbeddingService>();
        services.AddScoped<DomainCopilot.Api.Core.Interfaces.IHybridRetrievalService, DomainCopilot.Api.Features.Corpus.Search.HybridRetrievalService>();
        services.AddScoped<DomainCopilot.Api.Infrastructure.Security.IPromptInjectionGuard, DomainCopilot.Api.Infrastructure.Security.PromptInjectionGuard>();
        services.AddScoped<DomainCopilot.Api.Features.Screening.Services.IScreeningPipelineService, DomainCopilot.Api.Features.Screening.Services.ScreeningPipelineService>();

        services.AddHttpClient<DomainCopilot.Api.Infrastructure.AI.Providers.GeminiLLMProvider>();
        services.AddHttpClient<DomainCopilot.Api.Infrastructure.AI.Providers.OllamaLLMProvider>();
        services.AddScoped<DomainCopilot.Api.Infrastructure.AI.Providers.GeminiLLMProvider>();
        services.AddScoped<DomainCopilot.Api.Infrastructure.AI.Providers.OllamaLLMProvider>();
        services.AddScoped<DomainCopilot.Api.Infrastructure.AI.Providers.ResilientLLMService>();

        services.AddHttpClient<DomainCopilot.Api.Infrastructure.AI.Providers.GeminiEmbeddingService>();
        services.AddHttpClient<DomainCopilot.Api.Infrastructure.AI.Providers.OllamaEmbeddingService>();

        services.AddScoped(sp => 
        {
            var httpClientFactory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(DomainCopilot.Api.Infrastructure.AI.Providers.GeminiEmbeddingService));
            var config = sp.GetRequiredService<IConfiguration>();
            return new DomainCopilot.Api.Infrastructure.AI.Providers.GeminiEmbeddingService(httpClient, config["Gemini:ApiKey"] ?? "dummy-key");
        });

        services.AddScoped(sp => 
        {
            var httpClientFactory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(DomainCopilot.Api.Infrastructure.AI.Providers.OllamaEmbeddingService));
            var config = sp.GetRequiredService<IConfiguration>();
            return new DomainCopilot.Api.Infrastructure.AI.Providers.OllamaEmbeddingService(httpClient, config["Ollama:BaseUrl"] ?? "http://localhost:11434");
        });
        
        services.AddSingleton<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>(sp => 
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var apiKey = config["OpenAI:ApiKey"] ?? "dummy-key";
            return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAITextEmbeddingGenerationService("text-embedding-3-small", apiKey);
        });

        services.AddSingleton<Microsoft.SemanticKernel.Kernel>(sp => 
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var apiKey = config["OpenAI:ApiKey"] ?? "dummy-key";
            
            var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
            Microsoft.SemanticKernel.OpenAIKernelBuilderExtensions.AddOpenAIChatCompletion(builder, "gpt-4o-mini", apiKey);
            return builder.Build();
        });

        var jwtKey = configuration["Jwt:Key"] ?? "default_super_secret_key_which_should_be_long_enough_1234567890";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "DomainCopilotApi";
        var jwtAudience = configuration["Jwt:Audience"] ?? "DomainCopilotClients";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DomainCopilot API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
