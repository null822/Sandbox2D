# Notes

This is a structure for a binary file.

In this notes section, `<description>` denotes a required item in the syntax, and `[<description>]` denotes an optional item.
Any other characters are required in the syntax (including spaces).

Each value stored will have the following syntax in this file:  
`[<original type>] (<size in bytes>): <name / description OR a constant value>`.  
Note that these values will also always be in code blocks.  

Comments are denoted with `// <comment>`.  

Sections are denoted as `[ <section name> ]` in the heading format (level 2).  

Only things with the "value" syntax in a code block will be present in the file, and the only thing stored is the value, without a description/type/size attached to it.

# Block Matrix Serialization Structure

The structure is layed out as follows, and in this order:

## [ Header Section ]

These values exist only once in a file, right at the beginning, and exist as metadata for the file/Block Matrix.
This structure will always be exactly `9` bytes in size.

```
byte (1): depth of the entire blockMatrix

uint (4): size (bytes) of one element [denoted as `T` in this file]

uint (4): pointer to the start of the `[ Data Section ]`
```

## [ Tree Section ]

There are 2 types of structures in the `[Tree Section]`: one for a `QuadTree`, and one for a `QuadTreeLeaf`.

The structure of a `QuadTree` is as follows.  
This structure will always be `3` bytes in size, excluding the `_subBlocks` array  

```
{
    byte (1): 1 // this will be stored in binary, to tell the deserializer that this is a QuadTree

    // 1D index within _subBlocks
    byte (1): index

    // an array containing all of the sub blocks of this QuadTree (`_subBlocks` field)
    // really, this is just all of the sub blocks written one after the other
    []
    
    byte (1): 0 // this will be stored in binary, to tell the deserializer that this is the end of the QuadTree
}
```



The structure of a `QuadTreeLeaf` is as follows.  
This structure will always be exactly `6` bytes in size.  

```
{
    byte (1): 2 // this will be stored in binary, to tell the deserializer that this is a QuadTreeLeaf

    // 1D index within _subBlocks
    byte (1): index

    // `_value` field
    uint (4): index to the value
    // this is relative to the start of the `[ Data Section ]`
    // the absolute pointer to the value within the file can be calculated by multiplying this value by `T` and adding it to
    // the pointer to the start of the `[ Data Section ]` (found in the `[Header Section]`)
}

```

## [ Data Section ]

This section contains all of the values from the `QuadTreeLeaf`s stored in the `[Tree Section]`.

There is no character/symbol to denote a new value, since all of the values have the same size: `T`.
The order does not matter here either, so long as the pointers in the `[Tree Section]` line up correctly.  

Note that this section can be compressed by merging identical values and linking the pointers correctly,
though this may be expensive and slow down save speeds, especially for large saves.
