#include <stdint.h>
#include <stdio.h>
#include <errno.h>
extern int errno;
#include <stdbool.h>

#include <windows.h>

#include "curl/curl.h"

struct Memory {
    uint8_t *data;
    size_t count;
};

struct AsyncDownloadRequest {
    bool            complete;
    struct Memory   result;
    long            downloaded, total;
    
    /*PRIVATE*/
    char    *url;
    size_t  url_length;

    struct {
        HANDLE  handle;
        DWORD   id;
    } thread;
};

static void fatal_error(const char *fmt, ...)
{
    va_list list;
    va_start(list, fmt);

    char buffer[1024];
    vsprintf_s(buffer, 1024, fmt, list);

    va_end(list);

    fputs(buffer, stderr);

    MessageBox (
        NULL,
        buffer,
        "FATAL ERROR!",
        MB_ICONERROR | MB_OK | MB_DEFBUTTON1
    );

    // exit(EXIT_FAILURE);
}

static size_t write_memory(void *contents, size_t size, size_t bytecount, void *ptr)
{
    size_t total = size * bytecount;
    struct Memory *mem = ptr; //The layout of ptr should correspond to struct Memory

    //Resize the memory with the previous size, plus the new size with one to be the null character
    uint8_t *newmem = realloc(mem->data, mem->count + total + 1); 
    if (newmem == NULL) {
        char buf[256];
        strerror_s(buf, 256, errno);
        // fprintf(stderr, "Could not allocate %zu data of memory!\nReason: %s\n", mem->size + total, buf);
        fatal_error("Could not allocate %zu data of memory!\nReason: %s\n", mem->count + total, buf);
    }

    mem->data = newmem;
    memcpy(&(mem->data[mem->count]), contents, total);
    mem->count += total;
    //Just in case this is a string, add a null terminator
    mem->data[mem->count] = '\0';

    return total;
}

static int update_progress(struct AsyncDownloadRequest *mem, long total, long now, long _, long __)
{
    mem->total = total;
    mem->downloaded = now;
    return 0;
}


static DWORD asyncdownload(struct AsyncDownloadRequest *request)
{
    CURL *curl = curl_easy_init();
    struct Memory memory = {0};
    memory.data = malloc(sizeof(uint8_t));

    curl_easy_setopt(curl, CURLOPT_URL,             request->url);
    curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION,  true);

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION,   write_memory);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA,       &memory);


    curl_easy_setopt(curl, CURLOPT_NOPROGRESS,      false);
    curl_easy_setopt(curl, CURLOPT_XFERINFOFUNCTION,update_progress);
    curl_easy_setopt(curl, CURLOPT_XFERINFODATA,    request);

    curl_easy_setopt(curl, CURLOPT_USERAGENT,       "libcurl-agent/1.0");

    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER,  false);
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST,  false);
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYSTATUS,false);            
    // curl_easy_setopt(curl, CURLOPT_CAINFO,          "curl-ca-bundle.crt");
    // curl_easy_setopt(curl, CURLOPT_CAPATH,          "curl-ca-bundle.crt");

    CURLcode err;
    if ((err = curl_easy_perform(curl)) != CURLE_OK) {
        fatal_error("Could not download file at URL %s!\nReason: %s\n", request->url, curl_easy_strerror(err));
    }
    curl_free(curl);

    //The first byte of the returned value is gibberish
    //we need 1 byte for the null terminator
    request->result.data = malloc(sizeof(uint8_t) * memory.count);
    for (size_t i = 1; i < memory.count; i++)
        request->result.data[i - 1] = memory.data[i];        
    request->result.data[memory.count] = '\0';

    request->result.data = memory.data;
    request->result.count = memory.count;
    request->complete = true;

    //because we copied over the data without the gibberish, we can just free it
    free(memory.data);
    free(request->url);

    return 0;
}

struct AsyncDownloadRequest *download_file_async(const char *url)
{
    struct AsyncDownloadRequest *request = malloc(sizeof(struct AsyncDownloadRequest));
    request->complete = false;

    size_t urllen = strlen(url);
    urllen++;
    request->url = malloc(sizeof(char) * urllen);

    //strcpy does not play nice at all, this is the best alternative
    memcpy_s(request->url, urllen, url, urllen);
    request->url[urllen] = '\0';

    request->thread.handle = CreateThread(NULL, 0, (void *)asyncdownload, request, 0, &request->thread.id);

    return request;
}

void free_asyncdownload_request(struct AsyncDownloadRequest *request)
{
    CloseHandle(request->thread.handle);
    free(request->result.data);
    free(request);
}
