#[compute]
#version 450

const int chunkWidth = 16;

layout(local_size_x = chunkWidth, local_size_y = 1, local_size_z = chunkWidth) in;
layout(set = 0, binding = 0, std430) restrict buffer NoiseBuffer { float data[]; } noiseBuffer;
layout(set = 0, binding = 1, std430) restrict buffer GradientBuffer { vec3 data[]; } gradientBuffer;
layout(set = 0, binding = 2, std430) restrict buffer HashBuffer { int data[]; } hashBuffer;
layout(set = 0, binding = 3, std430) restrict buffer ChunkPosBuffer { ivec2 data[]; } chunkPosBuffer;
layout(set = 0, binding = 4, std430) restrict buffer HeightBiases { float data[]; } heightBiasesBuffer;

float dotGradient(int x0, int y0, int z0, float x, float y, float z) {
    int hx = int(x0 * 2654435761);
    int hy = int(y0 * 2654435761);
    int hz = int(z0 * 2654435761);
    vec3 gradient = gradientBuffer.data[hashBuffer.data[(hashBuffer.data[(hashBuffer.data[hx & 255] + hy) & 255] + hz) & 255] & 15];

    float dx = x - x0;
    float dy = y - y0;
    float dz = z - z0;

    return dx * gradient.x + dy * gradient.y + dz * gradient.z;
}

float fade(float t) {
    return (t * (t * 6 - 15) + 10) * t * t * t;
}

float getNoise(float x, float y, float z) {
    int x0 = int(floor(x));
    int x1 = x0 + 1;
    int y0 = int(floor(y));
    int y1 = y0 + 1;
    int z0 = int(floor(z));
    int z1 = z0 + 1;

    float dx = x - x0;
    float dy = y - y0;
    float dz = z - z0;

    float tx = fade(dx);
    float ty = fade(dy);
    float tz = fade(dz);

    float n000 = dotGradient(x0, y0, z0, x, y, z);
    float n100 = dotGradient(x1, y0, z0, x, y, z);
    float n010 = dotGradient(x0, y1, z0, x, y, z);
    float n110 = dotGradient(x1, y1, z0, x, y, z);
    float n001 = dotGradient(x0, y0, z1, x, y, z);
    float n101 = dotGradient(x1, y0, z1, x, y, z);
    float n011 = dotGradient(x0, y1, z1, x, y, z);
    float n111 = dotGradient(x1, y1, z1, x, y, z);

    return mix(
        mix(mix(n000, n100, tx), mix(n010, n110, tx), ty),
        mix(mix(n001, n101, tx), mix(n011, n111, tx), ty),
        tz
    );
}

// The code we want to execute in each invocation
void main() {
    int localX = int(gl_GlobalInvocationID.x);
    int localY = int(gl_GlobalInvocationID.y);
    int localZ = int(gl_GlobalInvocationID.z);
    int i = localY * chunkWidth * chunkWidth + localX * chunkWidth + localZ;
    int x = localX + chunkPosBuffer.data[0].x * chunkWidth;
    int y = localY;
    int z = localZ + chunkPosBuffer.data[0].y * chunkWidth;
    float noise = (
        getNoise(x / 128.0, y / 128.0, z / 128.0) * 8
        + getNoise(x / 64.0, y / 64.0, z / 64.0) * 4
        + getNoise(x / 32.0, y / 32.0, z / 32.0) * 2
        + getNoise(x / 16.0, y / 16.0, z / 16.0) * 1
    ) / 15;
    noiseBuffer.data[i] = noise + heightBiasesBuffer.data[y];
}
