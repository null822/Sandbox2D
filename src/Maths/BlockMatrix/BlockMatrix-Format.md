# Notes

This is a structure for a binary file.

In this notes section, `<description>` denotes a required item in the syntax, and `[<description>]` denotes an optional item.
Any other characters are required in the syntax (including spaces).

Each value stored will have the following syntax in this file:  
`[<original type>] (<size in bytes>): <name / description>`.  
Note that these values will also always be in code blocks.  

Comments are denoted with `// <comment>`.  

Sections are denoted as `[ <section name> ]` in the heading format (level 2).  

Only things with the "value" syntax in a code block will be present in the file, and the only thing stored is the value, without a description/type/size attached to it.

# Block Matrix Serialization Structure

The structure is layed out as follows, and in this order:

## [ Header Section ]

These values exist only once in a file, right at the beginning, and exist as metadata for the file/Block Matrix.
This structure will always be exactly `24` bytes in size.

```
long (8): width of the entire BlockMatrix
long (8): height of the entire BlockMatrix

uint (4): size (bytes) of one element [denoted as `T` in this file]

uint (4): pointer to the start of the `[ Data Section ]`
```

## [ Tree Section ]

There are 2 types of structures in the `[Tree Section]`: one for a `BlockMatrix`, and one for a `BlockMatrixValue`.

The structure of a `BlockMatrix` is as follows.  
This structure will always be `10` bytes in size, excluding the `_subBlocks` value  

```
{
    byte (1): 1 // this will be stored in binary, to tell the deserializer that this is a BlockMatrix

    // index within _subBlocks
    uint (4): xIndex
    uint (4): yIndex

    // // `BlockSize` field
    // long (8): xSize
    // long (8): ySize

    // an array containing all of the sub blocks of this BlockMatrix (`_subBlocks` field)
    // really, this is just all of the sub blocks written one after the other
    []
    
    byte (1): 0 // this will be stored in binary, to tell the deserializer that this is the end of the BlockMatrix
}
```



The structure of a `BlockMatrixValue` is as follows.  
This structure will always be exactly `13` bytes in size.  

```
{
    byte (1): 2 // this will be stored in binary, to tell the deserializer that this is a BlockMatrixValue

    // index within _subBlocks
    uint (4): xIndex
    uint (4): yIndex

    // // `BlockSize` field
    // long (8): xSize
    // long (8): ySize

    // `_value` field
    uint (4): index to the value
    // this is relative to the start of the `[ Data Section ]`
    // the absolute pointer to the value within the file can be calculated by multiplying this value by `T` and adding it to
    // the pointer to the start of the `[ Data Section ]` (found in the `[Header Section]`)
}

```

## [ Data Section ]

This section contains all of the values from the `BlockMatrixValue`s stored in the `[Tree Section]`.

There is no character/symbol to denote a new value, since all of the values have the same size: `T`.
The order does not matter here either, so long as the pointers in the `[Tree Section]` line up correctly.  

Note that this section can be compressed by merging identical values and linking the pointers correctly, though this may be expensive and slow down save speeds.
