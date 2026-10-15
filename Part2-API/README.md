# RaceDay – Part 2: RESTful API

RaceDay is a South African running, walking and cycling event management platform. This part is the **ASP.NET Core Web API** that powers the whole system: it handles all data operations, enforces role-based access for **Organisers** and **Participants**, and is covered by automated unit tests that run on every push through GitHub Actions.

## Video Presentation

[Watch the Part 2 video walkthrough](YOUTUBE_LINK_HERE)

## Tech Stack

- ASP.NET Core Web API (.NET 10), C#
- Entity Framework Core 10 (Code-First) with SQL Server
- Session-based authentication, with passwords hashed using ASP.NET Core's `PasswordHasher`
- Swagger UI (Swashbuckle) with XML documentation
- xUnit integration tests (`WebApplicationFactory` and the EF Core in-memory provider)
- GitHub Actions for CI

## Repository Structure

```
raceday-part1/
├── README.md                         <- Part 1 README
├── docs/                             <- Part 1 deliverables (ERD, endpoint plan, SQL script)
├── .github/workflows/
│   ├── validate-docs.yml             <- Part 1 structure check
│   └── part2-ci.yml                  <- Part 2 build and test workflow
└── Part2-API/
    ├── README.md                     <- you are here
    ├── docs/ci-build-green.png       <- green CI build screenshot
    └── RaceDay/
        ├── RaceDay.slnx
        ├── RaceDay.Api/
        │   ├── Controllers/          <- Auth, Users, Events, Categories, Entries, Results
        │   ├── DTOs/                 <- request and response shapes
        │   ├── Data/                 <- RaceDayContext (EF Core)
        │   ├── Filters/              <- SessionAuthorize role filter
        │   ├── Migrations/
        │   ├── Models/               <- entities matching the Part 1 ERD
        │   ├── Program.cs
        │   └── appsettings.json
        └── RaceDay.Tests/            <- xUnit tests
```

## Setup and Run Instructions

