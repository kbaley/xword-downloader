# xword-downloader
Script to download PDF versions of various online crossword puzzle providers

With credit to https://www.reddit.com/user/oxguy3/ for the [starting point](https://www.reddit.com/r/crossword/comments/dqtnca/my_automatic_nyt_crossword_downloading_script/)

Requires installation of jq and coreutils (for the gdate functions on Mac). Also requires your NYT cookies in a cookies.txt file in the same folder. Or comment out the NYT section of the main script. The important cookie is the one named: `NYT-S`

By default, the script will download all crosswords from New York Times, Washington Post Sunday, and Newsday since the last download.
