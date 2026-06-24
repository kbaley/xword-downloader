# xword-downloader

Azure Function that downloads configured crossword PDFs on a timer.

## Configuration

| Setting | Required | Purpose |
| --- | --- | --- |
| `AzureWebJobsStorage` | Yes | Azure Functions storage connection string. |
| `GoogleApiSecretsKeyVaultUri` | Yes | Key Vault URI that contains the Google service account JSON secret. |
| `GoogleApiSecretsKeyVaultSecretName` | Yes | Name of the Key Vault secret that contains the Google service account JSON. |
| `GoogleDriveFolderId` | Yes | Google Drive folder where puzzle PDFs are uploaded. |
| `DownloadToAzureFileStorage` | No | Set to `true`, `1`, or `yes` to also upload puzzle PDFs to the `puzzles` Azure File share. Defaults to off. |

`GoogleApiSecretsFileName` is still supported as a legacy fallback for local runs that read the Google service account JSON from the `secrets` Azure File share.
