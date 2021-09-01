# nav2json

A .NET Core cross-platform utility program to convert between .nav and .navjson files.

# Parameters

```
nav2json [parameters] [input file]
-json                               Convert from nav to a json file.
-nav                                Convert from json to a nav file.

-s                                  Silent.
-ow                                 Overwrite without asking.
-out [file]                         Output path + filename.
-outstdout                          Output to STDOUT instead of a file.

-map-filename [filename]            Specifies the map filename for the json.
-map-author [author]                Specifies the map author for the json.
-map-name [name]                    Specifies the map name for the json.
-map-url [url]                      Adds a map url for the json. Can add multiple.
-json-comment [comment]             Specifies a comment for the json.
-json-contributor [contributor]     Adds a contributor to the json. Can add multiple.
```

Automatic file detection will be applied if no -json or -nav parameter is specified. If .nav, it will convert to .navjson, and vice-versa.

You can just drag & drop a file into the program.
