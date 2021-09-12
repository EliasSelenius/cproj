#include <stdlib.h>

#include "greeter.h"


#include "folder/test.h"

int main(void) {

    void* t = malloc(1);
    free(t);

    char* msg = "W3qweO";
    say_hello(msg);
    //free(msg);
    //sleep(5);

    test();
    return 0;
}