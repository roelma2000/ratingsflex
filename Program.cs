using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ratingsflex.Areas.Identity.Data;
using Amazon.DynamoDBv2;
using ratingsflex.Areas.Movies.Data;
using Amazon;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;

namespace ratingsflex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

            // Configure services
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 2_147_483_648; // 2GB
            });

            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddDefaultIdentity<ratingsflexUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add AWS DynamoDB service
            var awsOptions = builder.Configuration.GetAWSOptions();
            awsOptions.Region = RegionEndpoint.CACentral1; // Specify your region endpoint here
            builder.Services.AddAWSService<IAmazonDynamoDB>(awsOptions);

            // Register DynamoDbService
            builder.Services.AddScoped<IDynamoDbService, DynamoDbService>();

            // Register S3Service with explicit region configuration
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.CACentral1
            };
            var s3Client = new AmazonS3Client(s3Config);
            builder.Services.AddSingleton<IAmazonS3>(s3Client);
            builder.Services.AddScoped<IS3Service, S3Service>();

            // Add AWS SSM Parameter Store service with custom endpoint configuration
            var ssmConfig = new AmazonSimpleSystemsManagementConfig
            {
                RegionEndpoint = RegionEndpoint.CACentral1,
                ServiceURL = "https://ssm.ca-central-1.amazonaws.com"
            };
            var ssmClient = new AmazonSimpleSystemsManagementClient(ssmConfig);

            // Register AWS SSM service
            builder.Services.AddSingleton<IAmazonSimpleSystemsManagement>(ssmClient);

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Register", "Register");
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Login", "Login");
            });

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.Use(async (context, next) =>
            {
                context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = 2_147_483_648; // 2GB
                await next.Invoke();
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();
            app.MapControllers();
            app.Run();
        }
    }
}
