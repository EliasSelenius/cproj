# "cproj" is just an abbreviation for "c project"

cproj is a command-line-interface (CLI) that compiles and links your c project

## Motive

I have been wanting to use the programing language C, but quikly found out 
that working directly with a C compiler (like `clang`) is a pain.
The problem, probably, stems from modern languages that spoil us with easy to use build systems,
if I am used to writing `dotnet run` to build and run my dotnet application, or `cargo run` to build and run
my rust application, and never having to think about `csc` or `rustc`, then of course, working with a C
compiler is a pain.

I am well aware of the dosens of existing solutions to C building, but all I want is a simple CLI, in wich I can
write `cproj run` and it just runs.

## What It Does

Work in Progress

## Usage

cproj uses clang, so make sure it is installed. 

You can see a list of CLI arguments by running:

    cproj

### Command Line Interface
Create a new project:

    cproj new

- Creates project.xml <br>
this will store project name and output type, 
and eventualy be the place to set project dependencies. <br>
- Creates bin, obj and src folders <br>
put your C files in the src folder. <br>
The obj folder stores the compiled object files of your C source files.<br>
The bin folder stores the linked binaries.

Build project:

    cproj build

Compiles all modified C files, and links the object files

Run project:

    cproj run

Builds your project and then runs the executable

Clear project:

    cproj clear

Removes the bin and obj folders, this means you have to recompile everything.

Rebuild project:

    cproj rebuild

Build everything from scratch