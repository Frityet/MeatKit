@echo off

cl -LD src/download_file.c -o libdownloadfile.dll src/curl/lib/libcurl.dll.a
echo "Fuck windows"
