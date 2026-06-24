# xword-downloader

Azure Function that downloads configured crossword PDFs on a timer.

## Configuration

| Setting | Required | Purpose |
| --- | --- | --- |
| `AzureWebJobsStorage` | Yes | Azure Functions storage connection string. |
| `GoogleApiSecretsJson` | Yes | Google service account JSON. In Azure, set this to a Key Vault reference rather than a literal JSON value. |
| `GoogleDriveFolderId` | Yes | Google Drive folder where puzzle PDFs are uploaded. |
| `DownloadToAzureFileStorage` | No | Set to `true`, `1`, or `yes` to also upload puzzle PDFs to the `puzzles` Azure File share. Defaults to off. |

`GoogleApiSecretsKeyVaultUri` and `GoogleApiSecretsKeyVaultSecretName` are also supported for direct Key Vault SDK reads.
`GoogleApiSecretsFileName` is still supported as a legacy fallback for local runs that read the Google service account JSON from the `secrets` Azure File share.
