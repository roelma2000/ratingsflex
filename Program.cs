using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ratingsflex.Areas.Identity.Data;
using Amazon.DynamoDBv2;
using ratingsflex.Areas.Movies.Data;
using Amazon;
using Amazon.S3;

namespace ratingsflex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");
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

            // Register S3Service
            builder.Services.AddAWSService<IAmazonS3>();
            builder.Services.AddScoped<IS3Service, S3Service>();

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
