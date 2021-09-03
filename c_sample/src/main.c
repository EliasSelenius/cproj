
#include "greeter.h"

#include <stdlib.h>

#include "folder/test.h"

int main(void) {

    void* t = malloc(1);
    free(t);

    char* msg = "WOWOWO";
    say_hello(msg);
    //free(msg);
    //sleep(5);

    test();
    return 0;
}