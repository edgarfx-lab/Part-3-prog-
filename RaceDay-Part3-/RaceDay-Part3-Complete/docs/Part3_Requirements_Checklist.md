# PROG6212 Part 3 – Requirements and Verification Checklist

Use this checklist against the question paper pages 12–14.

## MVC application setup
- [ ] ASP.NET Core MVC application exists in `src/RaceDay.Mvc`.
- [ ] MVC reads/writes application data only through the Part 2 API.
- [ ] No direct database access or duplicated business rules in MVC.

## Authentication and role-based navigation
- [ ] Register through MVC for Organiser and Participant.
- [ ] Login through MVC calls Part 2 API.
- [ ] API session cookie is retained and forwarded on later API requests.
- [ ] MVC session maintains signed-in state and role.
- [ ] Distinct navigation for each role.
- [ ] Protected pages redirect unauthenticated users.
- [ ] Logout calls API logout and clears MVC session.

## Organiser
- [ ] Dashboard lists events created by the signed-in Organiser.
- [ ] Dashboard displays enrolment total per event.
- [ ] Dashboard displays upcoming event count/dates.
- [ ] Create, edit and delete events.
- [ ] Event fields: name, description, date, location, distance and type.
- [ ] Upload event banner through API.
- [ ] Manage event categories.
- [ ] View event participants, selected category and enrolment status.
- [ ] Update enrolment status.
- [ ] Capture finish times, positions and total finishers.

## Participant
- [ ] Browse upcoming events.
- [ ] Filter events.
- [ ] View event information and available categories.
- [ ] Enrol by selecting a category.
- [ ] View own enrolments and status.
- [ ] View personal history, event name/date/category, finish time and position.
- [ ] View and update personal profile.
- [ ] Upload profile picture through API.

## Azure Blob Storage
- [ ] Azure Blob Storage service is registered in the API.
- [ ] Event banners are stored in `event-banners`.
- [ ] Profile pictures are stored in `profile-pictures`.
- [ ] API updates the matching database image URL.
- [ ] MVC uploads go through API; MVC has no Azure SDK dependency.
- [ ] Verify image links in a browser and verify upload failure when Azure is unconfigured.

## Docker
- [ ] MVC Dockerfile builds and runs.
- [ ] API Dockerfile builds and runs.
- [ ] Docker Compose starts SQL Server, API and MVC.
- [ ] Execute Part 1 SQL script against the new container database.
- [ ] Verify the containerised website behaves like the local website.
- [ ] README contains Docker commands.

## GitHub and submission
- [ ] Continue in lecturer-provided repository.
- [ ] Make 20+ meaningful Part 3 commits from actual work.
- [ ] GitHub Actions builds the complete solution and runs tests.
- [ ] Green build screenshot added to README.
- [ ] GitHub repository link added to README.
- [ ] Unlisted video link added to README.
- [ ] Video uses student's own voice and explains MVC, Blob Storage and Docker.

## Scope note
The POE background describes live weather and route information, but the detailed Part 3 requirements on pages 12–14 do not specify a particular weather/route integration or API. Confirm with the lecturer whether these are expected as additional features.
