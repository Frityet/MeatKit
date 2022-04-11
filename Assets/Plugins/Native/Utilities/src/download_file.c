#include <stdint.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#include <errno.h>
extern int errno;

#include <windows.h>

//Compilation does require CURL, so you should compile once, and just use the .dll
#include "curl/curl.h"

#define EXPORT __declspec(dllexport)

typedef uint8_t byte_t;

struct Memory {
    //if compiled with a C++ compiler, it will error on assigning a void* to any other pointer type
    //By using a union we can mitagate this
    union {
        byte_t  *bytes;
        void    *ptr;
    } data;
    size_t size;
};

struct ThreadedTask_Memory {
    bool            complete;
    //_Atomic
    struct Memory   result;   

    //PRIVATE:
    HANDLE  _thread_handle;
    DWORD   _thread_id;
};

static size_t write_memory(void *contents, size_t size, size_t bytecount, void *ptr)
{
    size_t total = size * bytecount;
    struct Memory *mem = ptr; //The layout of ptr should correspond to struct Memory

    //Resize the memory with the previous size, plus the new size with one to be the null character
    byte_t *newmem = realloc(mem->data.ptr, mem->size + total + 1); 
    if (newmem == NULL) {
        char buf[256];
        strerror_s(buf, 256, errno);
        fprintf(stderr, "Could not allocate %zu bytes of memory!\nReason: %s\n", mem->size + total, buf);
        return 0;
    }

    mem->data.ptr = newmem;
    memcpy(&(mem->data.bytes[mem->size]), contents, total);
    mem->size += total;
    //Just in case this is a string, add a null terminator
    mem->data.bytes[mem->size] = '\0';

    return total;
}

struct Memory download_file(const char *url)
{
    CURL *curl = curl_easy_init();
    
    struct Memory mem = {0};
    //Pointer must be allocated for it to be reallocated in write_memory
    mem.data.ptr = malloc(1);

    curl_easy_setopt(curl, CURLOPT_URL,             url);
    curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION,  true);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION,   write_memory);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA,       &mem);
    curl_easy_setopt(curl, CURLOPT_USERAGENT,       "libcurl-agent/1.0");

    CURLcode err;
    if ((err = curl_easy_perform(curl)) != CURLE_OK) {
        fprintf(stderr, "Could not download file at URL %s!\nReason: %s\n", url, curl_easy_strerror(err));
        free(mem.data.ptr);
        curl_free(curl);
        return (struct Memory){0};
    }
    curl_free(curl);

    return mem;
}

struct DownloadFileAsync_Arguments {
    /*_Atomic*/ 
    struct ThreadedTask_Memory  *thread;
    char                        *url;
    size_t                      url_length;     
};

static unsigned long async_download(struct DownloadFileAsync_Arguments *args)
{
    struct Memory mem = download_file(args->url);
    
    args->thread->result.data.ptr = mem.data.ptr;
    args->thread->result.size = mem.size;
    args->thread->complete = true;

    //The memory is allocated in the download_file_async function and must be freed here
    free(args->url);
    free(args);

    return true;
} 

EXPORT struct ThreadedTask_Memory *download_file_async(const char *url)
{
    //Must be on heap because it needs ot be allocated on the other thread
    struct ThreadedTask_Memory *thrdmem = calloc(1, sizeof(*thrdmem));
    struct DownloadFileAsync_Arguments *args = malloc(sizeof(*args));
    args->thread = thrdmem;

    //Because we need to allocate and then copy, it's faster to get the length
    // and use it with strncpy
    size_t urllen = strlen(url);
    args->url = malloc(sizeof(char) * urllen);
    args->url_length = urllen;
    strncpy_s(args->url, urllen, url, 256); 

    thrdmem->_thread_handle = CreateThread(NULL, 0, (void *)async_download, args, 0, &thrdmem->_thread_id);
    // pthread_create(&thrdmem->_thread, NULL, (void *)async_download, args);

    return thrdmem;
}

EXPORT void free_threaded_task_memory(struct ThreadedTask_Memory *task)
{
    WaitForSingleObject(task->_thread_handle, INFINITE);
    free(task);
}