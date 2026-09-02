/* ============================================================
   RaceDay System - Database Schema
   Part 1, Section C - SQL Database Script
   Target: Microsoft SQL Server (SSMS)
   This script matches the ERD in /docs/raceday_erd.png exactly.
   ============================================================ */

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RaceDayDB')
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* ------------------------------------------------------------
   Drop tables if they already exist (in FK-safe order) so this
   script can be re-run cleanly on the same instance.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Entries', 'U') IS NOT NULL DROP TABLE dbo.Entries;
IF OBJECT_ID('dbo.Venues', 'U') IS NOT NULL DROP TABLE dbo.Venues;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* ============================================================
   TABLE: Users
   Holds both Organisers and Participants, distinguished by Role.
   ============================================================ */
CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        VARCHAR(100)    NOT NULL,
    Email           VARCHAR(100)    NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255)    NOT NULL,
    Role            VARCHAR(20)     NOT NULL DEFAULT 'Participant'
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant')),
    ContactNumber   VARCHAR(20)     NULL,
    DateRegistered  DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* ============================================================
   TABLE: Events
   Created by an Organiser (Users.Role = 'Organiser').
   ============================================================ */
CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    EventName       VARCHAR(150)    NOT NULL,
    EventDate       DATE            NOT NULL,
    City            VARCHAR(100)    NOT NULL,
    Description     TEXT            NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Draft'
                        CONSTRAINT CK_Events_Status CHECK (Status IN ('Draft', 'Published', 'Completed')),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES dbo.Users(UserID)
);
GO

/* ============================================================
   TABLE: Categories
   Race categories/distances within an Event (e.g. 10km, 21km).
   ============================================================ */
CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    CategoryName    VARCHAR(50)     NOT NULL,
    DistanceKM      DECIMAL(5,2)    NOT NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0.00,
    MaxParticipants INT             NOT NULL DEFAULT 100,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID)
        REFERENCES dbo.Events(EventID)
);
GO

/* ============================================================
   TABLE: Venues
   Route/location details for an Event. One venue record per
   Event (1:1), matching the ERD.
   ============================================================ */
CREATE TABLE dbo.Venues (
    VenueID          INT IDENTITY(1,1) PRIMARY KEY,
    EventID          INT             NOT NULL UNIQUE,
    StartPoint       VARCHAR(150)    NOT NULL,
    EndPoint         VARCHAR(150)    NOT NULL,
    RouteDescription TEXT            NULL,
    ElevationGainM   INT             NOT NULL DEFAULT 0,
    CONSTRAINT FK_Venues_Events FOREIGN KEY (EventID)
        REFERENCES dbo.Events(EventID)
);
GO

/* ============================================================
   TABLE: Entries
   A Participant entering a Category. One row per registration.
   ============================================================ */
CREATE TABLE dbo.Entries (
    EntryID         INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    BibNumber       VARCHAR(10)     NOT NULL UNIQUE,
    EntryDate       DATETIME        NOT NULL DEFAULT GETDATE(),
    PaymentStatus   VARCHAR(20)     NOT NULL DEFAULT 'Pending'
                        CONSTRAINT CK_Entries_PaymentStatus CHECK (PaymentStatus IN ('Pending', 'Paid', 'Refunded')),
    CONSTRAINT FK_Entries_Users FOREIGN KEY (ParticipantID)
        REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Entries_Categories FOREIGN KEY (CategoryID)
        REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Entries_Participant_Category UNIQUE (ParticipantID, CategoryID)
);
GO

/* ============================================================
   TABLE: Results
   One result per Entry (1 : 0..1), matching the ERD.
   ============================================================ */
CREATE TABLE dbo.Results (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EntryID         INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Finished'
                        CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DNS')),
    CONSTRAINT FK_Results_Entries FOREIGN KEY (EntryID)
        REFERENCES dbo.Entries(EntryID)
);
GO


/* ============================================================
   SEED DATA
   2 Organisers, 2 Participants, 3 Events, categories for each
   event, matching Venues, and sample Entries/Results.
   ============================================================ */

-- Users: 2 Organisers, 2 Participants
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, ContactNumber) VALUES
('Thabo Nkosi',    'thabo.nkosi@raceday.co.za',   'hashed_pw_001', 'Organiser',   '0821234567'),
('Sarah van Wyk',   'sarah.vanwyk@raceday.co.za',  'hashed_pw_002', 'Organiser',   '0837654321'),
('Lindiwe Dube',    'lindiwe.dube@example.com',    'hashed_pw_003', 'Participant', '0731112222'),
('James Botha',     'james.botha@example.com',     'hashed_pw_004', 'Participant', '0743334444');
GO

-- Events: 3 events, owned by the 2 organisers
INSERT INTO dbo.Events (OrganiserID, EventName, EventDate, City, Description, Status) VALUES
(1, 'Johannesburg City Run',   '2026-10-18', 'Johannesburg', 'Annual road running event through the Johannesburg CBD.', 'Published'),
(1, 'Soweto Heritage Marathon','2026-11-08', 'Soweto',       'Marathon and half marathon celebrating Soweto''s heritage routes.', 'Published'),
(2, 'Cape Town Coastal Cycle', '2026-11-22', 'Cape Town',    'Scenic cycling event along the Atlantic seaboard.', 'Draft');
GO

-- Venues: one per event (1:1)
INSERT INTO dbo.Venues (EventID, StartPoint, EndPoint, RouteDescription, ElevationGainM) VALUES
(1, 'Mary Fitzgerald Square', 'Ellis Park Stadium',   'City-centre loop through Newtown and Doornfontein.', 120),
(2, 'Soweto Theatre',         'FNB Stadium',           'Route passes Vilakazi Street and the Hector Pieterson Memorial.', 260),
(3, 'Sea Point Promenade',    'Camps Bay Beach',       'Flat coastal route along Victoria Road.', 80);
GO

-- Categories: at least one per event (2 each here)
INSERT INTO dbo.Categories (EventID, CategoryName, DistanceKM, EntryFee, MaxParticipants) VALUES
(1, '5km Fun Run',   5.00,  100.00, 500),
(1, '10km Race',     10.00, 150.00, 500),
(2, 'Half Marathon', 21.10, 250.00, 1000),
(2, 'Full Marathon', 42.20, 350.00, 800),
(3, '40km Cycle',    40.00, 200.00, 300),
(3, '80km Cycle',    80.00, 300.00, 300);
GO

-- Entries: sample enrolments across events for both participants
INSERT INTO dbo.Entries (ParticipantID, CategoryID, BibNumber, PaymentStatus) VALUES
(3, 2, 'JCR-1001', 'Paid'),      -- Lindiwe: 10km Race
(4, 2, 'JCR-1002', 'Paid'),      -- James: 10km Race
(3, 3, 'SHM-2001', 'Paid'),      -- Lindiwe: Half Marathon
(4, 5, 'CTC-3001', 'Pending');   -- James: 40km Cycle
GO

-- Results: sample results for the completed Johannesburg City Run entries
INSERT INTO dbo.Results (EntryID, FinishTime, Position, Status) VALUES
(1, '00:52:31', 1, 'Finished'),
(2, '00:55:47', 2, 'Finished');
GO
