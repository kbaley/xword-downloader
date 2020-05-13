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

## Email providers

I've subscribed to some providers by email:

- Crossword Nation (i.e. Elizabeth Gorski)
- American Values Crossword
- Inkubator
- Matt Gaffney

To manage these, I've set up some zaps in Zapier. These ones parse emails and save the appropriate attachment to Google Drive

- [Extract PDF from AVCX and Inkubator](https://zapier.com/shared/bb19d3831467fdf7210a835e8b14e0d80842b4c6)
- [Extract PDF from Crossword Nation](https://zapier.com/shared/d2ded7c36035204ee7c164ffc51fd3e4ad825bef)

These were done with Zapier's "New Attachment in Gmail" zap. Each one triggers on any emails from one of the specified crossword providers. Then it filters the attachment to the one I want since the providers usually include several, for example, the solution or different versions of the crossword. After filtering, it is simply saved to a specified folder in Google Drive.

Matt Gaffney's crosswords need a bit more work. His are sent from Patreon which doesn't actually attach the crosswords but instead sends a link to them. I couldn't find a way to parse the link with Zapier's built-in email parser so I created an account with mailparser.io, which also integrates with Zapier.

## The eventual workflow

- Run the bash script to get the most recent crosswords
- Run the bash script with `--merge` to merge them (along with any saved from email providers) into a single PDF file
- Print
- Solve
- Repeat when you run out
