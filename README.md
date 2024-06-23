# xword-downloader

There are two major components to this:

- A bash script (and associated utilities)
- An Azure Functions app

Both do the same thing in that they download Crossword puzzles from various providers. The bash script is more complete since it covers more providers though as of June 2024, I haven't tested it with Uclick and Newsday in a couple of years. The Azure Functions app was added so that this can run on a timer automatically. As of June 2024, that's what I'm using so the bash script will likely fall into disrepair though I haven't had to update it much, except to deal with providers changing things around.

## Azure Functions app

This is newer than the bash file and has the advantage that it runs on a timer rather than on demand. It could easily be modified to use an HTTP trigger to behave more like the Bash script. That would require keeping track of the last time it was run.

Current disadvantage of the app is that it dumps everything to Azure Storage which requires the extra step of me downloading the files or setting up a network drive. The bash script dumps files to a local folder that's hooked up to Google Drive so next step is to configure the Azure Functions app to save the files to Google Drive. For now, dumping to Azure Storage is pretty simple.

The Azure Functions app also doesn't have the merge capability that the bash script does where it can merge all the PDFs into a single file. So you can keep using the bash script for that. Or MacOS's built-in capability of right-clicking a bunch of PDFs | Quick Actions | Create PDF. An alternative could be to look for a specific file in the destination and append the new downloads to it. Then I'd delete the file each time I printed it and start again.

The Azure Functions app requires a few environment variables:

- AzureWebJobsStorage: This usually gets created for you when you create an Azure Functions app. It's where the puzzles get stored. 
- NYTS_COOKIE: The value of the nyt-s cookie from your browser indicating you have an active subscription. Login to the NYT website and use your browser tools to find this. You'll obviously need to update this value when your subscription renews.
- GoogleDriveFolderId: The ID of the folder in Google Drive where the files should be copied. You can get this by navigating to the folder in a browser. Everything after `folder/` in the URL is the ID
- GoogleApiSecretsFileName: The name of the filename where the Google API service account details are stored. This file should exist in the same Azure Storage account in a file share folder called `secrets`

When working locally, the AzureWebJobsStorage variable isn't necessary. And the NTYS_COOKIE once isn't necessary if you aren't testing New York Times's puzzle.

### Google Drive API

This was a lot of trial and error to figure out and because it involves security, I got bored and/or frustrated very quickly and went with what worked. The major steps include:

