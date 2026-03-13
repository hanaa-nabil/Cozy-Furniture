using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Furniture.Domain.Entities;
using Furniture.Infrastructure.Data;
using Furniture.Infrastructure.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;

namespace Furniture
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseSqlServer(
         builder.Configuration.GetConnectionString("DefaultConnection"),
         sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
             maxRetryCount: 5,
             maxRetryDelay: TimeSpan.FromSeconds(30),
             errorNumbersToAdd: null
         )
     ));


            // Redis
            var redisConnectionString = builder.Configuration["Redis:ConnectionString"]!;
            var configOptions = ConfigurationOptions.Parse(redisConnectionString);
            configOptions.Ssl = true;
            configOptions.AbortOnConnectFail = false;
            configOptions.SyncTimeout = 10000;
            configOptions.AsyncTimeout = 10000;
            configOptions.ConnectTimeout = 10000;

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = configOptions; 
                options.InstanceName = "FurnitureApp_";
            });


            // Add Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();



            // Add JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration[AuthConstants.JwtSettings.Issuer],
                    ValidAudience = builder.Configuration[AuthConstants.JwtSettings.Audience],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration[AuthConstants.JwtSettings.Secret]!))
                };
            }).AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["OAuth:Google:ClientId"]!;
                options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]!;
                options.Scope.Add("profile");
                options.SaveTokens = true;
            });

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IOtpService, OtpService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<EmailService>());
            builder.Services.AddScoped<IEmailTemplateService>(sp => sp.GetRequiredService<EmailService>());


            // Services
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddScoped<ISearchService, SearchService>();

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ValidateModelFilter>();
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });

            });
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Furniture API", Version = "v1" });

                // Add JWT bearer definition
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token below. Example: eyJhbGci..."
                });

                // Make every endpoint show the lock icon
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
            });

            var app = builder.Build();
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var error = context.Features.Get<IExceptionHandlerFeature>();
                    if (error != null)
                    {
                        await context.Response.WriteAsJsonAsync(new
                        {
                            Success = false,
                            Message = "An unexpected error occurred.",
                            Detail = app.Environment.IsDevelopment()
                                        ? error.Error.Message
                                        : null
                        });
                    }
                });
            });
            // Seed Roles
            await SeedRolesAsync(app);

            async Task SeedRolesAsync(WebApplication application)
            {
                using var scope = application.Services.CreateScope();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                var roles = new[] { AuthConstants.Roles.Admin, AuthConstants.Roles.User, AuthConstants.Roles.Manager,
                    AuthConstants.Roles.Seller,AuthConstants.Roles.Customer };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }
            }
            // Seed Admin User
            using (var scope = app.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Create Admin role if it doesn't exist
                if (!await roleManager.RoleExistsAsync("Admin"))
                    await roleManager.CreateAsync(new IdentityRole("Admin"));

                // Create Admin user if it doesn't exist
                var adminEmail = "admin@furniture.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = "Super",
                        LastName = "Admin",
                        IsEmailConfirmed = true,   
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, "Admin@1234");

                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()|| app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Furniture API V1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the root
                });
            }
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}