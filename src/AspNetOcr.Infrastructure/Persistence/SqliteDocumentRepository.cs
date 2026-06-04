using AspNetOcr.Application.Interfaces;
using AspNetOcr.Domain.Documents;
using Microsoft.Data.Sqlite;

namespace AspNetOcr.Infrastructure.Persistence;

public sealed class SqliteDocumentRepository : IDocumentRepository
{
    private readonly string _connectionString;

    public SqliteDocumentRepository(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath)) ?? ".");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        Initialize();
    }

    public async Task<DocumentRecord?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select * from documents where id = $id limit 1;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<DocumentRecord?> FindByContentHashAsync(string contentHash, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select * from documents where content_hash = $content_hash limit 1;";
        command.Parameters.AddWithValue("$content_hash", contentHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task SaveAsync(DocumentRecord document, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into documents (
                id,
                correlation_id,
                file_name,
                content_hash,
                status,
                created_at,
                updated_at,
                original_artifact_path,
                raw_ocr_artifact_path,
                validation_artifact_path,
                manifest_artifact_path,
                export_artifact_path,
                last_error
            )
            values (
                $id,
                $correlation_id,
                $file_name,
                $content_hash,
                $status,
                $created_at,
                $updated_at,
                $original_artifact_path,
                $raw_ocr_artifact_path,
                $validation_artifact_path,
                $manifest_artifact_path,
                $export_artifact_path,
                $last_error
            )
            on conflict(id) do update set
                correlation_id = excluded.correlation_id,
                file_name = excluded.file_name,
                content_hash = excluded.content_hash,
                status = excluded.status,
                updated_at = excluded.updated_at,
                original_artifact_path = excluded.original_artifact_path,
                raw_ocr_artifact_path = excluded.raw_ocr_artifact_path,
                validation_artifact_path = excluded.validation_artifact_path,
                manifest_artifact_path = excluded.manifest_artifact_path,
                export_artifact_path = excluded.export_artifact_path,
                last_error = excluded.last_error;
            """;

        command.Parameters.AddWithValue("$id", document.Id.ToString());
        command.Parameters.AddWithValue("$correlation_id", document.CorrelationId);
        command.Parameters.AddWithValue("$file_name", document.FileName);
        command.Parameters.AddWithValue("$content_hash", document.ContentHash);
        command.Parameters.AddWithValue("$status", document.Status.ToString());
        command.Parameters.AddWithValue("$created_at", document.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", document.UpdatedAt.ToString("O"));
        command.Parameters.AddWithValue("$original_artifact_path", (object?)document.OriginalArtifactPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$raw_ocr_artifact_path", (object?)document.RawOcrArtifactPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$validation_artifact_path", (object?)document.ValidationArtifactPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$manifest_artifact_path", (object?)document.ManifestArtifactPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$export_artifact_path", (object?)document.ExportArtifactPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$last_error", (object?)document.LastError ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists documents (
                id text primary key,
                correlation_id text not null,
                file_name text not null,
                content_hash text not null unique,
                status text not null,
                created_at text not null,
                updated_at text not null,
                original_artifact_path text null,
                raw_ocr_artifact_path text null,
                validation_artifact_path text null,
                manifest_artifact_path text null,
                export_artifact_path text null,
                last_error text null
            );
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static DocumentRecord Map(SqliteDataReader reader)
    {
        return DocumentRecord.Rehydrate(
            Guid.Parse(ReadString(reader, "id")),
            ReadString(reader, "correlation_id"),
            ReadString(reader, "file_name"),
            ReadString(reader, "content_hash"),
            Enum.Parse<DocumentStatus>(ReadString(reader, "status")),
            DateTimeOffset.Parse(ReadString(reader, "created_at")),
            DateTimeOffset.Parse(ReadString(reader, "updated_at")),
            ReadNullableString(reader, "original_artifact_path"),
            ReadNullableString(reader, "raw_ocr_artifact_path"),
            ReadNullableString(reader, "validation_artifact_path"),
            ReadNullableString(reader, "manifest_artifact_path"),
            ReadNullableString(reader, "export_artifact_path"),
            ReadNullableString(reader, "last_error"));
    }

    private static string ReadString(SqliteDataReader reader, string column)
    {
        return reader.GetString(reader.GetOrdinal(column));
    }

    private static string? ReadNullableString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
