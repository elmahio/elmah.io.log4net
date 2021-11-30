var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddLog4Net();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add the following line to decorate all messages logged through log4net and elmah.io with HTTP context information like server variables.
app.UseElmahIoLog4Net();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
