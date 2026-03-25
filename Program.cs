using Microsoft.AspNetCore.Http.Features;

try
{
    // Move builder INSIDE the try to catch configuration errors
    var builder = WebApplication.CreateBuilder(args);

    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new Exception("CONNECTION STRING MISSING IN APPSETTINGS.JSON");

    builder.Services.AddSingleton(connectionString);

    builder.Services.Configure<FormOptions>(options => {
        options.MultipartBodyLengthLimit = 10485760; 
    });

    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment()) {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    // If it hits here, we will finally see the text
    Console.Clear();
    Console.WriteLine("FOUND THE ERROR:");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}