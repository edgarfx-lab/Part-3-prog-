/* ==========================================================================
   RaceDay - Database Creation Script
   Module: PROG6212/w - Programming 2B, POE Part 1
   Description: Creates the full RaceDay schema (matches the submitted ERD)
                and seeds it with realistic sample data.
   Target: Microsoft SQL Server (tested in SSMS on a clean instance)
   ========================================================================== */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* --------------------------------------------------------------------------
   Drop tables if they already exist (child -> parent order), so the script
   can be re-run cleanly during testing.
   -------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.EventTypes', 'U') IS NOT NULL DROP TABLE dbo.EventTypes;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* --------------------------------------------------------------------------
   1. Users
   Stores both Organisers and Participants. Role is restricted to the two
   valid values via a CHECK constraint, which enforces the two-role design
   at the database level in addition to the API layer.
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.Users
(
    UserId              INT IDENTITY(1,1)   NOT NULL,
    FirstName           NVARCHAR(100)       NOT NULL,
    LastName            NVARCHAR(100)       NOT NULL,
    Email               NVARCHAR(255)       NOT NULL,
    PasswordHash        NVARCHAR(256)       NOT NULL,
    Role                NVARCHAR(20)        NOT NULL,
    PhoneNumber         NVARCHAR(20)        NULL,
    ProfilePictureUrl   NVARCHAR(500)       NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

/* --------------------------------------------------------------------------
   2. EventTypes
   Lookup table so the event type (Run / Walk / Cycle) is normalised rather
   than stored as a free-text value on every Event row.
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.EventTypes
(
    EventTypeId     INT IDENTITY(1,1)  NOT NULL,
    TypeName        NVARCHAR(20)       NOT NULL,
    CONSTRAINT PK_EventTypes PRIMARY KEY (EventTypeId),
    CONSTRAINT UQ_EventTypes_TypeName UNIQUE (TypeName)
);
GO

/* --------------------------------------------------------------------------
   3. Events
   Each Event is created by exactly one Organiser (Users) and belongs to
   exactly one EventType.
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.Events
(
    EventId         INT IDENTITY(1,1)      NOT NULL,
    OrganiserId     INT                     NOT NULL,
    EventTypeId     INT                     NOT NULL,
    Name            NVARCHAR(150)           NOT NULL,
    Description     NVARCHAR(1000)          NULL,
    EventDate       DATETIME2               NOT NULL,
    Location        NVARCHAR(200)           NOT NULL,
    DistanceKm      DECIMAL(5,2)            NOT NULL,
    BannerImageUrl  NVARCHAR(500)           NULL,
    CreatedAt       DATETIME2               NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Events_EventType FOREIGN KEY (EventTypeId)
        REFERENCES dbo.EventTypes (EventTypeId),
    CONSTRAINT CK_Events_DistanceKm CHECK (DistanceKm > 0)
);
GO

/* --------------------------------------------------------------------------
   4. Categories
   Age or distance categories defined per Event (e.g. Under 20, 10km).
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.Categories
(
    CategoryId      INT IDENTITY(1,1)  NOT NULL,
    EventId         INT                NOT NULL,
    Name            NVARCHAR(100)      NOT NULL,
    Description     NVARCHAR(300)      NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE
);
GO

/* --------------------------------------------------------------------------
   5. Enrolments
   Links a Participant (Users) to an Event and the Category they entered.
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.Enrolments
(
    EnrolmentId     INT IDENTITY(1,1)  NOT NULL,
    ParticipantId   INT                NOT NULL,
    EventId         INT                NOT NULL,
    CategoryId      INT                NOT NULL,
    EnrolmentDate   DATETIME2          NOT NULL DEFAULT SYSDATETIME(),
    Status          NVARCHAR(20)       NOT NULL DEFAULT 'Pending',
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantId, EventId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled'))
);
GO

/* --------------------------------------------------------------------------
   6. Results
   One-to-one with Enrolments: a result can only exist once an enrolment
   exists, and each enrolment can have at most one result (UNIQUE FK).
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.Results
(
    ResultId        INT IDENTITY(1,1)  NOT NULL,
    EnrolmentId     INT                NOT NULL,
    FinishTime      TIME               NULL,
    FinishPosition  INT                NULL,
    TotalFinishers  INT                NULL,
    CapturedAt      DATETIME2          NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId),
    CONSTRAINT UQ_Results_EnrolmentId UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_FinishPosition CHECK (FinishPosition IS NULL OR FinishPosition > 0)
);
GO


/* ==========================================================================
   SEED DATA
   ========================================================================== */

-- Users: 2 Organisers, 2 Participants (passwords shown are placeholder
-- SHA-256 style hashes - real hashing is implemented in the Part 2 API)
INSERT INTO dbo.Users (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber, ProfilePictureUrl)
VALUES
('Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za', 'HASH_PLACEHOLDER_ORG1', 'Organiser', '0821234567', NULL),
('Lindiwe', 'Dlamini', 'lindiwe.dlamini@raceday.co.za', 'HASH_PLACEHOLDER_ORG2', 'Organiser', '0837654321', NULL),
('Johan', 'van der Merwe', 'johan.vdm@example.com', 'HASH_PLACEHOLDER_PART1', 'Participant', '0731112222', NULL),
('Naledi', 'Sithole', 'naledi.sithole@example.com', 'HASH_PLACEHOLDER_PART2', 'Participant', '0723334444', NULL);
GO

-- EventTypes
INSERT INTO dbo.EventTypes (TypeName)
VALUES ('Run'), ('Walk'), ('Cycle');
GO

-- Events: 3 events, each linked to an Organiser and an EventType
INSERT INTO dbo.Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm, BannerImageUrl)
VALUES
(1, (SELECT EventTypeId FROM dbo.EventTypes WHERE TypeName = 'Run'),
 'Groblersdal Gold Run', 'A community road race through Groblersdal supporting local youth sport development.',
 '2026-11-14 06:00:00', 'Groblersdal, Limpopo', 21.10, NULL),
