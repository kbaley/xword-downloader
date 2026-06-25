# xword-downloader

Azure Function that downloads configured crossword PDFs on a timer.

## Configuration

| Setting | Required | Purpose |
| --- | --- | --- |
| `AzureWebJobsStorage` | Yes | Azure Functions storage connection string. Also used to read the Google API secrets file from the `secrets` Azure File share. |
| `GoogleApiSecretsFileName` | Yes | File name of the Google service account JSON file in the `secrets` Azure File share. |
| `GoogleDriveFolderId` | Yes | Google Drive folder where puzzle PDFs are uploaded. |
| `DownloadToAzureFileStorage` | No | Set to `true`, `1`, or `yes` to also upload puzzle PDFs to the `puzzles` Azure File share. Defaults to off. |

