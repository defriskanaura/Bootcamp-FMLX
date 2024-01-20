# WebAPI
dotnet new webapi
Test API :
- Swagger (internal project WebAPI)
- Postman (Extension VSCode, software)
- ThunderClient (Extension VSCode)

appsettings.json :
- Configuration
- ConnectionString
- Credential

# HTTP
## HTTP Request :
- GET : Request data or get data
- POST : Send data 
- PUT : Change the data
- DELETE :  Delete the data

## HTTP Response :
- 200-299 OK
- 300-399 Redirect
- 400-499 Client Error
- 500-599 Server Error

## Endpoint:
- Category
  - GET = localhost:port/api/category
  - GET = localhost:port/api/category/{id}
  - POST = localhost:port/api/category
  - PUT = localhost:port/api/category
  - DELETE =localhost:port/api/category/{id}
  

# DTO : Data Transfer Object
```cs
class User {
  id
  name
  email
  password
  alamat
  namaibu
  ktp
}
```
```cs
class UserDTO {
  id
  name
}
```


# AUTOMAPPER
1. Install NuGet Package
   1. Automapper
   2. Automapper.DependencyInjection
2. Create class inherit from Profile
3. Create the constructor
4. Fill it with CreateMap<TSource,TTarget>();
5. Controller make private variable or composition of IMapper with its constructor
6. Inject from Program.cs - builder.Services.AddAutoMapper(typeof(NameClassAutoMapper))
7. Example to use
   1. `Category category = _map.Map<Category>(dataSource);`
   2. `List<Category> categories = _map.Map<List<Category>>(dataSource);`


# Clean Architecture (N-Tier)
Project :
1. API project = dotnet new webapi (webAPI)
2. Application project = dotnet new classlib (React, Vue, Vanilla)
3. Model project = dotnet new classlib (Model, Entity, DTOs)
4. Persistence project = dotnet new classlib (DbContext)
5. Utilities project = dotnet new classlib (Mapper, Extension Method)

Reference Project :
API -> Application, Utilities, Persistence, Model
Persistence -> Model

---

MVC :
dotnet new mvc
Model = data layer
View = front-end
Controller = logic


## Area
Install tool : dotnet tool install --global dotnet-aspnet-codegenerator --version 7.0.11 
NuGet Package : Microsoft.VisualStudio.Web.CodeGeneration.Design

dotnet-aspnet-codegenerator area Admin
dotnet-aspnet-codegenerator area Customer