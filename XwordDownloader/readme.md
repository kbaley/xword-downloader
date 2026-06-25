# xword-downloader

Azure Function that downloads configured crossword PDFs on a timer.

## Configuration

| Setting | Required | Purpose |
| --- | --- | --- |
| `AzureWebJobsStorage` | Yes | Azure Functions storage connection string. Also used to read the Google API secrets file from the `secrets` Azure File share. |
| `GoogleApiSecretsFileName` | Yes | File name of the Google service account JSON file in the `secrets` Azure File share. |
| `GoogleDriveFolderId` | Yes | Google Drive folder where puzzle PDFs are uploaded. |
| `DownloadToAzureFileStorage` | No | Set to `true`, `1`, or `yes` to also upload puzzle PDFs to the `puzzles` Azure File share. Defaults to off. |
| `ResendApiKey` | No | Resend API key used to send the run summary email. Email is skipped if this or the other Resend settings are missing. |
| `ResendFromEmail` | No | Verified Resend sender, for example `Crossword Downloader <crosswords@example.com>`. |
| `ResendToEmail` | No | Recipient email address for run summaries. Separate multiple recipients with commas or semicolons. |
| `WallStreetJournalDownloadEnabled` | No | Set to `false`, `0`, `no`, or `off` to skip the WSJ downloader and report it as skipped in the summary email. Defaults to enabled. |
| `WallStreetJournalPuzzlePdfUrl` | No | Direct WSJ PDF URL to use when the WSJ article index cannot be scraped. |
| `WallStreetJournalPuzzlePdfDate` | No | Date in `yyyy-MM-dd` format for the direct WSJ PDF URL. If set, stale URLs are ignored on later days. |
