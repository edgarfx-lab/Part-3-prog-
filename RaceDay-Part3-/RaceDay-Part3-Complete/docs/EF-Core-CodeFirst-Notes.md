# EF Core Code-First Notes

The API defines the RaceDay schema through C# entity classes and `RaceDayDbContext.OnModelCreating`.

The model intentionally mirrors the approved Part 1 database:
- Users
- EventTypes
- Events
- Categories
- Enrolments
- Results

The SQL names, maximum lengths, decimal precision, defaults, unique indexes, check constraints and foreign-key relationships are explicitly configured in `RaceDayDbContext`.

For the strongest Code-First evidence, generate the migration from the API project:

```powershell
dotnet ef migrations add InitialCreate --project src/RaceDay.Api --startup-project src/RaceDay.Api
dotnet ef database update --project src/RaceDay.Api --startup-project src/RaceDay.Api
```

Then commit the generated `Migrations` folder. The migration should be reviewed against `docs/RaceDay_Database_Script.sql` before submission.
