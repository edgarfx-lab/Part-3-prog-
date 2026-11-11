# RaceDay - API Endpoint Plan

**Module:** PROG6212/w - Programming 2B, POE Part 1
**Purpose:** This plan lists every endpoint the RaceDay API will expose in Part 2, covering Authentication, User Profile, Events, Categories, Event Enrolments, and Results. Role-based access is enforced at the API level as required.

Legend for **Role Required**: `None` = public, `Any` = any authenticated user, `Organiser` / `Participant` = that role only.

---

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. Password is hashed before storage. | None | `{ firstName, lastName, email, password, role, phoneNumber }` | 201 Created - user created (no password returned) / 400 Bad Request - validation failed / 409 Conflict - email already registered |
| POST | /api/auth/login | Authenticates a user and starts a session storing their UserId and Role. | None | `{ email, password }` | 200 OK - session started, returns user id and role / 401 Unauthorized - invalid credentials |
| POST | /api/auth/logout | Ends the current user's session. | Any | None | 200 OK - session cleared |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's own profile information. | Any | None | 200 OK - profile object / 401 Unauthorized - no active session |
| PUT | /api/users/me | Updates the logged-in user's profile details (name, phone, etc.). | Any | `{ firstName, lastName, phoneNumber }` | 200 OK - updated profile / 400 Bad Request - validation failed |
| PUT | /api/users/me/profile-picture | Uploads or replaces the logged-in user's profile picture (stored via Azure Blob Storage in Part 3). | Any | `multipart/form-data: image file` | 200 OK - returns new image URL / 400 Bad Request - invalid file |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all events, with optional filtering (e.g. by date or event type). Visible to everyone. | None | None | 200 OK - array of events |
| GET | /api/events/{id} | Returns full details for a single event, including its categories. | None | None | 200 OK - event object / 404 Not Found - event does not exist |
| POST | /api/events | Creates a new event. The logged-in Organiser becomes the event's owner. | Organiser | `{ name, description, eventDate, location, distanceKm, eventTypeId }` | 201 Created - new event / 400 Bad Request - validation failed |
| PUT | /api/events/{id} | Updates an existing event. Only the owning Organiser may edit it. | Organiser | `{ name, description, eventDate, location, distanceKm, eventTypeId }` | 200 OK - updated event / 403 Forbidden - not the event owner / 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser | None | 204 No Content - deleted / 403 Forbidden - not the event owner / 404 Not Found |
| POST | /api/events/{id}/banner | Uploads or replaces the event's banner image (Azure Blob Storage, Part 3). | Organiser | `multipart/form-data: image file` | 200 OK - returns new banner URL / 403 Forbidden - not the event owner |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories available for a specific event. | None | None | 200 OK - array of categories / 404 Not Found - event does not exist |
| POST | /api/events/{eventId}/categories | Adds a new age or distance category to an event. | Organiser | `{ name, description }` | 201 Created - new category / 403 Forbidden - not the event owner |
| PUT | /api/categories/{id} | Updates an existing category's name or description. | Organiser | `{ name, description }` | 200 OK - updated category / 403 Forbidden - not the event owner / 404 Not Found |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser | None | 204 No Content - deleted / 403 Forbidden - not the event owner / 404 Not Found |

## 5. Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/enrolments | Enrols the logged-in Participant into the event under a chosen category. | Participant | `{ categoryId }` | 201 Created - enrolment record / 400 Bad Request - category does not belong to event / 409 Conflict - already enrolled |
| GET | /api/users/me/enrolments | Returns all events the logged-in Participant has enrolled for, with status. | Participant | None | 200 OK - array of enrolments |
| GET | /api/events/{eventId}/enrolments | Returns all Participants enrolled for a specific event, including category and status. | Organiser | None | 200 OK - array of enrolments / 403 Forbidden - not the event owner |
| PUT | /api/enrolments/{id}/status | Updates an enrolment's status (e.g. Confirmed, Cancelled). | Organiser | `{ status }` | 200 OK - updated enrolment / 403 Forbidden - not the event owner / 404 Not Found |

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures a finish time and position for a Participant's enrolment after the event. | Organiser | `{ finishTime, finishPosition, totalFinishers }` | 201 Created - result recorded / 403 Forbidden - not the event owner / 409 Conflict - result already exists for this enrolment |
| GET | /api/users/me/results | Returns the logged-in Participant's personal results history across all events. | Participant | None | 200 OK - array of results with event name, date, category, finish time and position |
| GET | /api/events/{eventId}/results | Returns all captured results for a specific event. | Organiser | None | 200 OK - array of results / 403 Forbidden - not the event owner |

---

### Notes
- All routes are prefixed `/api/` and use JSON request/response bodies (except image uploads, which use `multipart/form-data`).
- `Organiser` and `Participant` role checks are enforced server-side against the session, never trusted from the client.
- This plan will be implemented as closely as possible in Part 2; any deviation will be explained in that part's README.
