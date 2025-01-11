#version 430

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

uniform uint _dataSize;
uniform uint _totalCount;

layout(std430, binding = 0) buffer _targetBuffer
{
    uint _target[];
};

layout(std430, binding = 1) buffer _dataBuffer
{
    uint _data[];
};
layout(std430, binding = 2) readonly buffer _indexBuffer
{ 
    uint _indexes[];
};

void main()
{
    uint index = gl_GlobalInvocationID.x;
    
    if (index >= _totalCount) return;
    
    uint destStartIndex = _indexes[index] * _dataSize;
    uint sourceStartIndex = index * _dataSize;
    
    for (int i = 0; i < _dataSize; i++) {
        _target[destStartIndex + i] = _data[sourceStartIndex + i];
    }
}
