# Preface

This is a structure for a binary file.

In the syntax of this preface section, `<description>` denotes a required item in the syntax, and `[<description>]` denotes an optional item.
Any other characters are required in the syntax (including spaces).

Each value stored will have the following syntax in this file:  
`[<original type>] (<size in bytes>): <name / description OR a constant value>`.  
Note that these values will also always be in code blocks.

Comments are denoted with `// <comment>`.

Sections are denoted as `[ <section name> ]` in the heading format (level 2).

Only things with the "value" syntax in a code block will be present in the file, and the only thing stored is the value, without a description/type/size attached to it.

# Quadtree Serialization Structure

The structure is layed out as follows, and in this order:

## [ Header Section ]

These values exist only once in a file, right at the beginning, and exist as metadata for the quadtree.
This structure will always be exactly `13` bytes in size.

```
byte (1): max height (Quadtree._maxHeight) of the entire quadtree

uint (4): enabled features (Quadtree._enabledFeatures)

uint (4): size (bytes) of one element [denoted as `T` in this file]

uint (4): pointer to the start of the `[ Data Section ]`
```

## [ Tree Section ]

There are 2 types of structures in the `[Tree Section]`: one for a `Branch` node, and one for a `Leaf` node.

The structure of a `Branch` node is as follows.
This structure will always be `1` byte in size, excluding the `_subBlocks` array

```
{
    byte (1): 0 // states that this is a Branch node
    
    // an array containing all of the sub blocks of this QuadTree (`_subBlocks` field)
    T[]: child nodes
}
```


The structure of a `Leaf` node is as follows.  
This structure will always be exactly `5` bytes in size.

```
{
    byte (1): 1 // states that this is a Leaf node
    
    // `_value` field
    uint (4): index to the value
    // this is relative to the start of the `[ Data Section ]`
    // the absolute pointer to the value within the file can be calculated by multiplying this value by `T` and adding it to
    // the pointer to the start of the `[ Data Section ]` (found in the `[Header Section]`)
}

```

## [ Data Section ]

This section contains all the values from the `Leaf` nodes stored in the `[Tree Section]`.

There is no character/symbol to denote a new value, since all the values have the same size: `T`.
The order does not matter here either, so long as the pointers in the `[Tree Section]` line up correctly.

Note that this section can be compressed by merging identical values and linking the pointers correctly,
though this may be expensive and slow down save speeds, especially for large saves.
