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

## 1. Database setup

The Part 1 SQL script is in `docs/RaceDay_Database_Script.sql`.

1. Start SQL Server.
2. Open the Part 1 script in SSMS.
3. Read the script carefully before running it. It drops and recreates the RaceDay tables when re-run; do not run it against a database containing data you need to preserve.
4. Execute it against a development/test instance to create `RaceDayDB` and its sample data.
5. Confirm that the six tables exist: Users, EventTypes, Events, Categories, Enrolments and Results.

The API connection string is in `src/RaceDay.Api/appsettings.json`. Change it to match your SQL Server instance. Do not commit passwords or production connection strings.

## 2. Configure Azure Blob Storage

Part 3 image uploads are implemented in the **API**. The MVC application sends files to the API and does not connect to Azure directly.

1. Create an Azure Storage account.
2. Open the storage account's Access keys page and copy a connection string.
3. Set the connection string as an environment variable named `AzureBlobStorage__ConnectionString`, or configure `AzureBlobStorage:ConnectionString` using .NET User Secrets.
4. Do not commit the real connection string to GitHub.
5. The API creates private `event-banners` and `profile-pictures` containers as needed and returns time-limited read-only SAS URLs for uploaded images.
6. Uploads accept image content types up to 5 MB. Configure an Azure Storage account key in the connection string so the API can generate SAS URLs.

PowerShell example for the current terminal session:

```powershell
$env:AzureBlobStorage__ConnectionString = "PASTE-YOUR-AZURE-STORAGE-CONNECTION-STRING"
```

If Azure is not configured, the rest of the application can still be explored, but image upload endpoints return HTTP 503. Configure Azure before demonstrating image uploads.

## 3. Run locally in Visual Studio

Open `RaceDay.sln`. Set `RaceDay.Api` and `RaceDay.Mvc` as separate startup projects (or run them in two terminals).

### Terminal 1 – API

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project src/RaceDay.Api/RaceDay.Api.csproj
```

The existing API launch profile uses:
- Swagger: `https://localhost:7132/swagger`
- HTTP: `http://localhost:5132`

### Terminal 2 – MVC

```powershell
dotnet run --project src/RaceDay.Mvc/RaceDay.Mvc.csproj
```

The MVC launch profile uses:
- MVC website: `https://localhost:7233`
- HTTP: `http://localhost:5233`

The MVC app calls the API at `https://localhost:7132/` by default. If your API uses another address, change `ApiSettings:BaseUrl` in `src/RaceDay.Mvc/appsettings.json`.

Register test accounts through the MVC registration page. Create one Organiser account and one Participant account. The MVC app forwards the API's session cookie through server-side requests and keeps the role in its own session for role-aware navigation.

## 4. Part 3 functionality

### Organiser
- Role-aware dashboard with owned events, upcoming event count and enrolment totals per event.
- Create, edit and delete events.
- Upload an event banner through the API to Azure Blob Storage.
- Add and remove event categories.
- View event participants, selected categories and enrolment statuses.
- Update enrolment status.
- Capture participant finish times, positions and total finishers.

### Participant
- Browse upcoming events.
- Filter events by event type and date on the public events page.
- View event details and available categories.
- Enrol in an event by selecting a category.
- View personal enrolments and status.
- View personal race history, finish times and finishing positions.
- View and update profile details and upload a profile picture through the API.

### Security and architecture
- MVC pages use the Part 2 API for all application data operations.
- The MVC project contains no Entity Framework Core or direct database access.
- API session cookie is forwarded by the server-side API client.
- Organiser and Participant navigation is separated.
- Unauthenticated visitors are redirected away from protected MVC pages.
- API ownership and role checks remain enforced by the Part 2 API.

## 5. Run unit tests

From the repository root:

```powershell
dotnet test RaceDay.sln --configuration Release
```

The existing xUnit project tests the Part 2 API. The Part 3 workflow builds the MVC project and runs the API test suite. Add and run further MVC integration tests if required by your lecturer or your final design.

## 6. Docker

The repository contains Dockerfiles for both the API and MVC application, plus `docker-compose.yml` for SQL Server, API and MVC.

1. Copy `.env.example` to `.env`.
2. Replace `SA_PASSWORD` with a strong SQL Server password.
3. Set `AZURE_BLOB_CONNECTION_STRING` in `.env` if image upload is to be demonstrated.
4. Start the containers:

```powershell
docker compose up --build -d
```

5. Wait for SQL Server to finish starting.
6. Connect to the container SQL Server from SSMS using `localhost,1433`, login `sa`, and the password in `.env`.
7. Run the Part 1 SQL script against that new container database to create `RaceDayDB`.
8. Open the MVC site at `http://localhost:8080`.
9. The API is available at `http://localhost:8081/swagger`.

Stop the stack:

```powershell
docker compose down
```

To remove the SQL Server data volume as well (this permanently deletes the container database):

```powershell
docker compose down -v
```

The Docker compose stack uses an isolated SQL Server container. Do not use the destructive Part 1 SQL script on an existing database with data you want to keep.

## 7. GitHub Actions and Part 3 commits

The workflow `.github/workflows/part3-ci.yml` restores and builds the complete solution and runs the API unit tests on every push and pull request.

The POE requires at least **20 meaningful Part 3 commits**. Make and push your own commits as you implement, understand and verify each feature. Do not create artificial commits or count cosmetic-only changes. The lecturer's repository link is the single repository for all parts; continue in that repository and follow the lecturer's required branch/subfolder arrangement.

After pushing:
1. Open the repository's Actions tab.
2. Open the latest RaceDay Part 3 CI run.
3. Confirm the build and tests are green.
4. Capture your own screenshot of the successful run and add it to the README before final submission.

## 8. Part 3 demonstration video

Record your own voice and screen. AI-generated voices are not permitted by the question paper. Demonstrate:
1. The running MVC website and consistent RaceDay styling.
2. Registration and login for both roles.
3. The Organiser dashboard, event CRUD, banner upload, categories, enrolments and results.
4. The Participant event discovery, event details, category enrolment, personal enrolments, results and profile picture.
5. How MVC calls the API and forwards the session cookie.
6. Azure Blob containers and stored images.
7. Docker images/containers and the running application.
8. The GitHub Actions green build.

Add the unlisted YouTube demonstration links for all three parts here before final submission:

- Part 1: `[Add your unlisted Part 1 v
- Part

A




## 9. AI-use declaration

The POE requires disclosure if AI tools were used and warns against submitting AI-generated code without analysis, synthesis and critical thinking. Review every file, understand the implementation, run it on your own computer, make your own decisions and adapt it to your approved Part 1 and Part 2 work. Add a truthful declaration describing the AI assistance you actually used, for example:

> AI tools were used as a development support resource for planning, code review and troubleshooting. I reviewed, tested and adapted the resulting implementation, and I take responsibility for understanding and explaining the submitted work.

Edit this statement so it accurately reflects your actual contribution and the assistance you received.

## 10. Important verification status

This ZIP is a prepared Part 3 development package based on the supplied Part 2 source and the Part 3 requirements in the PROG6212 question paper. It has **not** been built or run in your Visual Studio/SQL Server/Azure/Docker environment. Before submission, you must run restore, build, tests, database setup, API/MVC integration checks, Azure upload checks, Docker checks and GitHub Actions. Only submit after those checks succeed and you have completed your own required commits, video and evidence.
