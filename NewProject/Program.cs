using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NewProject.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Servislerin Kayıtları (Builder.Build() ÖNCESİNDE olmalıdır)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Session Servisi Ekleniyor
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HATA BURadaydı: Parantez eksikti ve DbContext arada kaynıyordu. Düzeltilmiş hali:


builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
// DbContext Servisi
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// 2. HTTP İstek Boru Hattı (Pipeline) Ayarları
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session Middleware (UseRouting'den sonra, UseAuthorization'dan ÖNCE olmalıdır)
app.UseSession();

// Kimlik Doğrulama ve Yetkilendirme Sıralaması
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();