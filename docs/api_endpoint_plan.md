# RaceDay – API Endpoint Plan
### Part 1, Section B — submitted to `/docs/api_endpoint_plan.md`

This plan covers all functional areas required by the brief: **Authentication, User Profile, Events, Categories, Event Enrolments, and Results.** It is designed to match the ERD in `/docs/raceday_erd.png` exactly — every route and body field maps to a real column on a real entity, and there are no endpoints here that don't correspond to something in the data model.

**Roles used below:**
- `None` — public, no login required
- `Any` — any logged-in user (Organiser or Participant)
- `Organiser` — logged-in user with Role = Organiser
- `Participant` — logged-in user with Role = Participant

---

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new user account as either an Organiser or a Participant. | None | `{ fullName, email, password, role, contactNumber }` | 201 Created – user object (no password); 400 Bad Request – missing/invalid fields; 409 Conflict – email already registered |
| POST | `/api/auth/login` | Authenticates a user and issues a JWT for subsequent requests. | None | `{ email, password }` | 200 OK – `{ token, user }`; 401 Unauthorized – invalid credentials |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the profile of the currently logged-in user. | Any | None | 200 OK – user object; 401 Unauthorized – no/invalid token |
| PUT | `/api/users/me` | Updates the logged-in user's own profile details. | Any | `{ fullName, contactNumber }` | 200 OK – updated user object; 400 Bad Request – invalid fields; 401 Unauthorized |
| GET | `/api/users/me/entries` | Returns the logged-in Participant's own entry & performance history (past and upcoming races). | Participant | None | 200 OK – array of entries with linked category, event and result data; 401 Unauthorized |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Lists all published events. Supports optional query filters (e.g. `?city=`, `?date=`). | None | None | 200 OK – array of event objects |
| GET | `/api/events/{id}` | Returns full details for a single event, including its venue/route info. | None | None | 200 OK – event object; 404 Not Found – event does not exist |
| POST | `/api/events` | Creates a new event. The logged-in Organiser becomes the `OrganiserID`. | Organiser | `{ eventName, eventDate, city, description, status, venue: { startPoint, endPoint, routeDescription, elevationGainM } }` | 201 Created – event object; 400 Bad Request – validation error; 403 Forbidden – not an Organiser |
| PUT | `/api/events/{id}` | Updates an event. Only the Organiser who created it may edit it. | Organiser | `{ eventName, eventDate, city, description, status }` | 200 OK – updated event; 403 Forbidden – not the owning Organiser; 404 Not Found |
| DELETE | `/api/events/{id}` | Deletes/cancels an event owned by the logged-in Organiser. | Organiser | None | 204 No Content; 403 Forbidden – not the owning Organiser; 404 Not Found |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Lists all race categories belonging to a given event. | None | None | 200 OK – array of category objects; 404 Not Found – event does not exist |
| POST | `/api/events/{eventId}/categories` | Adds a new category (e.g. 10km, 21km) to an event owned by the logged-in Organiser. | Organiser | `{ categoryName, distanceKM, entryFee, maxParticipants }` | 201 Created – category object; 400 Bad Request; 403 Forbidden – not the owning Organiser; 404 Not Found – event does not exist |
| PUT | `/api/categories/{id}` | Updates a category's details. | Organiser | `{ categoryName, distanceKM, entryFee, maxParticipants }` | 200 OK – updated category; 403 Forbidden; 404 Not Found |
| DELETE | `/api/categories/{id}` | Removes a category from an event. | Organiser | None | 204 No Content; 403 Forbidden; 404 Not Found; 409 Conflict – category already has entries |

## 5. Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/categories/{categoryId}/entries` | Enters the logged-in Participant into a category. Generates a bib number and links `ParticipantID` + `CategoryID`. | Participant | `{ paymentStatus }` | 201 Created – entry object with bib number; 400 Bad Request; 404 Not Found – category does not exist; 409 Conflict – already entered, or category full |
| GET | `/api/events/{eventId}/entries` | Lists all entries/participants across an event, for the Organiser managing it. | Organiser | None | 200 OK – array of entries; 403 Forbidden – not the owning Organiser; 404 Not Found |
| DELETE | `/api/entries/{id}` | Cancels the logged-in Participant's own entry. | Participant | None | 204 No Content; 403 Forbidden – not the entry owner; 404 Not Found |

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/entries/{entryId}/result` | Captures a race result for a specific entry (finish time, position). | Organiser | `{ finishTime, position, status }` | 201 Created – result object; 400 Bad Request; 403 Forbidden – not the event's Organiser; 404 Not Found – entry does not exist; 409 Conflict – result already recorded |
| PUT | `/api/results/{id}` | Corrects an already-captured result. | Organiser | `{ finishTime, position, status }` | 200 OK – updated result; 403 Forbidden; 404 Not Found |
| GET | `/api/entries/{entryId}/result` | Returns the result for one entry, visible to the entry owner or the event's Organiser. | Any | None | 200 OK – result object; 403 Forbidden – not owner/organiser; 404 Not Found – no result yet |
| GET | `/api/categories/{categoryId}/results` | Returns the full public leaderboard/results list for a category. | None | None | 200 OK – array of results ordered by position; 404 Not Found – category does not exist |

---

**Total: 22 endpoints across all 6 required functional areas.**
