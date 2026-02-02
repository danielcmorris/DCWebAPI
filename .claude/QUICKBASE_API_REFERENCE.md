# Quickbase API Quick Reference

Base URL: `https://api.quickbase.com/v1`

## Required Headers

```http
QB-Realm-Hostname: {your-realm}.quickbase.com
Authorization: QB-USER-TOKEN {your-token}
Content-Type: application/json
```

---

## POST /records/query

**Query for data**

Pass in a query in the Quickbase query language. Returns record data with intelligent pagination based on the approximate size of each record.

**Request Body Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `from` | string | **Required.** The table identifier (e.g., "bjrvqd33t") |
| `select` | array | Array of field IDs to return. If empty, returns default fields only |
| `where` | string | Filter using Quickbase query language |
| `sortBy` | array | Array of `{fieldId, order}` objects. Order: "ASC" or "DESC" |
| `groupBy` | array | Array of `{fieldId, grouping}` objects |
| `options` | object | `{skip, top, compareWithAppLocalTime}` for pagination |

**Example Request:**
```json
{
  "from": "bck7gp3q2",
  "select": [1, 2, 3, 119, 27],
  "where": "{18.EX.'Customer Name'}AND{45.GTE.'01-01-2025'}",
  "sortBy": [{"fieldId": 4, "order": "ASC"}],
  "options": {"skip": 0, "top": 100}
}
```

**Example Response:**
```json
{
  "data": [
    {
      "119": {"value": "TKT-001"},
      "27": {"value": "Acme Corp"},
      "45": {"value": "2025-01-15"}
    }
  ],
  "fields": [
    {"id": 119, "label": "Ticket Number", "type": "text"},
    {"id": 27, "label": "Customer Name", "type": "text"},
    {"id": 45, "label": "Date", "type": "date"}
  ],
  "metadata": {
    "numRecords": 150,
    "numFields": 3,
    "skip": 0,
    "totalRecords": 150
  }
}
```

---

## Query Language Operators

| Operator | Meaning | Example |
|----------|---------|---------|
| `CT` | Contains | `{6.CT.'search text'}` |
| `XCT` | Does not contain | `{6.XCT.'exclude'}` |
| `EX` | Equals | `{6.EX.'exact match'}` |
| `XEX` | Does not equal | `{6.XEX.'not this'}` |
| `SW` | Starts with | `{6.SW.'prefix'}` |
| `XSW` | Does not start with | `{6.XSW.'prefix'}` |
| `BF` | Before (dates) | `{10.BF.'01-01-2025'}` |
| `OBF` | On or before | `{10.OBF.'01-01-2025'}` |
| `AF` | After (dates) | `{10.AF.'01-01-2025'}` |
| `OAF` | On or after | `{10.OAF.'01-01-2025'}` |
| `LT` | Less than | `{7.LT.'100'}` |
| `LTE` | Less than or equal | `{7.LTE.'100'}` |
| `GT` | Greater than | `{7.GT.'100'}` |
| `GTE` | Greater than or equal | `{7.GTE.'100'}` |

**Combining Conditions:**
- `AND` - Both must be true: `{6.EX.'A'}AND{7.GT.'10'}`
- `OR` - Either can be true: `{6.EX.'A'}OR{6.EX.'B'}`

---

## POST /records

**Insert/Update record(s)**

Insert and/or update records in a table. Max payload: 40MB.

**Request Body:**
```json
{
  "to": "tableId",
  "data": [
    {
      "6": {"value": "Field 6 value"},
      "7": {"value": 123}
    }
  ],
  "mergeFieldId": 3,
  "fieldsToReturn": [3, 6, 7]
}
```

---

## DELETE /records

**Delete record(s)**

```json
{
  "from": "tableId",
  "where": "{3.EX.'123'}"
}
```

---

## GET /tables?appId={appId}

**Get tables for an app**

Returns all tables in the application with their properties.

**Response:**
```json
[
  {
    "id": "bjrvqd33t",
    "name": "Tickets",
    "alias": "_DBID_TICKETS",
    "description": "Work order tickets",
    "singleRecordName": "Ticket",
    "pluralRecordName": "Tickets",
    "keyFieldId": 3,
    "defaultSortFieldId": 1
  }
]
```

---

## GET /fields?tableId={tableId}

**Get fields for a table**

Returns all fields with their properties, types, and configurations.

**Response:**
```json
[
  {
    "id": 6,
    "label": "Customer Name",
    "fieldType": "text",
    "mode": "default",
    "appearsByDefault": true,
    "properties": {
      "maxLength": 255,
      "appendOnly": false
    }
  }
]
```

---

## GET /apps/{appId}

**Get app details**

Returns application properties and variables.

---

## POST /reports/{reportId}/run

**Run a report**

Executes a saved report and returns data. Useful when you've pre-built complex reports in Quickbase UI.

**Request Body:**
```json
{
  "skip": 0,
  "top": 100
}
```

---

## Field Types Reference

| Type | JSON Value Format | Notes |
|------|------------------|-------|
| `text` | `"string value"` | Plain text |
| `rich-text` | `"<b>HTML</b>"` | HTML content |
| `numeric` | `123` or `123.45` | Numbers |
| `currency` | `123.45` | Decimal number |
| `date` | `"2025-01-15"` | ISO date format |
| `datetime` | `"2025-01-15T10:30:00Z"` | ISO datetime |
| `checkbox` | `true` or `false` | Boolean |
| `user` | `{"id": "user123"}` | User reference |
| `file` | Complex object | File attachment |

---

## Pagination

Quickbase uses cursor-based pagination. Check `metadata` in response:

```json
{
  "metadata": {
    "numRecords": 100,
    "skip": 0,
    "totalRecords": 500
  }
}
```

To get next page:
```json
{
  "options": {
    "skip": 100,
    "top": 100
  }
}
```

---

## Error Responses

```json
{
  "message": "Error description",
  "description": "Detailed error info"
}
```

Common HTTP status codes:
- `400` - Bad request (invalid query syntax)
- `401` - Unauthorized (bad token)
- `403` - Forbidden (no access to resource)
- `404` - Not found (invalid table/app ID)
- `429` - Rate limited

---

## Rate Limits

- Requests are rate-limited per realm
- Implement exponential backoff on 429 responses
- Consider caching frequently-accessed metadata (tables, fields)
