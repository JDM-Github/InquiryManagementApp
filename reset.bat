echo y | rmdir /s Migrations
dotnet ef database drop --force
dotnet ef migrations add "InitialUpdate"
dotnet ef database update
