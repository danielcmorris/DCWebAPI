# DC Electric - Quickbase Field Mappings

Generated on: 2026-01-22

This document maps Quickbase field IDs to human-readable names for all tables used in the Street Lights application.

## Street Lights App (`bjrvqd33c`)

---

### Tickets (`bjrvqd33t`)

The main work order/trouble ticket table.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | ✅ `select` | `RecordId` |
| 4 | Record Owner | user | | | |
| 5 | Last Modified By | user | | | |
| 7 | Service Type | text-multiple-choice | | ✅ `select` | `ServiceType` |
| 8 | Problem Type | text-multiple-choice | | ✅ `select` | `ProblemType` |
| 9 | Response Time | text-multiple-choice | | | |
| 10 | Details | text-multi-line | | ✅ `select` | `Details` |
| 11 | Supporting File | file | | | |
| 12 | Status | text-multiple-choice | | | |
| 13 | Due Date | date | | | |
| 14 | Actual Resolution Date / Time | timestamp | | | |
| 15 | Ticket Age | duration | formula | | |
| 16 | Analysis | text-multi-line | | ✅ `select` | `Analysis` |
| 17 | Status Change Log | text | | | |
| 18 | Customer Name | text | | ✅ `where` | (filter only) |
| 19 | Customer Phone | phone | lookup | | |
| 20 | Customer Mobile | phone | lookup | | |
| 21 | Caller Type | text-multiple-choice | | ✅ `select` | `CallerType` |
| 22 | Assigned Team Member | user | | | |
| 23 | Related Caller | numeric | | | |
| 25 | Date / Time Ticket Opened | timestamp | | ✅ `select` | `DateTimeOpened` |
| 26 | Created By | user | | | |
| 27 | Ticket ID | text | formula | ✅ `select` | `TicketId` |
| 42 | Due Time | timeofday | | | |
| 43 | Start Date | date | | ✅ `select` | `StartDate` |
| 44 | Start Time | timeofday | | ✅ `select` | `StartTime` |
| 45 | Completion Date | date | | ✅ `select`, `where` | `CompletionDate` |
| 46 | Completion Time | timeofday | | ✅ `select` | `CompletionTime` |
| 47 | Labor | dblink | | | |
| 50 | Follow-up Needed? | checkbox | | | |
| 56 | Completed By | text-multiple-choice | | ✅ `select` | `Technician` |
| 76 | Description of Follow-Up Needed | text-multi-line | | | |
| 77 | File Attachment? | checkbox | | | |
| 78 | Emergency Response Time | text-multiple-choice | | | |
| 99 | Caller Name | text | lookup | ✅ `select` | `CallerName` |
| 100 | Caller Phone Number | phone | lookup | | |
| 101 | E-Mail Address | email | lookup | | |
| 102 | Related Location | numeric | | | |
| 105 | Fixture Type | text | lookup | ✅ `select` | `FixtureType` |
| 110 | Materials | dblink | | | |
| 112 | Equipment | dblink | | | |
| 114 | Labor2 | dblink | | | |
| 119 | Job # | text | | ✅ `select` | `JobNumber` |
| 143 | Location - Address | text | lookup | ✅ `select` | `LocationAddress` |
| 144 | Location - Major Street | text | lookup | | |
| 145 | Location - Cross Street | text | lookup | ✅ `select` | `LocationCross` |
| 151 | Street Light Number | text | lookup | ✅ `select` | `StreetLightNumber` |
| 167 | Not Billable | checkbox | | ✅ `where` | (filter) |
| 204 | Billable Override | checkbox | | ✅ `select` | `BillableOverride` |

**Query Example:**
```csharp
q.from = "bjrvqd33t";
q.select = new List<int>() { 119, 27, 99, 21, 105, 151, 143, 145, 25, 7, 8, 10, 167, 204, 43, 44, 45, 46, 56, 16, 3 };
q.where = $"{{18.EX.'{customerName}'}}AND{{45.GTE.'{start}'}}AND{{45.LTE.'{end}'}}AND{{167.EX.'false'}}";
```

---

### Customers (`bjrvqd33q`)

