
using TodoApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using BCrypt.Net;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");

// הגדרת משתנה עבור Policy ה-CORS
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// ********** 1. הגדרת שירותי CORS, DB, ו-Swagger (BUILDER) **********
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // הרשאה כללית לכל מקור, כותרת ו-Method (עבור פיתוח)
                          policy.WithOrigins("https://todolist-client-0241.onrender.com")                               
                          .AllowAnyHeader()
                           .AllowAnyMethod();
                      });
});

// הגדרת שירותי ה-DB Context
builder.Services.AddDbContext<ToDoDbContext>(options =>
{
    // משתמשים ב-connectionString מתוך appsettings.json
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// הוספת Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ********** 2. הגדרת שירותי האימות (Authentication) **********

// מפתח סודי (Jwt:Key)
var jwtSecretKey = builder.Configuration["Jwt:Key"] ?? "THIS_IS_A_VERY_LONG_AND_COMPLEX_SECRET_KEY_FOR_DEMO_PURPOSES";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = false, 
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();


var app = builder.Build();

// ********** 3. הגדרות סביבת ריצה (APP) **********

// הפעלת Swagger ב-Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// הפעלת CORS
app.UseCors(MyAllowSpecificOrigins);

// חובה! הפעלת האימות וההרשאה (חובה לפני ה-MapEndpoints)
app.UseAuthentication();
app.UseAuthorization();

// ********** 4. ניתובים לאימות (Authentication) תחת Route /auth **********

// יוצר קבוצת ניתובים שכל ה-Endpoints בה מתחילים ב- /auth
var authGroup = app.MapGroup("/auth");

// ניתוב לרישום משתמש חדש
authGroup.MapPost("/register", async (ToDoDbContext context, RegisterDto registerDto, IConfiguration configuration) =>
{
    // בדיקה אם המשתמש כבר קיים
    if (await context.Users.AnyAsync(u => u.Username == registerDto.Username))
    {
        return Results.Conflict("User already exists.");
    }

    // 👈 התיקון: גיבוב הסיסמה לפני שמירה
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

    var user = new User
    {
        Username = registerDto.Username,
        // שמירת הסיסמה המגובבת
        PasswordHash = hashedPassword 
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    var token = GenerateToken(user, configuration);
    return Results.Ok(new { user.Username, token });
});

// ניתוב להתחברות משתמש
authGroup.MapPost("/login", async (ToDoDbContext context, LoginDto loginDto, IConfiguration configuration) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
if (user is null)
    {
        return Results.Unauthorized();
    }
    // בדיקה לא מאובטחת - לצורך הדגמה
if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))    {
        return Results.Unauthorized();
    }

    var token = GenerateToken(user, configuration);
    return Results.Ok(new { user.Username, token });
});


// ********** 5. ניתובים למשימות (Todo Items) - מוגנים ע"י .RequireAuthorization() **********

// GET: שליפת כל המשימות
// GET: שליפת כל המשימות עבור המשתמש המחובר בלבד
app.MapGet("/items", async (ToDoDbContext context, ClaimsPrincipal user) =>
{
    var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null || !int.TryParse(userIdString, out int userId))
    {
        return Results.Unauthorized();
    }
    
    // 👈 סינון המשימות
    var userItems = await context.Items
                                .Where(i => i.UserId == userId)
                                .ToListAsync();
    
    return Results.Ok(userItems);
})
.RequireAuthorization();

app.MapPost("/items", async (ToDoDbContext context, Item item, ClaimsPrincipal user) =>
{
    // 1. חילוץ מזהה המשתמש (ID) מה-JWT Token
    var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null || !int.TryParse(userIdString, out int userId))
    {
        return Results.Unauthorized();
    }
    
    // 👈 2. קישור המשימה למזהה המשתמש
    item.UserId = userId; 

    context.Items.Add(item);
    await context.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item); 
}).RequireAuthorization();

app.MapPut("/items/{id}", async (ToDoDbContext context, int id, Item updatedItem, ClaimsPrincipal user) =>
{
    var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null || !int.TryParse(userIdString, out int userId))
    {
        return Results.Unauthorized();
    }
    
    var existingItem = await context.Items
                                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    if (existingItem is null) 
    {
        return Results.NotFound();
    }
    
    existingItem.IsComplete = updatedItem.IsComplete;

    await context.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/items/{id}", async (ToDoDbContext context, int id, ClaimsPrincipal user) =>
{
    // 1. חילוץ מזהה המשתמש מה-JWT Token
    var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null || !int.TryParse(userIdString, out int userId))
    {
        return Results.Unauthorized();
    }

    // 2. בדיקה שהמשימה קיימת ושייכת למשתמש
    var item = await context.Items
                            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    if (item is null) 
    {
        // אם לא נמצא, זה או שה-ID לא קיים או שהוא שייך למשתמש אחר
        return Results.NotFound();
    }

    context.Items.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();


// ניתוב בסיסי (אינו מוגן)
app.MapGet("/", () => "Hello World!");

// ********** 6. הרצת האפליקציה **********
app.Run();


// ********** 7. פונקציה סטטית ליצירת טוקן (JWT) **********

// פונקציה ליצירת JWT Token עבור משתמש נתון
static string GenerateToken(User user, IConfiguration configuration)
{
    // 1. הגדרת ה-Claims (הצהרות): מה המידע שאנחנו מכניסים לטוקן
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username!)
    };

    // 2. קבלת המפתח הסודי והגדרות
    var jwtSecretKey = configuration["Jwt:Key"] ?? "THIS_IS_A_VERY_LONG_AND_COMPLEX_SECRET_KEY_FOR_DEMO_PURPOSES";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // 3. יצירת האסימון (Token)
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        // Issuer ו-Audience ריקים כרגע
        expires: DateTime.Now.AddHours(2), // תוקף הטוקן הוא שעתיים
        claims: claims,
        signingCredentials: credentials);

    // 4. המרת האסימון לטקסט
    return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
}