#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>
#include <math.h>
#include <string.h>


struct Memory {
    uint8_t *data;
    size_t  size;
};

struct ThreadedTaskMemory {
    bool            complete;
    struct Memory   result;
    long            downloaded, total;
};

extern struct ThreadedTaskMemory *download_file_async(const char *url);
extern void free_asyncdownload_request(struct ThreadedTaskMemory *task);

static char *tostring(uint8_t *data, size_t count)
{
    char *buf = malloc(sizeof(char) * count + 1);
    memcpy_s(buf, sizeof(char) * count, 
             data, sizeof(uint8_t) * count);

    buf[count] = '\0';
    return buf;
}

int main()
{
    puts("Starting download");
    struct ThreadedTaskMemory *request = download_file_async("https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json");

    while (!request->complete)
        printf("Downloading... (%lf%% done)\r", ceil((float)request->downloaded / (float)request->total));
    
    printf("\nDone, downloaded %zu bytes\n", request->result.size);

    puts(tostring(request->result.data, request->result.size));

    free_asyncdownload_request(request);

    request = download_file_async("https://raw.githubusercontent.com/Frityet/H3VRUtilities/master/meatkitpkg.json");
    while (!request->complete)
        printf("Downloading... (%lf%% done)\r", ceil((float)request->downloaded / (float)request->total));

    printf("\nDone, downloaded %zu bytes\n", request->result.size);

    puts(tostring(request->result.data, request->result.size));
    free_asyncdownload_request(request);
}