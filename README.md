All endpoints are documented via Swagger at /swagger. Key endpoints include:

1-POST /api/countries/block
Add a country to the blocked list.
Body: { "Code": "US", "Name": "United States" }

2-DELETE /api/countries/block/{countryCode}
Remove a country from the blocked list.
Example: /api/countries/block/US

3-GET /api/countries/blocked
List blocked countries with pagination and search.
Query: ?page=1&pageSize=10&search=US

4-GET /api/ip/lookup
Lookup country details for an IP (defaults to caller’s IP if omitted).
Query: ?ipAddress=8.8.8.8
5-GET /api/ip/check-block
Check if the caller’s IP is blocked and log the attempt.

6-GET /api/logs/blocked-attempts
List blocked attempt logs with pagination.
Query: ?page=1&pageSize=10

7-POST /api/countries/temporal-block
Temporarily block a country (1-1440 minutes).
Body: { "countryCode": "EG", "durationMinutes": 120 }