Customer master data.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | ✅ `select` | `RecordID` |
| 6 | Customer Name | text | | ✅ `select`, `where` | `CustomerName` |
| 7 | Address | text | | ✅ `select` | `Address` |
| 8 | City | text | | ✅ `select` | `City` |
| 9 | State | text-multiple-choice | | ✅ `select` | `State` |
| 10 | Zip | text | | ✅ `select` | `Zip` |
| 11 | Phone | phone | | ✅ `select` | `Phone` |
| 12 | Mobile | phone | | ✅ `select` | `Mobile` |
| 13 | Fax | phone | | ✅ `select` | `Fax` |
| 14 | Email | email | | ✅ `select` | `Email` |
| 15 | Web | url | | ✅ `select` | `Web` |
| 17 | Customer Full Address | text | formula | ✅ `select` | `CustomerFullAddress` |
| 30 | Intersections | text | | ✅ `select` | `Intersections` |
| 58 | Main Contact | text | | ✅ `select` | `MainContact` |
| 62 | Group Pricing Level | text-multiple-choice | | ✅ `select` | `GroupPricingLevel` |
| 76 | Customer has Billing Divisions? | checkbox | | ✅ `select` | `HasBillingDivisions` |
| 78 | Divisions | dblink | | ✅ `select` | `Divisions` |

**Query Example:**
```csharp
q.from = "bjrvqd33q";
q.select = new List<int>() { 3, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 30, 58, 62, 76, 78 };
q.where = $"{{6.EX.'{customerName}'}}";
```

---

### Labor Line Items (`bjrvqd34z`)

Individual labor entries linked to tickets.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | | |
| 6 | Date | date | | ✅ `select` | `LaborDate` |
| 7 | Hours | numeric | | ✅ `select` | `Hours` |
| 8 | Type of Hours | text-multiple-choice | | ✅ `select` | `TypeOfHours` |
| 9 | Team Member | user | | ✅ `select` | `TeamMember` |
| 10 | Related Ticket | numeric | | | |
| 11 | Ticket ID | text | lookup | ✅ `select`, `where` | `TicketID` |
| 12 | Type of Labor | text-multiple-choice | | ✅ `select` | `TypeOfLabor` |

**Query Example:**
```csharp
q.from = "bjrvqd34z";
q.select = new List<int>() { 6, 7, 8, 9, 11, 12 };
q.where = "{11.EX.'TKT-001'}OR{11.EX.'TKT-002'}";  // Multiple tickets
```

---

### Material Line Items (`bjrvqd34t`)

Individual material/inventory entries linked to tickets.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | | |
| 6 | Quantity | numeric | | ✅ `select` | `Quantity` |
| 7 | Product Type | text | | | |
| 8 | Product Sub-type | text | | | |
| 12 | Related Ticket | numeric | | | |
| 13 | Ticket ID | text | lookup | ✅ `select`, `where` | `TicketID` |
| 24 | Item ID | text | | ✅ `select` | `ItemID` |
| 25 | Item Description | text | lookup | ✅ `select` | `ItemDescription` |
| 26 | Item ID - List Price | currency | lookup | ✅ `select` | `ItemIDListPrice` |
| 27 | Unit of Measurement | text | lookup | ✅ `select` | `UnitOfMeasurement` |
| 32 | Non-Inventory Material | checkbox | | ✅ `select` | `NonInventoryMaterial` |
| 35 | Non-Inventory Material SALE Price | currency | | ✅ `select` | `NonInventoryMaterialSALEPrice` |
| 36 | Material Description CALC | text | formula | ✅ `select` | `MaterialDescriptionCALC` |

**Query Example:**
```csharp
q.from = "bjrvqd34t";
q.select = new List<int>() { 13, 24, 32, 36, 35, 6, 27, 25, 26 };
q.where = "{13.EX.'TKT-001'}OR{13.EX.'TKT-002'}";
```

---

### Equipment Line Items (`bjrvqd34w`)

Equipment usage entries linked to tickets.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | | |
| 6 | Hours | numeric | | ✅ `select` | `Hours` |
| 7 | Date | date | | | |
| 8 | Related Equipment | numeric | | | |
| 9 | Equipment | text | lookup | ✅ `select` | `Equipment` |
| 10 | Related Ticket | numeric | | | |
| 11 | Ticket ID | text | lookup | ✅ `select`, `where` | `TicketID` |

**Query Example:**
```csharp
q.from = "bjrvqd34w";
q.select = new List<int>() { 11, 9, 6 };
q.where = "{11.EX.'TKT-001'}";
```

---

### Labor Pricing (`bjrvqd346`)

Customer-specific labor rates.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | | |
| 6 | Customer Name | text-multiple-choice | | ✅ `select`, `where` | `CustomerName` |
| 7 | Type of Labor | text-multiple-choice | | ✅ `select` | `TypeOfLabor` |
| 8 | Type of Hours | text-multiple-choice | | ✅ `select` | `TypeOfHours` |
| 9 | Labor Price | currency | | ✅ `select` | `LaborPrice` |

