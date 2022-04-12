OUTPUT="libdownloadfile.dll"
DESTINATION="../../../MeatKit/Editor/Plugins/${OUTPUT}"
CC="x86_64-w64-mingw32-gcc"

CURL_DLL="src/curl/bin/libcurl-x64.dll"
CURL_DLL_OBJECT="src/curl/lib/libcurl.dll.a"
CURL_OBJECT="src/curl/lib/libcurl.a"

cp $CURL_DLL ./libcurl-x64.dll

$CC src/downloadfile.c -g -Og -shared -DCURL_STATICLIB -o $OUTPUT -L. -lcurl $CURL_DLL_OBJECT $CURL_OBJECT 

