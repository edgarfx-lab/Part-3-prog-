# RaceDay – Parts 1, 2 and 3

**Module:** PROG6212 – Programming 2B  
**Application:** RaceDay event management platform  
**Stack:** ASP.NET Core 8 Web API, ASP.NET Core MVC, Entity Framework Core 8, SQL Server, Azure Blob Storage, Docker, GitHub Actions and xUnit.

RaceDay supports two roles:
- **Organiser:** creates and manages events, categories, enrolments and race results.
- **Participant:** browses events, enrols in a category, tracks enrolments and views personal results.

## Repository structure

```text
RaceDay/
├── .github/workflows/
│   ├── part2-ci.yml
│   └── part3-ci.yml
├── docs/
│   ├── RaceDay_ERD.png
│   ├── RaceDay_API_Endpoint_Plan.md
│   ├── RaceDay_Database_Script.sql
│   ├── EF-Core-CodeFirst-Notes.md
│   └── Part2_Rubric_Checklist.md
├── src/
│   ├── RaceDay.Api/       # REST API, EF Core and Azure Blob integration
│   └── RaceDay.Mvc/       # Role-aware MVC web application
├── tests/
│   └── RaceDay.Api.Tests/
├── docker-compose.yml
└── RaceDay.sln
```

## Requirements

- Visual Studio 2026 Community or another compatible IDE.
- .NET 8 SDK (the existing API targets `net8.0`; the .NET 10 SDK can also build a .NET 8 target when the .NET 8 targeting/runtime packs are available).
- SQL Server 2022 or a compatible SQL Server instance.
- SQL Server Management Studio (SSMS) recommended.
- Git and a GitHub account.
- An Azure Storage account for image uploads.
- Docker Desktop with Linux containers enabled for the container demonstration.