**Query Example:**
```csharp
q.from = "bjrvqd346";
q.select = new List<int>() { 6, 7, 8, 9 };
q.where = $"{{6.EX.'{customerName}'}}";
```

---

### Material Pricing (`bjrvqd343`)

Customer/pricing-level specific material prices.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | | |
| 8 | Sell Price | currency | | ✅ `select` | `SellPrice` |
| 14 | Group Pricing Level | text-multiple-choice | | ✅ `select` | `GroupPricingLevel` |
| 27 | Item ID | text | | ✅ `select`, `where` | `ItemID` |
| 32 | Lump Sum | checkbox | | ✅ `select` | `LumpSum` |

**Query Example:**
```csharp
q.from = "bjrvqd343";
q.select = new List<int>() { 27, 8, 14, 32 };
q.where = "{27.EX.'ITEM001'}OR{27.EX.'ITEM002'}";
```

---

### Equipment Pricing (`bjrvqd347`)

Customer-specific equipment rates.

| Field ID | Label | Type | Mode | Used In Code | C# Property |
|----------|-------|------|------|--------------|-------------|
| 1 | Date Created | timestamp | | | |
| 2 | Date Modified | timestamp | | | |
| 3 | Record ID# | recordid | | | |
| 6 | Customer Name | text-multiple-choice | | ✅ `select`, `where` | `CustomerName` |
| 7 | Equipment | text-multiple-choice | | ✅ `select`, `where` | `Equipment` |
| 8 | Rate Type | text-multiple-choice | | | |
| 9 | Price | currency | | ✅ `select` | `Price` |

**Query Example:**
```csharp
q.from = "bjrvqd347";
q.select = new List<int>() { 11, 9, 6, 7 };
q.where = $"{{6.EX.'{customerName}'}}AND({{7.EX.'Bucket Truck'}}OR{{7.EX.'Crane'}})";
```

---

## All Tables in Street Lights App

| Table Name | Table ID | Description |
|------------|----------|-------------|
| Customers | `bjrvqd33q` | Customer master data |
| Callers | `bjrvqd33s` | Phone call notes |
| Tickets | `bjrvqd33t` | Work orders/trouble tickets |
| Documents | `bjrvqd336` | Document storage |
| Team Members | `bjrvqd337` | Employee records |
| Locations | `bjrvqd338` | Street light locations |
| Materials | `bjrvqd339` | Material inventory |
| Product Types | `bjrvqd34j` | Product categorization |
| Product Sub-types | `bjrvqd34n` | Product sub-categories |
| Material Line Items | `bjrvqd34t` | Materials used on tickets |
| Equipment | `bjrvqd34v` | Equipment master list |
| Equipment Line Items | `bjrvqd34w` | Equipment used on tickets |
| Labor Line Items | `bjrvqd34z` | Labor entries on tickets |
| Maintenance Schedules | `bjrvqd342` | Routine maintenance |
| Material Pricing | `bjrvqd343` | Material prices by customer |
| Labor Pricing | `bjrvqd346` | Labor rates by customer |
| Equipment Pricing | `bjrvqd347` | Equipment rates by customer |
| Maintenance Pricing | `bjrvqd35a` | Maintenance pricing |
| Group Pricing Level | `bj3w2ti62` | Pricing tier definitions |
| Client Access | `bkqp4qwin` | Portal access control |
| Night Checks | `bk2wgw2qv` | Night inspection records |
| Customer Division Names | `bmwe2vm9x` | Billing divisions |

---

## Quick Reference: Field IDs by Usage

### Tickets Query (Main Report)
```
select: 3, 7, 8, 10, 16, 21, 25, 27, 43, 44, 45, 46, 56, 99, 105, 119, 143, 145, 151, 167, 204
where: 18 (Customer Name), 45 (Completion Date), 167 (Not Billable)
```

### Customer Lookup
```
select: 3, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 30, 58, 62, 76, 78
where: 6 (Customer Name)
```

### Labor by Ticket
```
select: 6, 7, 8, 9, 11, 12
where: 11 (Ticket ID)
```

### Materials by Ticket
```
select: 6, 13, 24, 25, 26, 27, 32, 35, 36
where: 13 (Ticket ID)
```

### Equipment by Ticket
```
select: 6, 9, 11
where: 11 (Ticket ID)
```
