using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WaterService.Data;
using WaterService.Middleware;
using WaterService.Models;
using WaterService.Services;

// Helper functions
static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}

static string? ExtractDatabasePath(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains("Data Source="))
        return null;
    
    var dbPath = connectionString.Replace("Data Source=", "").Trim();
    return dbPath.Contains(';') ? dbPath.Split(';')[0] : dbPath;
}

static async Task<bool> ShouldRecreateDatabaseAsync(string dbPath, ILogger logger)
{
    try
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();
        
        // Check table count
        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%'";
        var tableCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
        
        // Check migration history
        var historyCommand = connection.CreateCommand();
        historyCommand.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId";
        var existingMigrations = new List<string>();
        
        using var reader = await historyCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            existingMigrations.Add(reader.GetString(0));
        }
        
        var hasConflictingMigrations = existingMigrations.Any(m => 
            m.Contains("AddCustomerTableToDB") || m.Contains("AddCustomerCodeToCustomer"));
        
        if (tableCount == 0 || hasConflictingMigrations)
        {
            logger.LogWarning(hasConflictingMigrations 
                ? "Database có migration history cũ không tương thích. Sẽ xóa và tạo lại..."
                : "Database tồn tại nhưng không có bảng. Sẽ xóa và tạo lại...");
            
            if (hasConflictingMigrations)
            {
                logger.LogWarning("Migrations hiện tại: {Migrations}", string.Join(", ", existingMigrations));
            }
            
            return true;
        }
        
        logger.LogInformation("Database đã có {TableCount} bảng.", tableCount);
        return false;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Không thể kiểm tra database.");
        return false;
    }
}

static async Task RecreateDatabaseAsync(string dbPath, ApplicationDbContext context, ILogger logger)
{
    try
    {
        logger.LogWarning("Xóa database cũ và tạo lại: {DbPath}", dbPath);
        File.Delete(dbPath);
        
        await context.Database.MigrateAsync();
        logger.LogInformation("Đã tạo lại database thành công.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Không thể tạo lại database. Thử dùng EnsureCreated...");
        
        try
        {
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database đã được tạo với EnsureCreated.");
        }
        catch (Exception ensureEx)
        {
            logger.LogError(ensureEx, "Không thể tạo database bằng EnsureCreated.");
        }
    }
}

static void EnsureDatabaseDirectoryExists(string? connectionString, ILogger logger)
{
    var dbPath = ExtractDatabasePath(connectionString);
    if (string.IsNullOrEmpty(dbPath))
        return;
    
    try
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            logger.LogInformation("Đã tạo thư mục database: {Directory}", directory);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Không thể tạo thư mục database.");
    }
}

static async Task InitializeAdminUserAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        
        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                Username = "admin",
                Password = HashPassword("admin123"),
                DisplayName = "Quản trị viên",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Đã khởi tạo tài khoản admin thành công - Username: admin, Password: admin123");
        }
        else
        {
            logger.LogInformation("Tài khoản admin đã tồn tại");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo tài khoản admin.");
    }
}

static async Task<int> GetTableCountAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        
        await connection.CloseAsync();
        return count;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Không thể đếm số bảng.");
        return 0;
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add custom services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ITierPriceService, TierPriceService>();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseMiddleware<AdminAuthorizationMiddleware>();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var config = services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    
    var connectionString = config.GetConnectionString("DefaultConnection");
    logger.LogInformation("Khởi tạo database với Connection String: {ConnectionString}", connectionString);
    
    EnsureDatabaseDirectoryExists(connectionString, logger);
    
    var dbPath = ExtractDatabasePath(connectionString);
    
    // Check if database needs to be recreated
    if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
    {
        var shouldRecreate = await ShouldRecreateDatabaseAsync(dbPath, logger);
        if (shouldRecreate)
        {
            await RecreateDatabaseAsync(dbPath, dbContext, logger);
        }
        else
        {
            // Run migrations normally
            try
            {
                logger.LogInformation("Đang chạy migrations...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations đã hoàn thành.");
            }
            catch (SqliteException sqliteEx)
            {
                logger.LogError(sqliteEx, "Lỗi SQLite trong quá trình migration.");
                await RecreateDatabaseAsync(dbPath, dbContext, logger);
            }
        }
    }
    else
    {
        // First time running - create database
        logger.LogInformation("Database chưa tồn tại. Đang tạo mới...");
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database đã được tạo thành công.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không thể tạo database với migrations. Thử dùng EnsureCreated...");
            await dbContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Database đã được tạo với EnsureCreated.");
        }
    }
    
    // Initialize admin user
    await InitializeAdminUserAsync(dbContext, logger);
    
    // Initialize default tier price
    var tierPriceService = services.GetRequiredService<WaterService.Services.ITierPriceService>();
    await tierPriceService.EnsureDefaultTierPriceExistsAsync();
    
    // Log database status
    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogWarning("Vẫn còn {Count} pending migrations: {Migrations}", 
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));
        }
        
        var tables = await GetTableCountAsync(dbContext, logger);
        logger.LogInformation("Database đã được khởi tạo với {TableCount} bảng.", tables);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Không thể kiểm tra database.");
    }
}

app.Run();