(1, (SELECT EventTypeId FROM dbo.EventTypes WHERE TypeName = 'Walk'),
 'Limpopo Charity Walk', 'A family-friendly fun walk raising funds for local schools.',
 '2026-09-27 07:00:00', 'Polokwane, Limpopo', 5.00, NULL),
(2, (SELECT EventTypeId FROM dbo.EventTypes WHERE TypeName = 'Cycle'),
 'Highveld Cycle Classic', 'An annual road cycling event across the Mpumalanga highveld.',
 '2026-10-03 06:30:00', 'Middelburg, Mpumalanga', 94.00, NULL);
GO

-- Categories: at least one set per event
INSERT INTO dbo.Categories (EventId, Name, Description)
VALUES
(1, '21km Open', 'Open category for the half marathon distance'),
(1, 'Under 20', 'Junior category, athletes under 20 years old'),
(1, 'Senior 40+', 'Senior category for athletes 40 years and older'),
(2, '5km Fun Walk', 'Non-competitive fun walk category'),
(3, '94km Open', 'Open category for the full cycle route'),
(3, '45km Half Route', 'Shorter half-route category');
GO

-- Enrolments: participants entering events with a chosen category
INSERT INTO dbo.Enrolments (ParticipantId, EventId, CategoryId, Status)
VALUES
(3, 1, (SELECT CategoryId FROM dbo.Categories WHERE EventId = 1 AND Name = '21km Open'), 'Confirmed'),
(4, 1, (SELECT CategoryId FROM dbo.Categories WHERE EventId = 1 AND Name = 'Senior 40+'), 'Confirmed'),
(3, 3, (SELECT CategoryId FROM dbo.Categories WHERE EventId = 3 AND Name = '45km Half Route'), 'Pending'),
(4, 2, (SELECT CategoryId FROM dbo.Categories WHERE EventId = 2 AND Name = '5km Fun Walk'), 'Confirmed');
GO

-- Results: captured for the confirmed, already-run-style enrolments
INSERT INTO dbo.Results (EnrolmentId, FinishTime, FinishPosition, TotalFinishers)
VALUES
((SELECT EnrolmentId FROM dbo.Enrolments WHERE ParticipantId = 3 AND EventId = 1), '01:38:22', 47, 312),
((SELECT EnrolmentId FROM dbo.Enrolments WHERE ParticipantId = 4 AND EventId = 1), '02:05:10', 118, 312);
GO

/* ==========================================================================
   Quick verification queries (optional - comment out before grading if not
   required, kept here to demonstrate the script runs end-to-end)
   ========================================================================== */
-- SELECT * FROM dbo.Users;
-- SELECT * FROM dbo.EventTypes;
-- SELECT * FROM dbo.Events;
-- SELECT * FROM dbo.Categories;
-- SELECT * FROM dbo.Enrolments;
-- SELECT * FROM dbo.Results;