- Create a project in your Google Developer Console
- Enable the Google Drive API (it wasn't enabled by default for me)
- Create a _service account_ credential
- Under the new service account, create a _key_. This will download a file to your computer.
- Upload the file to the Azure Storage account for the Azure Functions app in a File Share folder called `secrets`
- Create/update two environment variables for the Azure Function:
  - GoogleApiSecretsFileName: the name of the file (include the .json extension) you downloaded for the key
  - GoogleDriveFolderId: The ID of the folder where the puzzles should be saved. Navigate to the folder in a browser. The ID is everything after `folder/` in the URL.
- Share the Google drive folder with the Google API service account (with Editor access). The email address is on the Credentials page in the Google Developer Console and probably ends with .iam.gserviceaccount.com.

The Azure documentation and ChatGPT suggest strongly that OAuth2 credentials should work if you set up Google as an authenticator on the Azure Functions app. I ran into problems with this _I think_ because I couldn't debug locally from a console app so I went with the service account.

### Working locally

Working with Azure Functions locally isn't that bad but since this runs on a timer, it's a bit of a pain having to trigger it manually for debugging purposes. So there's a console option that runs in DEBUG mode instead of using the Azure Functions host. This also saves the puzzle PDFs to your Downloads folder rather than Azure Storage.

## Bash script

With credit to https://www.reddit.com/user/oxguy3/ for the [starting point](https://www.reddit.com/r/crossword/comments/dqtnca/my_automatic_nyt_crossword_downloading_script/)

Requires installation of jq and coreutils (for the gdate functions on Mac). Also requires your NYT cookies in a cookies.txt file in the same folder. Or comment out the NYT section of the main script. The important cookie is the one named: `NYT-S`

Requires GhostScript (`brew install gs`) to merge all PDFs into a single one.

Create a copy of xword.config.src called xword.config, updating the values as appropriate.

By default, the script will download all crosswords from New York Times, Washington Post Sunday, and Newsday since the last download.

By providing a `--merge` or `-m` parameter to the app, it will:

- merge all PDFs in the destination folder into a single PDF called Crosswords.pdf
- copy the PDFs into a folder in the destination called `archive`

This uses the built-in MacOS PDF-merging python script.

## Uclick providers

**NOTE**: This works only for regular-sized (i.e. 15x15) daily puzzles. The larger Sunday ones (e.g. from Newsday) have some formatting issues.

Puzzle providers that use uclick.com as their provider are a bit tricky. The puzzles are provided in what appears to be a custom XML format ([sample USA Today puzzle](http://picayune.uclick.com/comics/usaon/data/usaon200510-data.xml)). Based on an idea from [a friend](https://twitter.com/stimms), I created an XSLT transform for this and made a small C# utility that I could use to apply it to each XML file and generate an HTML file. Then I use Google Chrome's built-in headless mode to print the HTML file to a PDF. A sample command on Mac:

`/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --headless --print-to-pdf="./moo.pdf" ./output.html`

The XSLT uses CSS grid to layout the crossword grid which, as its name implies, is almost exactly what it was designed for. I initially tried to use wkhtmltopdf to write the HTML to PDF but it can't handle CSS grid.

The XSLT contains a bit of CSS at the top to remove the default headers that Chrome typically adds to each page. There seems to be a `--print-to-pdf-no-header` flag that is coming out soon (currently in Chrome Canary) but I didn't want to install Canary.

Specify the path to Chrome in your `xword.config` file.

When converting to a PDF, I typically see an error saying "Operation not permitted". I've been ignoring it on the grounds that it still works.

The XSLT transform utility is in C# because that's what I know. That is a bit restrictive because it limits things to XSLT 1.0 which was pretty annoying when it came to URI decoding the clues. At some vague point in the future, it might be nice to try some other language that supports XSLT 2.0 parsing but so far, it works.

The bash script expects the utility to be in a certain location. To publish:

`dotnet publish --configuration=Release -r osx.10.14-x64 --self-contained false -o ../xml2htmlutil/`

## Email providers

I've subscribed to some providers by email:

- Crossword Nation (i.e. Elizabeth Gorski)
- American Values Crossword
- Inkubator
- Matt Gaffney

To manage these, I've set up some zaps in Zapier. These ones parse emails and save the appropriate attachment to Google Drive

- [Extract PDF from AVCX and Inkubator](https://zapier.com/shared/bb19d3831467fdf7210a835e8b14e0d80842b4c6)
- [Extract PDF from Crossword Nation](https://zapier.com/shared/d2ded7c36035204ee7c164ffc51fd3e4ad825bef)

These were done with Zapier's "New Attachment in Gmail" event. Each one triggers on any emails from one of the specified crossword providers. Then it filters the attachment to the one I want since the providers usually include several, for example, the solution or different versions of the crossword. After filtering, it is simply saved to a specified folder in Google Drive.

Matt Gaffney's crosswords need a bit more work. His are sent from Patreon which doesn't actually attach the crosswords but instead sends a link to them. I couldn't find a way to parse the link with Zapier's built-in email parser so I created an account with mailparser.io, which also integrates with Zapier. It took a bit of fiddling to get the parsing rule right.

Using Mailparser also means the parsing isn't done automatically when the email comes in. Instead, I need to forward the email to a special mailparser.io email address to trigger the rule. This was easy enough to automate with a Gmail filter to auto-forward any mail meeting certain criteria, then auto-archiving the email in Gmail. You have to be a little creative with the Gmail filter because Matt often postpones crosswords and will send an email letting you know. Can't blame him, he's got a young daughter who is (understandably) more important than making sure I have a puzzle to work on every Friday. Either way, the Gmail filter should be set up so that it includes only the ones from Matt with a link to the puzzle, otherwise the zap will fail, which, to be fair, isn't exactly the end of the world.

In Zapier, this uses the "New Email Parsed in Mailparser" and "Upload File in Google Drive" events. No filter is needed because it's done in Mailparser.

This entire setup requires 3 zaps in Zapier and an account with Mailparser. The free Mailparser account allows 30 emails per month which is more than enough for Matt Gaffney (typically 4 or 5 plus an occasional informational email that slips through the filter).

## Wall Street Journal Crossword Contest

> **June 2024 update**: This section doesn't really apply anymore because the URLs are deterministic now (based on the date). The bash file still uses the custom utility but the Azure Function shows how this can be done MUCH more easily. I'm leaving the text for historic purposes.

This is a bit of a hack. The puzzles don't seem to have deterministic URLs; there is always some sort of random string of characters involved, possibly to protect against the likes of me.

The WSJDownloader utility uses [Playwright](https://playwright.dev/dotnet/) to essentially scrape the WSJ website and download the necessary puzzles. It's not the prettiest solution but we'll see how it goes.

The batch file executes the utility in the WSJDownloadUtil folder so a compiled version of the code must exist there. The command to do this is (from the WSJDownloader folder):

`dotnet publish --configuration=Release -o ../WSJDownloadUtil`

## The workflow

- Run the bash script to get the most recent crosswords
- Run the bash script with `--merge` to merge them (along with any saved from email providers) into a single PDF file
- Print
- Solve
- Repeat when you run out

## Next

I want to tackle some other providers that are a bit more difficult to automate, either because they have no explicit download API or they provide it in some format that requires some conversion before it gets printed.

## Other crossword resources

- [Crosswords Classic on iOS](https://apps.apple.com/us/app/crosswords-classic/id284036524)
  - There's a newer one that is probably better. I'm used to the classic one and still prefer it
- [Shortyz on Android](https://play.google.com/store/apps/details?id=com.totsp.crossword.shortyz&hl=en)
  - Back when I had an Android device, this was a great app. I can only assume it still is. Either way, its source code is [open source](https://github.com/kebernet/shortyz)