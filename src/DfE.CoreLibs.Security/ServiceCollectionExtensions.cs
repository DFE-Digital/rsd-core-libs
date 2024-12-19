﻿using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication;

namespace DfE.CoreLibs.Security
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Token Service and its dependencies in the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration object containing token settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddUserTokenService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TokenSettings>(configuration.GetSection("Authorization:TokenSettings"));
            services.AddScoped<IUserTokenService, UserTokenService>();
            services.AddHttpContextAccessor();
            return services;
        }

        /// <summary>
        /// Registers the Api Obo Token Service and its dependencies in the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration object containing token settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApiOboTokenService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TokenSettings>(configuration.GetSection("Authorization:TokenSettings"));
            services.AddScoped<IApiOboTokenService, ApiOboTokenService>();
            services.AddHttpContextAccessor();
            return services;
        }

        /// <summary>
        /// ----<br/>
        /// DO NOT USE THIS METHOD IF YOU ARE USING AZURE OR ANY OTHER THIRD PARTY IDENTITY SERVICE PROVIDER TO GENERATE AN ACCESS TOKEN.
        /// ONLY USE THIS METHOD IF YOU ARE USING <see cref="IUserTokenService.GetUserTokenAsync"/> TO GENERATE A CUSTOM TOKEN.
        /// ----<br/>
        /// Adds and configures Custom JWT Bearer authentication which uses Symmetric Security Key to validate a custom token.
        /// </summary>
        /// <param name="services">The service collection to which authentication services are added.</param>
        /// <param name="configuration">The application configuration containing token settings.</param>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <param name="authenticationBuilder">The authentication builder.</param>
        /// <param name="jwtBearerEvents">The JwtBearerEvents.</param>
        /// <returns>The updated service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the TokenSettings section is missing in configuration.</exception>
        public static IServiceCollection AddCustomJwtAuthentication(this IServiceCollection services, IConfiguration configuration, string authenticationScheme, AuthenticationBuilder authenticationBuilder, JwtBearerEvents? jwtBearerEvents = null)
        {
            var tokenSettings = configuration.GetSection("Authorization:TokenSettings").Get<TokenSettings>();

            if (tokenSettings == null)
            {
#pragma warning disable S3928
                throw new ArgumentNullException(nameof(tokenSettings), "TokenSettings section is missing in configuration.");
#pragma warning restore S3928
            }

            authenticationBuilder.AddJwtBearer(authenticationScheme: authenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = tokenSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = tokenSettings.Audience,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.SecretKey)),

                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };

                if (jwtBearerEvents != null)
                    options.Events = jwtBearerEvents;
            });

            return services;
        }
    }
}