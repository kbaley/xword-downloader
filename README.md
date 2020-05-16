# xword-downloader
Script to download PDF versions of various online crossword puzzle providers

With credit to https://www.reddit.com/user/oxguy3/ for the [starting point](https://www.reddit.com/r/crossword/comments/dqtnca/my_automatic_nyt_crossword_downloading_script/)

Requires installation of jq and coreutils (for the gdate functions on Mac). Also requires your NYT cookies in a cookies.txt file in the same folder. Or comment out the NYT section of the main script. The important cookie is the one named: `NYT-S`

Create a copy of xword.config.src called xword.config, updating the values as appropriate.

By default, the script will download all crosswords from New York Times, Washington Post Sunday, and Newsday since the last download.

By providing a `--merge` or `-m` parameter to the app, it will:

- merge all PDFs in the destination folder into a single PDF called Crosswords.pdf
- copy the PDFs into a folder in the destination called `archive`

This uses the built-in MacOS PDF-merging python script.

## Uclick providers

Puzzle providers that use uclick.com as their provider are a bit tricky. The puzzles are provided in what appears to be a custom XML format ([sample USA Today puzzle](http://picayune.uclick.com/comics/usaon/data/usaon200510-data.xml)). Based on an idea from [a friend](https://twitter.com/stimms), I created an XSLT transform for this and made a small C# utility that I could use to apply it to each XML file and generate an HTML file. Then I use Google Chrome's built-in headless mode to print the HTML file to a PDF. A sample command on Mac:

`/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --headless --print-to-pdf="./moo.pdf" ./output.html`

The XSLT uses CSS grid to layout the crossword grid which, as its name implies, is almost exactly what it was designed for. I initially tried to use wkhtmltopdf to write the HTML to PDF but it can't handle CSS grid.

The XSLT contains a bit of CSS at the top to remove the default headers that Chrome typically adds to each page. There seems to be a `--print-to-pdf-no-header` flag that is coming out soon (currently in Chrome Canary) but I didn't want to install Canary.

Specify the path to Chrome in your `xword.config` file.

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

Using Mailparser also means the parsing isn't done automatically when the email comes in. Instead, I need to forward the email to a special mailparser.io email address to trigger the rule. This was easy enough to automate with a Gmail filter to auto-forward any mail meeting certain criteria, then auto-archiving the email in Gmail.

In Zapier, this uses the "New Email Parsed in Mailparser" and "Upload File in Google Drive" events. No filter is needed because it's done in Mailparser.

This entire setup requires 3 zaps in Zapier and an account with Mailparser. The free Mailparser account allows 30 emails per month which is more than enough for Matt Gaffney (typically 4 or 5 plus an occasional informational email that slips through the filter).

## The eventual workflow

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
    
    https://github.com/kebernet/shortyz)
  - Back 