**Prerequisites:** .NET 10 SDK, SQL Server (LocalDB or SQL Server Express), and Visual Studio 2026 (or VS Code with the C# extension).

1. **Clone the repository and switch to the Part 2 branch**

   ```
   git clone https://github.com/Telezi5/raceday-part1.git
   cd raceday-part1
   git checkout part2-restful-api
   ```

2. **Open the solution:** `Part2-API/RaceDay/RaceDay.slnx` in Visual Studio.
3. **Check the connection string** in `RaceDay.Api/appsettings.json`. It uses LocalDB by default: `Server=(localdb)\MSSQLLocalDB;Database=RaceDayApiDB;...`. If you use SQL Express, change the `Server=` part (for example `.\SQLEXPRESS`).
4. **Create the database:** open **Tools → NuGet Package Manager → Package Manager Console**, set the default project to `RaceDay.Api`, and run `Update-Database`.
5. **Run the API:** press **F5** with the `https` profile. Swagger opens at `https://localhost:7110/swagger`.
6. **Register accounts in Swagger:** use `POST /api/auth/register`. Registering as an **Organiser** needs the organiser key `RACEDAY-ORG-2026` (set in `appsettings.json`).
7. **Run the tests:** use **Test → Run All Tests** in Visual Studio, or run `dotnet test Part2-API/RaceDay/RaceDay.slnx`. The tests use an in-memory database, so they need no SQL Server.

## The Two Roles

| | Organiser | Participant |
|---|---|---|
| Registers with | role `Organiser` and the secret organiser key | role `Participant` (no key) |
| Events | create, update and delete **their own** events | view events |
| Categories | add, update and delete categories on their own events | view categories |
| Enrolments | view all entries for their own events | enter a category, cancel their own entry, view their own entry history |
| Results | record and correct results for their own events | view their own results |
| Profile | view and update own profile | view and update own profile |

**Why an organiser key?** The brief lets users choose their role at registration. If anyone could pick Organiser, any visitor could create and manage events. Requiring a secret key for the Organiser role keeps role selection under control while still letting users choose.

## Authentication and Sessions

- `POST /api/auth/login` verifies the password hash and stores the user's **ID and role in the server-side session**. A session cookie then identifies the user on later requests.
- A reusable `[SessionAuthorize]` filter protects endpoints. It returns **401** if the user isn't logged in and **403** if their role is wrong.
- Ownership is also enforced: an Organiser can only change their own events, and a Participant can only cancel their own entries.
- Passwords are hashed with ASP.NET Core's `PasswordHasher` and never stored or returned in their original form.

## Endpoint Summary (22 endpoints)

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Logged in |
| GET | `/api/users/me` | Logged in |
| PUT | `/api/users/me` | Logged in |
| GET | `/api/users/me/entries` | Participant |
| GET | `/api/events` | Public |
| GET | `/api/events/{id}` | Public |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{id}` | Organiser (owner) |
| DELETE | `/api/events/{id}` | Organiser (owner) |
| GET | `/api/events/{eventId}/categories` | Public |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) |
| PUT | `/api/categories/{id}` | Organiser (owner) |
| DELETE | `/api/categories/{id}` | Organiser (owner) |
| POST | `/api/categories/{categoryId}/entries` | Participant |
| GET | `/api/events/{eventId}/entries` | Organiser (owner) |
| DELETE | `/api/entries/{id}` | Participant (owner) |
| POST | `/api/entries/{entryId}/result` | Organiser (owner) |
| PUT | `/api/results/{id}` | Organiser (owner) |
| GET | `/api/entries/{entryId}/result` | Participant (owner) or Organiser (owner) |
| GET | `/api/categories/{categoryId}/results` | Public |

Every endpoint is visible and testable in Swagger, with a description of its purpose, expected input and responses.

## Database

The schema follows the Part 1 ERD and SQL script: **Users, Events, Categories, Venues, Entries and Results**, with the same keys, unique rules and check constraints, created with EF Core Code-First migrations. The API database is named `RaceDayApiDB` so it doesn't clash with the Part 1 `RaceDayDB`.

## Unit Tests

55 tests in `RaceDay.Tests` start the real API in memory (real routing, real session cookies and the real role filter) with an in-memory database.

| Test class | Tests | Covers |
|---|---|---|
| `AuthTests` | 14 | registration, login, logout, profile and the organiser key |
| `EventTests` | 17 | event and category management, ownership, drafts and role checks |
| `EnrolmentTests` | 14 | enrolling, the participant-event-category link, duplicates, full categories, cancelling and role checks |
| `ResultTests` | 10 | recording, correcting and viewing results, and the public leaderboard |

They cover both expected successes and expected failures:

- **Registration and login:** success, duplicate email, invalid role, short password, missing or wrong organiser key, wrong password, unknown email, logout
- **Event management:** Organisers create, update and delete; invalid types rejected; ownership enforced; drafts hidden from the public
- **Role enforcement:** Participants are rejected from Organiser actions (create events and categories, list entries, record results) and Organisers from Participant actions (enrol)
- **Enrolments:** a Participant can enrol, the link between participant, event and category is recorded in the database, and duplicate and full-category cases are rejected
- **Results:** recording, correcting and viewing results, and the public leaderboard

All tests pass locally and in GitHub Actions.

## Git and GitHub

Part 2 is developed on the `part2-restful-api` branch, inside the `Part2-API` folder, with 30+ meaningful commits made from my own GitHub account. Both GitHub Actions workflows (the Part 1 structure check and the Part 2 build and test) pass on every push.

## CI/CD

The workflow in `.github/workflows/part2-ci.yml` builds the solution and runs all unit tests on every push.

![Green CI build](docs/ci-build-green.png)

## Changes from Part 1

The Part 2 brief required some small changes to the Part 1 plan:

- **Sessions instead of JWT.** The brief requires session management, so login stores the user in a server-side session. A `POST /api/auth/logout` endpoint was added, which brings the total to 22 endpoints.
- **`EventType` and `DistanceKM` added to Events.** The brief says every event must capture an event type (run, walk or cycle) and a distance. Per-category distances remain on Categories.
- **Event visibility.** Draft events are visible only to the Organiser who owns them. All other events are public.
- **Database name.** `RaceDayApiDB` replaces `RaceDayDB`.
- **Seed data.** Accounts are created through the registration endpoint so that passwords are properly hashed.
- **Bib numbers** are generated as `RD` plus the six-digit entry number. Finish times use the format `hh:mm:ss`.
