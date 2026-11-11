# Part 2 Excellence Checklist

This checklist maps the implementation to the Part 2 rubric.

- [x] Correct submission structure and detailed README
- [x] ASP.NET Core Web API project
- [x] EF Core Code-First model
- [x] SQL Server provider
- [x] Six Part 1 entities preserved
- [x] Register and login
- [x] Server-side session stores UserId and Role
- [x] Passwords hashed, never stored as plaintext
- [x] Organiser-only event/category/result administration
- [x] Participant-only enrolment and personal result history
- [x] Ownership checks prevent one organiser editing another organiser's records
- [x] Events and categories publicly readable
- [x] Profile read/update
- [x] Image endpoints prepared for Part 3 Azure Blob integration
- [x] Swagger UI
- [x] Swagger session-cookie security definition
- [x] Unit tests for authentication
- [x] Unit tests for organiser/participant access control
- [x] Unit tests for enrolment
- [x] Unit tests for results
- [x] GitHub Actions restore/build/test
- [x] No secrets committed to source control

## Evidence to capture

1. Visual Studio solution builds successfully.
2. Swagger UI showing all endpoints.
3. Register response = 201.
4. Database Users table showing PasswordHash rather than plaintext.
5. Login response = 200 and session cookie in browser developer tools.
6. Participant receiving 403 when attempting organiser event creation.
7. Organiser successfully creating an event.
8. Participant successfully enrolling.
9. Organiser successfully capturing a result.
10. `dotnet test` showing all tests passed.
11. GitHub Actions showing a green check.
12. GitHub commit history showing at least 20 meaningful Part 2 commits.

## Important

Part 1 requires a minimum of 20 meaningful commits per part, and the Part 2 rubric awards full GitHub/CI marks at 20+ meaningful commits with a green build. Do not manufacture meaningless commits. Make genuine incremental changes and explain them in the commit messages.
