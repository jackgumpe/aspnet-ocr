# Phase 1 Review Evidence

## Scope Boundary

- Phase 1 API-only vertical slice is implemented in `src/AspNetOcr.Api`, `src/AspNetOcr.Application`, `src/AspNetOcr.Domain`, and `src/AspNetOcr.Infrastructure`.
- `src/AspNetOcr.Web` is a placeholder only. It contains no Angular code and no UI.
- Phase 2 queue/dashboard hooks are interface or placeholder surfaces only.

## Non-Negotiables

| Non-negotiable | Evidence |
| --- | --- |
| No silent success | `PipelineOrchestrator.ProcessAsync_EmptyOcrFailsValidationAndDeadLetters`, `ProcessAsync_DuplicateSkuFailsValidationBeforeExport`, and `ProcessAsync_LowConfidenceFailsValidationBeforeExport` assert invalid OCR states dead-letter and do not export. |
| No lost OCR artifact | `ProcessAsync_RawOcrArtifactPreservesExactEngineText` and `ProcessAsync_EmptyOcrFailsValidationAndDeadLetters` assert raw OCR is written before validation failure. |
| No export without manifest | `ClosedXmlExport_ThrowsWhenManifestIsMissing` asserts the ClosedXML adapter refuses export without a manifest path. |
| No duplicate rows after retry | `ProcessAsync_ReplaySameContentDoesNotExportDuplicateRows` asserts retry is replayed and the Excel export is not run twice. |
| No infrastructure in Domain | `src/AspNetOcr.Domain/AspNetOcr.Domain.csproj` has no package references and no project references. |
| No Phase 2 creep | `src/AspNetOcr.Web/README.md` marks the web project as a Phase 2 placeholder only. |

## Reviewer Commands

```bash
DOTNET_CLI_HOME=/tmp/dotnet-home /tmp/dotnet9/dotnet build
DOTNET_CLI_HOME=/tmp/dotnet-home /tmp/dotnet9/dotnet test --no-build --no-restore
```
