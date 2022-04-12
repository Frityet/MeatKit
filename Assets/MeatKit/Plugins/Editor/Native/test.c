#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>
#include <math.h>


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


int main()
{
    puts("Starting download");
    struct ThreadedTaskMemory *request = download_file_async("https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json");

    while (!request->complete)
        printf("Downloading... (%lf%% done)\r", ceil((float)request->downloaded / (float)request->total));
    
    puts("\nDone");

    char *data = malloc(request->result.size);
    size_t i = 0;
    for (; i < request->result.size; i++) {
        data[i] = request->result.data[i];
    }
    data[i] = '\0';

    free_asyncdownload_request(request);

    puts(data);
}