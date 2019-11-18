#!/usr/bin/env bash

## Modify these for your environment
dest=~/Desktop/crosswords/
cookies=$(greadlink -m ./cookies.txt)

mkdir -p "$dest"
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
    numdays=$(( ($(gdate +%s) - $(gdate -d $lastchecked +%s) )/(60*60*24) ))
    if [[ $numdays > 0 ]]
    then
        echo "Retrieving $numdays puzzles from New York Times"
        puzzids=`curl -b "$cookies" "https://nyt-games-prd.appspot.com/svc/crosswords/v3/36569100/puzzles.json?publish_type=daily&sort_order=asc&sort_by=print_date&limit=$numdays" | jq '.results[].puzzle_id'`
        for puzzid in $puzzids
        do
            echo "https://www.nytimes.com/svc/crosswords/v2/puzzle/$puzzid.pdf"
            curl -b "$cookies" -OJ "https://www.nytimes.com/svc/crosswords/v2/puzzle/$puzzid.pdf"
        done
    fi
}

washington_post_sunday() {
    puzzle_day=$(gdate -d $lastchecked +"%u")
    if [ $puzzle_day == 7 ]
    then
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

new_york_times
while [ $lastchecked != $tomorrow ]
do
    echo "Retrieving puzzles for $lastchecked"
    washington_post_sunday
    newsday
    lastchecked=$(gdate -d "$lastchecked tomorrow" +$dateformat)
done

 rm $FILE
 echo $lastchecked >> $FILE
