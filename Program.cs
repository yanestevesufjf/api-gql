using System.Text;
using API;
using API.Auth;
using API.Models;
using API.Ofertas;
using API.Repositories;
using API.Users;
using API.Veiculos;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// carrego os repositories
builder.Services
    .AddSingleton<IUsuarioRepository, UsuarioRepository>()
    .AddSingleton<IVeiculoRepository, VeiculoRepository>()
    .AddSingleton<IOfertaRepository, OfertaRepository>();


// inicio graphql server
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()

    // adiciono as query
    .AddQueryType()
        .AddTypeExtension<VeiculosQueries>()
        .AddTypeExtension<OfertasQueries>()
        .AddTypeExtension<UsersQueries>()

    // adiciono as mutations
    .AddMutationType()
        .AddTypeExtension<VeiculosMutation>()
        .AddTypeExtension<OfertasMutation>()
        .AddTypeExtension<AuthMutation>()

    // adiciono as subscriptions
    .AddSubscriptionType()
        .AddTypeExtension<VeiculosSubscription>()
        .AddTypeExtension<OfertasSubscription>()

    // dou suporte ao pub/sub da subscription
    .AddInMemorySubscriptions()
    .AddApolloTracing();

// configuro o jwt e authorization
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration.GetSection("TokenSettings").GetValue<string>("Issuer"),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = builder.Configuration.GetSection("TokenSettings").GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("TokenSettings").GetValue<string>("Key"))),
        };
    });

// Configuração do cors para permitir qualquer entrada, metodo e header.
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
{
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));


// Configure
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseRouting();
// suporte ao websockets
app.UseWebSockets();
// utilizando autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// aplicando os endpoins graphql.
app.UseEndpoints(endpoint =>
    endpoint.MapGraphQL("/graphql") // local onde posso definir uma nova rota para o playground/banana
);
app.Run();
