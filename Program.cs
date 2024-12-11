using Microsoft.AspNetCore.Mvc.TagHelpers;
using TravelInsuranceAdvisor.Hubs;
using TravelInsuranceAdvisor.Services;
using TravelInsuranceAdvisor.Helpers;


var builder = WebApplication.CreateBuilder(args);

//Add services to the DI container.
builder.Services.AddRazorPages();
//SignalR service
builder.Services.AddSignalR(); 
//HttpClient Service
builder.Services.AddHttpClient();
//QuoteService
builder.Services.AddScoped<QuoteService>();
//Validation Helpers
builder.Services.AddTransient<ValidatonHelper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

//Map the SignalR hub
app.MapHub<ChatHub>("/chathub"); 

app.Run();
