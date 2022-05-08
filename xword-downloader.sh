#!/usr/bin/env bash

## Modify these for your environment
cookies=$(greadlink -m ./cookies.txt)
declare -a subscriptions
. ./xword.config

current_dir="$(pwd)"
echo "Current directory: $current_dir"
mkdir -p "$dest"
mkdir -p "$dest/archive"
cd "$dest"

FILE=./lastchecked.txt
dateformat='%Y-%m-%d'
lastchecked=$(date +$dateformat)
if test -f "$FILE"; then
    lastchecked=$(<$FILE)
else
    echo $lastchecked >> $FILE
fi

tomorrow=$(gdate -d "tomorrow" +"$dateformat")

new_york_times() {
    # We're starting from the day after the last time we checked. So if we're starting from
    # today, we want to get one day's worth of puzzles (i.e. today's). Hence we add one to
    # our numdays calculation below
    numdays=$(( ($(gdate +%s) - $(gdate -d $lastchecked +%s) )/(60*60*24)+1))
    if [[ $numdays -gt 0 ]]; then
        echo "Retrieving $numdays puzzles from New York Times"
        puzzids=`curl -b "$cookies" "https://nyt-games-prd.appspot.com/svc/crosswords/v3/36569100/puzzles.json?publish_type=daily&sort_order=asc&sort_by=print_date&limit=$numdays" | jq '.results[].puzzle_id'`
        for puzzid in $puzzids
        do
            echo "https://www.nytimes.com/svc/crosswords/v2/puzzle/$puzzid.pdf"
            curl -b "$cookies" -OJ "https://www.nytimes.com/svc/crosswords/v2/puzzle/$puzzid.pdf"
        done
    fi
}

uclick() {
    # $1: Name of provider
    # $2: Short name (used for filename)
    # $3: uclick code for URL
    echo "$1: $lastchecked"
    usadate=$(gdate -d $lastchecked +'%y%m%d')
    filename="$2$usadate"
    DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
    cd "$current_dir"
    ./xml2htmlutil/xml2html "http://picayune.uclick.com/comics/$3/data/$3$usadate-data.xml" "$filename"
    eval $("$pathToChrome" --headless --print-to-pdf="$dest/$filename.pdf" ./$filename.html)
    rm ./$filename.html
    cd "$dest"
}

washington_post_sunday() {
    puzzle_day=$(gdate -d $lastchecked +"%u")
    if [ $puzzle_day == 7 ]; then
        echo "Washington post: $lastchecked"
        wapodate=$(gdate -d $lastchecked +'%y%m%d')
        curl -OJ -X POST -H "Content-Type: application/x-www-form-urlencoded" -d "id=ebirnholz_${wapodate}&set=wapo-eb&theme=wapo&locale=en-US&print=1&checkPDF=true" "https://cdn1.amuselabs.com/wapo/crossword-pdf"
    fi
}

newsday() {
    echo "Newsday: $lastchecked"
    newsdaydate=$(gdate -d $lastchecked +'%Y%m%d')
    curl -OJ -X POST -H 'Content-Type: application/x-www-form-urlencoded' -d "id=Creators_WEB_${newsdaydate}&set=creatorsweb&theme=newsday&locale=en-US&print=1&checkPDF=true" "https://cdn2.amuselabs.com/pmm/crossword-pdf" 
}

wsjcontest() {
    # Format: https://s.wsj.net/public/resources/documents/XWDMMDDYYYYY.pdf
    # Note the extra Y at the end which is a constant
    # e.g. XWD04302021Y.pdf
    # UPDATE July 2021: seems their format isn't as standard as I thought. There are semi-random
    # characters at the end of the filename now.
    # Promising lead: http://herbach.dnsalias.com/wsj/wsfYYMMDD.puz
    # Would need to convert from puz to PDF. See https://github.com/njyoon/njyoon.github.io
    # UPDATE 2 July 21: I created a utility that uses Selenium to extract the puzzles instead
    # Not bullet proof (it won't work if you need to download more than is available on the first page)
    # but it should work
    cd "$current_dir"
    ./WSJDownloadUtil/WSJDownloader "$dest" "$lastchecked"
    cd "$dest"

}

