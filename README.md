# AspNetOCR

Phase 1 implementation for `HES-ASP-OCR-001`.

## Scope

- ASP.NET Core API only.
- Clean Architecture source projects: `Api`, `Application`, `Domain`, `Infrastructure`, `Web`.
- Single document upload through OCR, validation, manifest preservation, and Excel export.
- Gold dataset parser regression with 10 product sheets.
- Phase 2 hooks exist as interfaces only. Bulk queue, Angular dashboard, parallelism, Azure fallback, Lagrange gate, auth, and deployment are out of scope.

## Local Commands

The repo is target-locked to `.NET 9` with `global.json`.

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/AspNetOcr.Api
```

## API

- `POST /documents` with multipart form field `file`.
- `GET /documents/{id}` returns processing state.
- `GET /documents/{id}/export` downloads the exported workbook.
- `GET /health`, `GET /health/ocr`, `GET /health/queue`.

Every request gets a `correlation_id` through `X-Correlation-ID`; one is generated when omitted.
