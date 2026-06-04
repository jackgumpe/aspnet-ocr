# Phase 1 Boundary

## Included

- API upload for one document at a time.
- Safe artifact persistence for original file, raw OCR text, validation report, manifest, and Excel workbook.
- Text-PDF extraction and Tesseract image OCR behind `IOcrService`.
- ClosedXML workbook export behind `IExcelService`.
- SQLite document state repository.
- Serilog JSON logs with `correlation_id`.
- Health endpoints.
- Gold dataset parser regression.

## Excluded

- Angular UI implementation.
- Bulk queue or parallel processing.
- Azure Document Intelligence fallback.
- Scanned-PDF rasterization.
- Lagrange threshold tuning.
- OpenTelemetry, Prometheus, or ZMQ bridge.
- Authentication, production deployment, blob storage, or PostgreSQL.

## Non-Negotiables Tracked

- No silent success.
- No lost OCR artifact.
- No export without manifest.
- No duplicate rows after retry.
- No infrastructure in Domain.
- No Phase 2 creep.
- Invalid file rejected.
- Corrupt document is dead-lettered.
- Correlation ID is preserved across logs and state.