do_merge() {
    # The following works with Google Drive on Mac but not with OneDrive
    destination_file=./Crosswords.pd_
    source_files=./*.pdf
    # The following works with OneDrive on Mac but not with Google Drive
    # destination_file=$dest/Crosswords.pd_
    # source_files=$dest/*.pdf
    pdf_count=$(ls -l $source_files 2>/dev/null| wc -l)
    destination_count=$(ls -l $dest/Crosswords.pdf 2>/dev/null| wc -l)
    if [ $destination_count -eq 1 ]; then
        echo "There is already a merged file in the $dest. Please rename or remove it and try again"
        return
    elif [ $pdf_count -eq 0 ]; then
        echo "There are no PDFs in $dest to merge"
        return
    fi
    echo "Count: $pdf_count"
    echo "Merging files into a single PDF"
    # No worky in Monterey with the removal of Python2 (I think)
    # $(/System/Library/Automator/Combine\ PDF\ Pages.action/Contents/Resources/join.py -o $destination_file $source_files)

    # Requires GhostScript: brew install gs
    gs -q -dNOPAUSE -dBATCH -sDEVICE=pdfwrite -sOutputFile=$destination_file $source_files
    mv $source_files ./archive/
    mv $destination_file ./Crosswords.pdf
}

retrieve_crosswords() {
    # start from the day after the last time we checked
    lastchecked=$(gdate -d "$lastchecked tomorrow" +$dateformat)
    echo "Retrieving puzzles starting from $lastchecked into $dest"
    if [ $lastchecked = $tomorrow ]; then
        echo "Already retrieved puzzles today"
    else
        if [[ ${subscriptions[@]}  =~ "nyt" ]]; then
            echo "...New York Times..."
            new_york_times
        fi
    fi

    # wsjcontest only needs to happen once; the utility downloads whatever is required
    # but we need to do this before we start changing the lastcheckeddate variable around
    if [[ ${subscriptions[@]} =~ "wsjcontest" ]]; then
        wsjcontest
    fi

    while [ $lastchecked != $tomorrow ]; do
        echo "Retrieving puzzles for $lastchecked"
        if [[ ${subscriptions[@]}  =~ "wapoSunday" ]]; then
            washington_post_sunday
        fi
        if [[ ${subscriptions[@]}  =~ "newsday" ]]; then
            newsday  # Doesn't work anymore
            # uclick "Newsday" "Newsday" "crnet"
        fi
        if [[ ${subscriptions[@]} =~ "usaToday" ]]; then
            uclick "USA Today" "USAToday" "usaon"
        fi
        if [[ ${subscriptions[@]} =~ "universal" ]]; then
            uclick "Universal" "Universal" "fcx"
        fi
        lastchecked=$(gdate -d "$lastchecked tomorrow" +$dateformat)
    done

    lastchecked=$(date +$dateformat)
    rm $FILE
    echo $lastchecked >> $FILE
}

usage() {
    echo "usage: xword-downloader [-m|--merge]"
    echo "  <no arguments>  retrieve all subscriptions"
    echo "  -m|--merge      Merge and archive PDFs in destination folder into a single PDF"
    echo "  -d|--date       Override the last checked date"
    echo "  -s|--sources    Override the sources/subscriptions from the config file"
}

set_date() {
    lastchecked=$(gdate -d "$1 tomorrow" +$dateformat)
}

set_sources() {
    subscriptions=($1)
}

merge_done=false

while [ "$1" != "" ]; do
    case $1 in
        -m | --merge )      do_merge
                            merge_done=true
                            ;;
        -h | --help )       usage
                            ;;
        -d | --date )       set_date "$2"
                            shift
                            ;;
        -s | --sources )    set_sources "$2"
                            shift
                            ;;
        * )                 usage
                            merge_done=true
                            ;;
    esac
    shift
done

if [[ $merge_done == false ]]
then
    retrieve_crosswords
fi
